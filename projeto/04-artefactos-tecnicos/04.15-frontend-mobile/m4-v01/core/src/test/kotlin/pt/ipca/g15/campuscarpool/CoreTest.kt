package pt.ipca.g15.campuscarpool

import java.io.IOException
import java.util.Base64
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.advanceTimeBy
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertThrows
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test
import pt.ipca.g15.campuscarpool.data.remote.ApiFactory
import pt.ipca.g15.campuscarpool.data.remote.ApiService
import pt.ipca.g15.campuscarpool.data.remote.ErrosApi
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.data.session.SessionStore
import pt.ipca.g15.campuscarpool.domain.model.Credenciais
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.Sessao
import pt.ipca.g15.campuscarpool.domain.model.SessaoAtual
import pt.ipca.g15.campuscarpool.domain.model.TerminarSessaoPedido
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso
import retrofit2.Response

internal const val BASE = "https://exemplo.test/api/v1/"
internal fun token(id: String = "A", exp: Long = 3600): String = "header." +
    Base64.getUrlEncoder().withoutPadding().encodeToString(
        "{\"sub\":\"$id\",\"exp\":$exp}".toByteArray()
    ) +
    ".signature"
internal class Memoria(var valor: Sessao? = null) : SessionStore {
    override fun ler() = valor
    override fun guardar(sessao: Sessao) {
        valor = sessao
    }
    override fun limpar() {
        valor = null
    }
}
internal class ApiFalsa : ApiService {
    var logins = 0
    var entrada: Credenciais? = null
    var entradaLogout: TerminarSessaoPedido? = null
    var loginResposta: Response<TokenAcesso> = Response.success(TokenAcesso(token(), 3600))
    var atualResposta: Response<SessaoAtual> = Response.success(SessaoAtual("A"))
    var logoutResposta: Response<Unit> = Response.success<Unit>(204, null)
    var falharLogout = false
    override suspend fun login(dados: Credenciais): Response<TokenAcesso> {
        logins++
        entrada =
            dados
        return loginResposta
    }
    override suspend fun sessaoAtual() = atualResposta
    override suspend fun logout(pedido: TerminarSessaoPedido): Response<Unit> {
        entradaLogout =
            pedido
        if (falharLogout) throw IOException()
        return logoutResposta
    }
    override suspend fun perfilProprio() =
        Response.success(PerfilProprio("A", "Ana", "ana@exemplo.test", null, null, null, 0))
}
internal fun <T> erro(status: Int): Response<T> = Response.error(
    status,
    "{\"erro\":\"Pedido recusado\"}".toResponseBody("application/json".toMediaType())
)

@OptIn(ExperimentalCoroutinesApi::class)
class SessaoTest {
    @Test fun persisteERestauraSessaoValida() = runTest {
        val store = Memoria()
        val s = SessaoManager(store, BASE, backgroundScope) { testScheduler.currentTime }
        s.iniciar(TokenAcesso(token(), 3600))
        assertEquals("A", s.estado.value?.utilizadorId)
        assertEquals(token(), SessaoManager(store, BASE, backgroundScope) { 0 }.obterToken())
    }

    @Test fun expiraAutomaticamente() = runTest {
        val store = Memoria()
        val s = SessaoManager(store, BASE, backgroundScope) { testScheduler.currentTime }
        s.iniciar(TokenAcesso(token(exp = 1), 3600))
        runCurrent()
        advanceTimeBy(1001)
        runCurrent()
        assertNull(s.estado.value)
        assertNull(store.valor)
    }

    @Test fun expiraNaConsultaMesmoSemTemporizador() = runTest {
        var agora = 0L
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { agora }
        s.iniciar(TokenAcesso(token(), 1))
        agora = 1000
        assertNull(s.obterToken())
    }

    @Test fun rejeitaSessaoDeOutroBackend() = runTest {
        val m = Memoria(Sessao(token(), "A", 3600000, "https://outro.test/api/v1/"))
        assertNull(SessaoManager(m, BASE, backgroundScope) { 0 }.obterToken())
        assertNull(m.valor)
    }

    @Test fun rejeitaEstadoPersistidoIncoerente() = runTest {
        listOf(
            Sessao(token(), "B", 1000, BASE),
            Sessao(token(), "A", 4000000, BASE),
            Sessao("invalido", "A", 1000, BASE),
            Sessao(token(), "A", 0, BASE)
        ).forEach {
            val m = Memoria(it)
            assertNull(SessaoManager(m, BASE, backgroundScope) { 0 }.obterToken())
            assertNull(m.valor)
        }
    }

