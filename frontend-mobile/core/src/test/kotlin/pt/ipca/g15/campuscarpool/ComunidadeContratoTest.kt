package pt.ipca.g15.campuscarpool
import com.google.gson.JsonObject
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
import pt.ipca.g15.campuscarpool.data.remote.ComunidadeApi
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
class ComunidadeContratoTest {
    private val servidor = MockWebServer()
    private lateinit var api: ComunidadeApi

    @Before fun preparar() {
        servidor.start()
        api =
            Retrofit.Builder().baseUrl(
                servidor.url("/api/v1/")
            ).addConverterFactory(
                GsonConverterFactory.create()
            ).build().create(ComunidadeApi::class.java)
    }

    @After fun terminar() {
        servidor.shutdown()
    }
    private fun json(vararg pares: Pair<String, String>) =
        JsonObject().apply { pares.forEach { (k, v) -> addProperty(k, v) } }

    @Test fun pesquisaCodificaFiltrosEConservaPaginacao() = runBlocking {
        servidor.enqueue(MockResponse().setBody("{\"total\":0,\"viagens\":[]}"))
        val r = api.pesquisar(
            mapOf(
                "origem" to "Porto & Gaia",
                "pagina" to "2"
            )
        )
        assertTrue(r.isSuccessful)
        val p = servidor.takeRequest()
        assertEquals("Porto & Gaia", p.requestUrl!!.queryParameter("origem"))
        assertEquals("2", p.requestUrl!!.queryParameter("pagina"))
        assertEquals("/api/v1/pesquisa/viagens", p.requestUrl!!.encodedPath)
    }

    @Test fun pedidoNaoInvocaAceitacaoAutomaticamente() = runBlocking {
        servidor.enqueue(
            MockResponse().setResponseCode(201).setBody("{\"id\":\"p1\",\"estado\":\"Pendente\"}")
        )
        val r = api.pedir(
            json(
                "viagemId" to "v1",
                "recolha" to "Campus"
            )
        )
        assertEquals("Pendente", r.body()!!.get("estado").asString)
        val p = servidor.takeRequest()
        assertEquals("POST", p.method)
        assertEquals("/api/v1/pedidos", p.path)
        assertEquals(1, servidor.requestCount)
    }

    @Test fun decisaoAceiteSemCorpoE204() = runBlocking {
        servidor.enqueue(MockResponse().setResponseCode(204))
        assertTrue(api.decidir("p1", "aceitar").isSuccessful)
        assertEquals("/api/v1/participacoes/p1/aceitar", servidor.takeRequest().path)
    }

    @Test fun erroDeConflitoNaoEConvertidoEmSucesso() = runBlocking {
        servidor.enqueue(MockResponse().setResponseCode(409).setBody("{\"erro\":\"Sem lugares\"}"))
        val r = api.decidir("p1", "aceitar")
        assertFalse(r.isSuccessful)
        assertEquals(409, r.code())
        servidor.takeRequest()
        Unit
    }

    @Test fun notificacoesUsamParametroTamanhoPagina() = runBlocking {
        servidor.enqueue(MockResponse().setBody("{\"itens\":[]}"))
        api.notificacoes(2, 20)
        val p = servidor.takeRequest()
        assertEquals("2", p.requestUrl!!.queryParameter("pagina"))
        assertEquals("20", p.requestUrl!!.queryParameter("tamanhoPagina"))
        assertNull(p.requestUrl!!.queryParameter("tamanho"))
    }

    @Test fun leituraUsaPutE204() = runBlocking {
        servidor.enqueue(MockResponse().setResponseCode(204))
        assertTrue(api.ler("n1").isSuccessful)
        val p = servidor.takeRequest()
        assertEquals("PUT", p.method)
        assertEquals("/api/v1/notificacoes/n1/ler", p.path)
    }

    @Test fun mensagemConservaTextoSemHTMLLocal() = runBlocking {
        servidor.enqueue(MockResponse().setResponseCode(201).setBody("{\"id\":\"m1\"}"))
        api.mensagem(
            json(
                "destinatarioId" to "u1",
                "conteudo" to "Olá <colega>"
            )
        )
        val p = servidor.takeRequest()
        assertEquals("/api/v1/mensagens", p.path)
        val texto = com.google.gson.JsonParser.parseString(p.body.readUtf8()).asJsonObject
        assertEquals("Olá <colega>", texto.get("conteudo").asString)
    }
}
