package pt.ipca.g15.campuscarpool

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.runBlocking
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import pt.ipca.g15.campuscarpool.data.remote.ApiFactory
import pt.ipca.g15.campuscarpool.data.remote.AuthInterceptor
import pt.ipca.g15.campuscarpool.data.remote.ErroInterceptor
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso

class ApiIntegrationTest {
    private val server = MockWebServer()
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private lateinit var sessao: SessaoManager

    @Before fun preparar() {
        server.start(System.getProperty("testServerPort", "0").toInt())
        sessao = SessaoManager(Memoria(), server.url("/api/v1/").toString(), scope) { 0 }
        sessao.iniciar(TokenAcesso(token(), 3600))
    }

    @After fun limpar() {
        scope.cancel()
        server.shutdown()
    }

    @Test fun loginPublicoEPerfilPrivadoUsamContratoReal() = runBlocking {
        val api = ApiFactory.criar(server.url("/api/v1/"), sessao)
        server.enqueue(
            MockResponse().setBody("{\"accessToken\":\"${token()}\",\"expiresIn\":3600}")
        )
        server.enqueue(MockResponse().setBody("{\"id\":\"A\"}"))
        assertTrue(
            AutenticacaoRepository(
                api,
                sessao
            ).login("ana@exemplo.test", "Segredo123") is Resultado.Sucesso
        )
        val login = server.takeRequest()
        assertEquals("/api/v1/autenticacao/login", login.path)
        assertNull(login.getHeader("Authorization"))
        assertTrue(login.body.readUtf8().contains("\"email\":\"ana@exemplo.test\""))
        assertEquals("Bearer ${token()}", server.takeRequest().getHeader("Authorization"))
        server.enqueue(
            MockResponse().setBody(
                "{\"id\":\"A\",\"nome\":\"Ana\",\"email\":\"ana@exemplo.test\",\"totalAvaliacoes\":0}"
            )
        )
        assertEquals("Ana", (PerfilRepository(api).obterProprio() as Resultado.Sucesso).valor.nome)
        assertEquals("/api/v1/utilizadores/me", server.takeRequest().path)
    }

    @Test fun resposta401LimpaSessaoEnviada() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(401).setBody("{\"erro\":\"Token revogado\"}"))
        val r = PerfilRepository(ApiFactory.criar(server.url("/api/v1/"), sessao)).obterProprio()
        assertEquals(401, (r as Resultado.Falha).status)
        assertNull(sessao.obterToken())
    }

    @Test fun erro403MantemSessao() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(403).setBody("{\"erro\":\"Sem acesso\"}"))
        val r = PerfilRepository(ApiFactory.criar(server.url("/api/v1/"), sessao)).obterProprio()
        assertEquals(403, (r as Resultado.Falha).status)
        assertEquals(token(), sessao.obterToken())
    }

    @Test fun logoutAceita204SemCorpo() = runBlocking {
        server.enqueue(MockResponse().setResponseCode(204))
        assertTrue(
            AutenticacaoRepository(
                ApiFactory.criar(server.url("/api/v1/"), sessao),
                sessao
            ).logout() is Resultado.Sucesso
        )
        assertEquals("/api/v1/autenticacao/logout", server.takeRequest().path)
        assertNull(sessao.obterToken())
    }

    @Test fun naoSegueRedirecionamentoComCredenciais() = runBlocking {
        server.enqueue(
            MockResponse().setResponseCode(302).setHeader("Location", server.url("/fora"))
        )
        assertTrue(
            PerfilRepository(
                ApiFactory.criar(server.url("/api/v1/"), sessao)
            ).obterProprio() is Resultado.Falha
        )
        assertEquals(1, server.requestCount)
    }

    @Test fun naoEnviaTokenForaDaBase() {
        val base = server.url("/api/v1/")
        val client = OkHttpClient.Builder().addInterceptor(
            AuthInterceptor(base, sessao)
        ).addInterceptor(ErroInterceptor(base, sessao)).build()
        server.enqueue(MockResponse().setResponseCode(401))
        client.newCall(Request.Builder().url(server.url("/fora")).build()).execute().close()
        assertNull(server.takeRequest().getHeader("Authorization"))
        assertEquals(token(), sessao.obterToken())
    }

    @Test fun naoEnviaTokenParaOutraOrigem() {
        val base = server.url("/api/v1/").newBuilder().host("exemplo.test").build()
        val client = OkHttpClient.Builder().addInterceptor(AuthInterceptor(base, sessao)).build()
        server.enqueue(MockResponse())
        client.newCall(
            Request.Builder().url(server.url("/api/v1/utilizadores/me")).build()
        ).execute().close()
        assertNull(server.takeRequest().getHeader("Authorization"))
    }

    @Test fun semSessaoNaoAcrescentaBearer() = runBlocking {
        sessao.terminar()
        server.enqueue(MockResponse().setResponseCode(401))
        PerfilRepository(ApiFactory.criar(server.url("/api/v1/"), sessao)).obterProprio()
        assertNull(server.takeRequest().getHeader("Authorization"))
    }
}
