package pt.ipca.g15.campuscarpool

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.runBlocking
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import pt.ipca.g15.campuscarpool.data.remote.ApiFactory
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.LigacaoApp
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso

class ValidacaoFormulariosTest {
    private val valido = arrayOf("Ana", "ana@ipca.pt", "Segredo123", "Segredo123")

    private fun registo(
        nome: String = valido[0],
        email: String = valido[1],
        password: String = valido[2],
        confirmacao: String = valido[3]
    ) = ValidacaoFormularios.registo(nome, email, password, confirmacao)

    @Test fun registoValidoNaoTemErros() = assertTrue(registo().isEmpty())

    @Test fun nomeObrigatorioEAte100() {
        assertEquals("Indica o teu nome.", registo(nome = "   ")["nome"])
        assertTrue(registo(nome = "a".repeat(100)).isEmpty())
        assertEquals(
            "O nome tem no máximo 100 caracteres.",
            registo(nome = "a".repeat(101))["nome"]
        )
    }

    @Test fun formatoDoEmail() {
        assertEquals("Indica o teu email académico.", registo(email = "")["email"])
        listOf("ana", "ana@ipca", "a b@ipca.pt", "a".repeat(250) + "@ip.pt").forEach {
            assertEquals(it, "Indica um email válido.", registo(email = it)["email"])
        }
        assertNull(ValidacaoFormularios.email("  ana@ipca.pt  "))
    }

    @Test fun regraDaPalavraPasseP08() {
        val regra = "Usa pelo menos 8 caracteres, com uma letra e um dígito."
        listOf("Abc1234", "abcdefgh", "12345678").forEach {
            assertEquals(it, regra, registo(password = it, confirmacao = it)["password"])
        }
        assertTrue(registo(password = "Ção12345", confirmacao = "Ção12345").isEmpty())
        assertEquals(
            "Indica uma palavra-passe.",
            registo(password = "", confirmacao = "")["password"]
        )
    }

    @Test fun confirmacaoDaPalavraPasse() {
        assertEquals("Repete a palavra-passe.", registo(confirmacao = "")["confirmacao"])
        assertEquals(
            "As palavras-passe não coincidem.",
            registo(confirmacao = "Outra1234")["confirmacao"]
        )
    }

    @Test fun limitesDoPerfilP26() {
        assertTrue(ValidacaoFormularios.perfil("Ana", "c".repeat(150), "i".repeat(150)).isEmpty())
        val erros = ValidacaoFormularios.perfil(" ", "c".repeat(151), "i".repeat(151))
        assertEquals(setOf("nome", "curso", "instituicao"), erros.keys)
    }

    @Test fun enderecosDasLigacoes() {
        listOf("https://exemplo.pt/a?b=1", "HTTP://exemplo.pt").forEach {
            assertTrue(it, ValidacaoFormularios.ligacao(it, "").isEmpty())
        }
        listOf(
            "javascript:alert(1)",
            "exemplo.pt",
            "https:exemplo.pt",
            "ftp://a.pt",
            "https://a b"
        ).forEach {
            assertEquals(
                it,
                "Indica um endereço completo, a começar por http:// ou https://.",
                ValidacaoFormularios.ligacao(it, "")["url"]
            )
        }
        assertEquals("Indica o endereço da ligação.", ValidacaoFormularios.ligacao(" ", "")["url"])
    }

    @Test fun limitesDasLigacoes() {
        val base = "https://a.pt/"
        assertTrue(
            ValidacaoFormularios.ligacao(base + "a".repeat(2048 - base.length), "").isEmpty()
        )
        assertEquals(
            setOf("url", "descricao"),
            ValidacaoFormularios.ligacao(base + "a".repeat(2048), "d".repeat(101)).keys
        )
    }
}

class LigacaoAppTest {
    private val id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"

    @Test fun ligacaoDeConfirmacao() {
        assertEquals(
            LigacaoApp.Confirmar("a+b/c"),
            LigacaoApp.interpretar("campuscarpool://confirmar-conta?token=a%2Bb%2Fc")
        )
    }