    @Test fun rejeitaTokensInvalidos() = runTest {
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        listOf(
            TokenAcesso("abc", 3600),
            TokenAcesso(token(), 0),
            TokenAcesso(token(), 86401),
            TokenAcesso(token(exp = 0), 3600),
            TokenAcesso(token(id = ""), 3600)
        ).forEach {
            assertThrows(IllegalArgumentException::class.java) { s.iniciar(it) }
        }
        assertNull(s.obterToken())
    }

    @Test fun respostaAntigaNaoTerminaSessaoNova() = runTest {
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        s.iniciar(TokenAcesso(token(), 3600))
        s.iniciar(TokenAcesso(token("B"), 3600))
        s.terminarSeToken(token())
        s.terminarSeToken(null)
        assertEquals(token("B"), s.obterToken())
        s.terminarSeToken(token("B"))
        assertNull(s.obterToken())
    }

    @Test fun expiresInLimitaJwt() = runTest {
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        s.iniciar(TokenAcesso(token(), 60))
        assertEquals(60000L, s.estado.value?.expiraEm)
    }
}
class ErrosTest {
    @Test fun interpretaErroDaApi() {
        assertEquals(
            "Pedido recusado",
            ErrosApi.resposta(400, "{\"erro\":\"Pedido recusado\"}").mensagem
        )
    }

    @Test fun interpretaCamposProblemDetails() {
        val r = ErrosApi.resposta(
            400,
            "{\"detail\":\"Inválido\",\"errors\":{\"Email\":[\"Obrigatório\"]}}"
        )
        assertEquals(listOf("Obrigatório"), r.campos["Email"])
    }

    @Test fun interpretaFormatoHistorico() {
        val r = ErrosApi.resposta(
            400,
            "{\"erro\":{\"mensagem\":\"Inválido\",\"campos\":{\"nome\":\"Obrigatório\"}}}"
        )
        assertEquals("Inválido", r.mensagem)
        assertEquals(listOf("Obrigatório"), r.campos["nome"])
    }

    @Test fun naoExibeDetalhesInternos() {
        assertFalse(ErrosApi.resposta(500, "{\"erro\":\"segredo\"}").mensagem.contains("segredo"))
    }

    @Test fun mostraMensagemDaApiNasFalhasDeServicosExternos() {
        listOf(502, 503, 504).forEach {
            assertEquals(
                "Serviço de mapas indisponível",
                ErrosApi.resposta(it, "{\"erro\":\"Serviço de mapas indisponível\"}").mensagem
            )
        }
    }

    @Test fun semCorpoAceita204() = runTest {
        assertEquals(
            Resultado.Sucesso(Unit),
            ErrosApi.executarSemCorpo { Response.success<Unit>(204, null) }
        )
    }

    @Test fun semCorpoPropagaErroDaApi() = runTest {
        assertEquals(409, (ErrosApi.executarSemCorpo { erro(409) } as Resultado.Falha).status)
    }

    @Test fun toleraCorposInvalidosEStatusConhecidos() {
        listOf(400, 401, 403, 404, 409, 429, 503, 418).forEach {
            assertTrue(ErrosApi.resposta(it, "<html>").mensagem.isNotBlank())
            assertEquals(it, ErrosApi.resposta(it, null).status)
        }
    }

    @Test fun limitaMensagens() {
        assertEquals(
            500,
            ErrosApi.resposta(400, "{\"erro\":\"${"x".repeat(600)}\"}").mensagem.length
        )
    }

    @Test fun sucessoEVazio() = runTest {
        assertEquals(Resultado.Sucesso("ok"), ErrosApi.executar { Response.success("ok") })
        assertTrue(ErrosApi.executar<String> { Response.success(null) } is Resultado.Falha)
    }

    @Test fun erroHttpERede() = runTest {
        assertEquals(409, (ErrosApi.executar<String> { erro(409) } as Resultado.Falha).status)
        assertTrue(
            (
                ErrosApi.executar<String> {
                    throw IOException()
                } as Resultado.Falha
                ).mensagem.contains("ligação")
        )
    }

    @Test fun formatoInvalido() = runTest {
        assertTrue(ErrosApi.executar<String> { throw IllegalStateException() } is Resultado.Falha)
    }

    @Test fun respeitaCancelamento() = runTest {
        try {
            ErrosApi.executar<String> { throw CancellationException() }
            fail()
        } catch (
            _: CancellationException
        ) { }
    }
}
class RepositoryTest {
    @Test fun loginGuardaSessaoEConfirmaIdentidade() = runTest {
        val api = ApiFalsa()
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        assertTrue(
            AutenticacaoRepository(
                api,
                s
            ).login(" ana@exemplo.test ", " Segredo123 ") is Resultado.Sucesso
        )
        assertEquals("ana@exemplo.test", api.entrada?.email)
        assertEquals(" Segredo123 ", api.entrada?.password)
        assertNotNull(s.obterToken())
    }

    @Test fun naoEnviaCredenciaisVazias() = runTest {
        val api = ApiFalsa()
        val r = AutenticacaoRepository(api, SessaoManager(Memoria(), BASE, backgroundScope))
        assertTrue(r.login("", "x") is Resultado.Falha)
        assertTrue(r.login("a", "") is Resultado.Falha)
        assertEquals(0, api.logins)
    }

    @Test fun falhaLoginNaoGuardaSessao() = runTest {
        val api = ApiFalsa()
        api.loginResposta =
            erro(401)
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        assertTrue(AutenticacaoRepository(api, s).login("a", "b") is Resultado.Falha)
        assertNull(s.obterToken())
    }

    @Test fun tokenInvalidoNaoAbreSessao() = runTest {
        val api = ApiFalsa()
        api.loginResposta =
            Response.success(TokenAcesso("abc", 3600))
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        assertTrue(AutenticacaoRepository(api, s).login("a", "b") is Resultado.Falha)
        assertNull(s.obterToken())
    }

    @Test fun identidadeDivergenteEliminaSessao() = runTest {
        val api = ApiFalsa()
        api.atualResposta =
            Response.success(SessaoAtual("B"))
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        assertTrue(AutenticacaoRepository(api, s).login("a", "b") is Resultado.Falha)
        assertNull(s.obterToken())
    }

    @Test fun validacaoRecusadaEliminaSessao() = runTest {
        val api = ApiFalsa()
        api.atualResposta =
            erro(403)
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        assertTrue(AutenticacaoRepository(api, s).login("a", "b") is Resultado.Falha)
        assertNull(s.obterToken())
    }

    @Test fun logoutRevogaELimpaComTokenDispositivo() = runTest {
        val api = ApiFalsa()
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        s.iniciar(TokenAcesso(token(), 3600))
        assertTrue(AutenticacaoRepository(api, s).logout("fcm-teste") is Resultado.Sucesso)
        assertEquals("fcm-teste", api.entradaLogout?.tokenDispositivo)
        assertNull(s.obterToken())
    }

    @Test fun logoutSemRedeTerminaLocalmente() = runTest {
        val api = ApiFalsa()
        api.falharLogout = true
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        s.iniciar(TokenAcesso(token(), 3600))
        assertTrue(AutenticacaoRepository(api, s).logout() is Resultado.Falha)
        assertNull(s.obterToken())
    }

    @Test fun logoutComErroTerminaLocalmente() = runTest {
        val api = ApiFalsa()
        api.logoutResposta =
            erro(500)
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        s.iniciar(TokenAcesso(token(), 3600))
        assertTrue(AutenticacaoRepository(api, s).logout() is Resultado.Falha)
        assertNull(s.obterToken())
    }

    @Test fun logout401TerminaComSucesso() = runTest {
        val api = ApiFalsa()
        api.logoutResposta =
            erro(401)
        val s = SessaoManager(Memoria(), BASE, backgroundScope) { 0 }
        s.iniciar(TokenAcesso(token(), 3600))
        assertTrue(AutenticacaoRepository(api, s).logout() is Resultado.Sucesso)
        assertNull(s.obterToken())
    }

    @Test fun consultaPerfil() = runTest {
        assertEquals(
            "Ana",
            (PerfilRepository(ApiFalsa()).obterProprio() as Resultado.Sucesso).valor.nome
        )
    }
}
class UrlTest {
    @Test fun aceitaHttpsEEmuladorDebug() {
        assertEquals("exemplo.test", ApiFactory.validarUrl(BASE).host)
        assertFalse(ApiFactory.validarUrl("http://10.0.2.2:5000/api/v1/", true).isHttps)
    }

    @Test fun recusaUrlInseguraOuAmbigua() {
        listOf(
            "http://exemplo.test/api/v1/",
            "https://user:pass@exemplo.test/api/v1/",
            "https://exemplo.test/api/v1/?x=1",
            "https://exemplo.test/api/v1/#a",
            "https://exemplo.test/outra/"
        ).forEach {
            assertThrows(IllegalArgumentException::class.java) { ApiFactory.validarUrl(it) }
        }
        assertThrows(IllegalArgumentException::class.java) {
            ApiFactory.validarUrl("http://exemplo.test/api/v1/", true)
        }
    }
}