    @Test fun ligacaoDePerfil() {
        assertEquals(
            LigacaoApp.Perfil(id),
            LigacaoApp.interpretar("campuscarpool://utilizadores/$id")
        )
    }

    @Test fun recusaLigacoesDesconhecidas() {
        listOf(
            null,
            "",
            "https://exemplo.pt/confirmar-conta?token=a",
            "campuscarpool://outra?token=a",
            "campuscarpool://confirmar-conta",
            "campuscarpool://confirmar-conta?token=",
            "campuscarpool://confirmar-conta?token=%zz",
            "campuscarpool://utilizadores/nao-e-um-id",
            "campuscarpool://utilizadores/$id/../x"
        ).forEach { assertNull(it, LigacaoApp.interpretar(it)) }
    }

    @Test fun textoColadoAceitaLigacaoOuCodigo() {
        assertEquals("abc", LigacaoApp.tokenDoTexto(" abc "))
        assertEquals(
            "a b",
            LigacaoApp.tokenDoTexto("http://localhost:4200/confirmar-conta?token=a%20b")
        )
        listOf("", "   ", "https://exemplo.pt/sem-codigo", "duas palavras").forEach {
            assertNull(it, LigacaoApp.tokenDoTexto(it))
        }
    }
}

class ContratoF001Test {
    private val server = MockWebServer()
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private lateinit var sessao: SessaoManager
    private lateinit var auth: AutenticacaoRepository
    private lateinit var perfis: PerfilRepository
    private val id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    private val ligacao = "{\"id\":\"L1\",\"url\":\"https://a.pt\",\"descricao\":null}"

    @Before fun preparar() {
        server.start(System.getProperty("testServerPort", "0").toInt())
        val base = server.url("/api/v1/")
        sessao = SessaoManager(Memoria(), base.toString(), scope) { 0 }
        sessao.iniciar(TokenAcesso(token(), 3600))
        val api = ApiFactory.criar(base, sessao)
        auth = AutenticacaoRepository(api, sessao)
        perfis = PerfilRepository(api)
    }

    @After fun limpar() {
        scope.cancel()
        server.shutdown()
    }

    private fun resposta(codigo: Int, corpo: String = "") =
        server.enqueue(MockResponse().setResponseCode(codigo).setBody(corpo))

    @Test fun registoEPublicoESemToken() = runBlocking {
        resposta(202, "{\"mensagem\":\"Verifica o email.\"}")
        val r = auth.registar(" Ana ", " ana@ipca.pt ", "Segredo123")
        assertEquals(Resultado.Sucesso("Verifica o email."), r)
        val pedido = server.takeRequest()
        assertEquals("/api/v1/autenticacao/registo", pedido.path)
        assertNull(pedido.getHeader("Authorization"))
        val corpo = pedido.body.readUtf8()
        assertTrue(corpo, corpo.contains("\"nome\":\"Ana\""))
        assertTrue(corpo, corpo.contains("\"email\":\"ana@ipca.pt\""))
        assertTrue(corpo, corpo.contains("\"password\":\"Segredo123\""))
    }

    @Test fun errosDoRegistoGuardamAOrigem() = runBlocking {
        resposta(409, "{\"erro\":\"Email já registado.\"}")
        val conflito = auth.registar("Ana", "a@ipca.pt", "Segredo123") as Resultado.Falha
        assertEquals(409, conflito.status)
        assertEquals("Email já registado.", conflito.mensagem)
        assertTrue(conflito.daApi)

        resposta(503, "<html>indisponível</html>")
        val infraestrutura = auth.registar("Ana", "a@ipca.pt", "Segredo123") as Resultado.Falha
        assertEquals(503, infraestrutura.status)
        assertFalse(infraestrutura.daApi)
    }

    @Test fun confirmarEReenviar() = runBlocking {
        resposta(204)
        assertEquals(Resultado.Sucesso(Unit), auth.confirmar("abc"))
        val confirmar = server.takeRequest()
        assertEquals("/api/v1/autenticacao/confirmar", confirmar.path)
        assertEquals("{\"token\":\"abc\"}", confirmar.body.readUtf8())
        assertNull(confirmar.getHeader("Authorization"))

        resposta(400, "{\"erro\":\"A ligação expirou.\"}")
        assertEquals("A ligação expirou.", (auth.confirmar("abc") as Resultado.Falha).mensagem)
        server.takeRequest()

        resposta(202, "{\"mensagem\":\"Se existir, foi enviada.\"}")
        assertEquals(
            Resultado.Sucesso("Se existir, foi enviada."),
            auth.reenviarConfirmacao(" a@b.pt ")
        )
        val reenvio = server.takeRequest()
        assertEquals("/api/v1/autenticacao/reenviar-confirmacao", reenvio.path)
        assertEquals("{\"email\":\"a@b.pt\"}", reenvio.body.readUtf8())
    }

    @Test fun atualizarPerfilOmiteCamposVazios() = runBlocking {
        resposta(
            200,
            "{\"id\":\"A\",\"nome\":\"Ana\",\"email\":\"a@b.pt\",\"emailConfirmado\":true," +
                "\"curso\":null,\"instituicao\":\"IPCA\"}"
        )
        val r = perfis.atualizar(" Ana ", "  ", " IPCA ") as Resultado.Sucesso
        assertEquals("IPCA", r.valor.instituicao)
        val pedido = server.takeRequest()
        assertEquals("PUT", pedido.method)
        assertEquals("/api/v1/utilizadores/me", pedido.path)
        assertEquals("Bearer ${token()}", pedido.getHeader("Authorization"))
        assertEquals("{\"nome\":\"Ana\",\"instituicao\":\"IPCA\"}", pedido.body.readUtf8())
    }

    @Test fun perfilProprioTrazLigacoes() = runBlocking {
        resposta(
            200,
            "{\"id\":\"A\",\"nome\":\"Ana\",\"email\":\"a@b.pt\",\"curso\":null," +
                "\"instituicao\":null,\"classificacaoMedia\":4.5,\"totalAvaliacoes\":2," +
                "\"ligacoesExternas\":[$ligacao]}"
        )
        val r = perfis.obterProprio() as Resultado.Sucesso
        assertEquals("https://a.pt", r.valor.ligacoesExternas.single().url)
    }

    @Test fun ligacoesDoPerfil() = runBlocking {
        resposta(201, ligacao)
        val criada = perfis.adicionarLigacao(" https://a.pt ", "") as Resultado.Sucesso
        assertEquals("L1", criada.valor.id)
        val pedido = server.takeRequest()
        assertEquals("/api/v1/utilizadores/me/ligacoes", pedido.path)
        assertEquals("{\"url\":\"https://a.pt\"}", pedido.body.readUtf8())

        resposta(204)
        assertEquals(Resultado.Sucesso(Unit), perfis.removerLigacao("L1"))
        val remover = server.takeRequest()
        assertEquals("DELETE", remover.method)
        assertEquals("/api/v1/utilizadores/me/ligacoes/L1", remover.path)
    }

    @Test fun perfilPublicoNaoTemEmail() = runBlocking {
        resposta(
            200,
            "{\"id\":\"$id\",\"nome\":\"Rui\",\"curso\":\"LEI\",\"instituicao\":null," +
                "\"ligacoesExternas\":[],\"classificacaoMedia\":null,\"totalAvaliacoes\":0}"
        )
        val r = perfis.obterPublico(id) as Resultado.Sucesso
        assertEquals("Rui", r.valor.nome)
        assertEquals("/api/v1/utilizadores/$id", server.takeRequest().path)
        assertFalse(r.valor.javaClass.declaredFields.any { it.name == "email" })

        resposta(404, "{\"erro\":\"Não encontrado.\"}")
        assertEquals(404, (perfis.obterPublico(id) as Resultado.Falha).status)
    }
}
