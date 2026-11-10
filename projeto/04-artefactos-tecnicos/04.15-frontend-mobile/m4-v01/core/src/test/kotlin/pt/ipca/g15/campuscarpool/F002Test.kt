package pt.ipca.g15.campuscarpool

import com.google.gson.JsonParser
import java.time.Instant
import java.time.ZoneOffset
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.runBlocking
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import pt.ipca.g15.campuscarpool.data.remote.ApiFactory
import pt.ipca.g15.campuscarpool.data.repository.ViagemRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.domain.DataHoraFormatter
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.CamposPartida
import pt.ipca.g15.campuscarpool.domain.model.CamposViagem
import pt.ipca.g15.campuscarpool.domain.model.DadosViagem
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso

class ValidacaoViagemTest {
    private val agora = Instant.parse("2026-11-10T10:00:00Z")
    private val valido = CamposViagem("Barcelos", "Braga", "25/12/2026", "09:30", "3")

    private fun erros(campos: CamposViagem, ocupados: Int = 0) =
        ValidacaoFormularios.viagem(campos, ocupados, agora)

    @Test fun dadosValidosNaoTemErros() = assertTrue(erros(valido).isEmpty())

    @Test fun camposObrigatorios() {
        val r = erros(CamposViagem("", " ", "", "", ""))
        assertEquals("Indica a origem.", r["origem"])
        assertEquals("Indica o destino.", r["destino"])
        assertEquals("Indica a data da viagem.", r["data"])
        assertEquals("Indica a hora da viagem.", r["hora"])
        assertEquals("Indica o número de lugares.", r["lugares"])
    }

    @Test fun locaisAte200Caracteres() {
        assertTrue(erros(valido.copy(origem = "a".repeat(200))).isEmpty())
        assertEquals(
            "A origem tem no máximo 200 caracteres.",
            erros(valido.copy(origem = "a".repeat(201)))["origem"]
        )
        assertEquals(
            "O destino tem no máximo 200 caracteres.",
            erros(valido.copy(destino = "a".repeat(201)))["destino"]
        )
    }

    @Test fun mesmoLocalIgnoraMaiusculasEEspacosRfF00204() {
        val mensagem = "A origem e o destino não podem ser o mesmo local."
        assertEquals(mensagem, erros(valido.copy(destino = "  bARCELOS "))["destino"])
    }

    @Test fun datasEHorasMalFormadas() {
        listOf("2026-12-25", "32/12/2026", "31/02/2026", "1/1/26").forEach {
            assertEquals(
                it,
                "Indica a data no formato dia/mês/ano.",
                erros(valido.copy(data = it))["data"]
            )
        }
        listOf("9h30", "24:00", "09:60").forEach {
            assertEquals(
                it,
                "Indica a hora no formato hh:mm.",
                erros(valido.copy(hora = it))["hora"]
            )
        }
    }

    @Test fun partidaTemDeSerFuturaRfF00202() {
        val mensagem = "A data e a hora têm de ser posteriores ao momento atual."
        assertEquals(mensagem, erros(valido.copy(data = "10/11/2026", hora = "10:00"))["hora"])
        assertEquals(mensagem, erros(valido.copy(data = "09/11/2026", hora = "23:00"))["hora"])
        assertTrue(erros(valido.copy(data = "10/11/2026", hora = "10:01")).isEmpty())
    }

    @Test fun lugaresEntre1E8P11() {
        assertEquals("Indica pelo menos 1 lugar.", erros(valido.copy(lugares = "0"))["lugares"])
        assertEquals("O máximo é 8 lugares.", erros(valido.copy(lugares = "9"))["lugares"])
        assertEquals(
            "O máximo é 8 lugares.",
            erros(valido.copy(lugares = "99999999999"))["lugares"]
        )
        listOf("1", "8", " 4 ").forEach {
            assertTrue(it, erros(valido.copy(lugares = it)).isEmpty())
        }
        listOf("2,5", "-1", "três").forEach {
            assertEquals(
                it,
                "Indica um número inteiro de lugares.",
                erros(valido.copy(lugares = it))["lugares"]
            )
        }
    }

    @Test fun naoOfereceMenosDoQueOsOcupadosRfF00208() {
        assertEquals(
            "Já tens 2 lugares ocupados nesta viagem. Não podes oferecer menos.",
            erros(valido.copy(lugares = "1"), ocupados = 2)["lugares"]
        )
        assertTrue(erros(valido.copy(lugares = "2"), ocupados = 2).isEmpty())
    }
}

class DataHoraFormatterTest {
    @Test fun partidaEmHoraDePortugalSegueEmUtc() {
        assertEquals("2026-07-01T08:30:00Z", DataHoraFormatter.paraUtc("01/07/2026", "09:30"))
        assertEquals("2026-12-01T09:30:00Z", DataHoraFormatter.paraUtc("01/12/2026", "09:30"))
        assertNull(DataHoraFormatter.paraUtc("31/02/2026", "09:30"))
        assertNull(DataHoraFormatter.paraUtc("01/12/2026", "25:00"))
    }

    @Test fun fusoDoTelemovelNaoMudaAHoraMostrada() {
        assertEquals(
            CamposPartida("01/07/2026", "09:30"),
            DataHoraFormatter.campos("2026-07-01T08:30:00Z")
        )
        assertEquals("01/12/2026 às 09:30", DataHoraFormatter.formatar("2026-12-01T09:30:00Z"))
        assertEquals("01/12/2026 às 09:30", DataHoraFormatter.formatar("2026-12-01T09:30:00.123Z"))
    }

    @Test fun zonaPodeSerIndicada() = assertEquals(
        "01/12/2026 às 09:30",
        DataHoraFormatter.formatar("2026-12-01T09:30:00Z", ZoneOffset.UTC)
    )

    @Test fun partidaIlegivelMostraOTextoRecebido() {
        assertEquals("ontem", DataHoraFormatter.formatar("ontem"))
        assertNull(DataHoraFormatter.campos("ontem"))
        assertNull(DataHoraFormatter.instanteDe("ontem"))
    }

    @Test fun calendarioUsaMeiaNoiteUtc() {
        val millis = DataHoraFormatter.paraCalendario("25/12/2026")
        assertEquals(Instant.parse("2026-12-25T00:00:00Z").toEpochMilli(), millis)
        assertEquals("25/12/2026", DataHoraFormatter.dataDoCalendario(millis!!))
        assertNull(DataHoraFormatter.paraCalendario(""))
    }

    @Test fun horaComDoisDigitos() = assertEquals("07:05", DataHoraFormatter.hora(7, 5))

    @Test fun textoDaRota() {
        assertEquals("21,5 km · cerca de 25 min", DataHoraFormatter.rota(21.5, 25))
        assertEquals("cerca de 1 h 5 min", DataHoraFormatter.rota(null, 65))
        assertEquals("100,0 km", DataHoraFormatter.rota(100.0, null))
        assertEquals("", DataHoraFormatter.rota(null, null))
    }
}

class ViagemRepositoryTest {
    private val server = MockWebServer()
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private lateinit var viagens: ViagemRepository
    private val id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    private val dados = DadosViagem("Barcelos", "Braga", "2026-12-01T09:30:00Z", 3)
    private val detalhes =
        "{\"id\":\"$id\",\"origem\":\"Barcelos\",\"destino\":\"Braga\"," +
            "\"partidaEm\":\"2026-12-01T09:30:00Z\",\"lugaresDisponiveis\":2," +
            "\"estado\":\"Ativa\"," +
            "\"distanciaKm\":21.5,\"duracaoMinutos\":25,\"condutorId\":\"A\"," +
            "\"condutor\":{\"id\":\"A\",\"nome\":\"Ana\",\"classificacaoMedia\":null}," +
            "\"pontosAdicionais\":0,\"estimativaCusto\":null}"

    @Before fun preparar() {
        server.start(System.getProperty("testServerPort", "0").toInt())
        val base = server.url("/api/v1/")
        val sessao = SessaoManager(Memoria(), base.toString(), scope) { 0 }
        sessao.iniciar(TokenAcesso(token(), 3600))
        viagens = ViagemRepository(ApiFactory.criar(base, sessao))
    }

    @After fun limpar() {
        scope.cancel()
        server.shutdown()
    }

    private fun resposta(codigo: Int, corpo: String = "") =
        server.enqueue(MockResponse().setResponseCode(codigo).setBody(corpo))

    @Test fun criarEnviaOContratoDaApi() = runBlocking {
        resposta(
            201,
            "{\"id\":\"$id\",\"origem\":\"Barcelos\",\"destino\":\"Braga\"," +
                "\"partidaEm\":\"2026-12-01T09:30:00Z\",\"lugaresDisponiveis\":3," +
                "\"distanciaKm\":21.5,\"duracaoMinutos\":25}"
        )
        val r = viagens.criar(dados) as Resultado.Sucesso
        assertEquals(3, r.valor.lugaresDisponiveis)
        assertEquals(21.5, r.valor.distanciaKm!!, 0.001)
        val pedido = server.takeRequest()
        assertEquals("POST", pedido.method)
        assertEquals("/api/v1/viagens", pedido.path)
        val corpo = JsonParser.parseString(pedido.body.readUtf8()).asJsonObject
        assertEquals(setOf("origem", "destino", "partidaEm", "lugares"), corpo.keySet())
        assertEquals("2026-12-01T09:30:00Z", corpo.get("partidaEm").asString)
        assertEquals(3, corpo.get("lugares").asInt)
    }

    @Test fun listaAsViagensAtivas() = runBlocking {
        resposta(
            200,
            "[{\"id\":\"$id\",\"origem\":\"Barcelos\",\"destino\":\"Braga\"," +
                "\"partidaEm\":\"2026-12-01T09:30:00Z\",\"lugaresOferecidos\":3,\"lugaresDisponiveis\":2}]"
        )
        val r = viagens.listarMinhas() as Resultado.Sucesso
        assertEquals(3, r.valor.single().lugaresOferecidos)
        assertEquals("/api/v1/viagens/minhas", server.takeRequest().path)
    }

    @Test fun detalheComCamposANull() = runBlocking {
        resposta(
            200,
            detalhes.replace(
                "21.5",
                "null"
            ).replace("\"duracaoMinutos\":25", "\"duracaoMinutos\":null")
        )
        val r = viagens.obterDetalhe(id) as Resultado.Sucesso
        assertEquals("Ativa", r.valor.estado)
        assertEquals("A", r.valor.condutorId)
        assertNull(r.valor.distanciaKm)
        assertNull(r.valor.duracaoMinutos)
        assertEquals("/api/v1/viagens/$id", server.takeRequest().path)
    }

    @Test fun alterarAceitaRespostaSemCorpo() = runBlocking {
        resposta(204)
        assertEquals(Resultado.Sucesso(Unit), viagens.alterar(id, dados))
        val pedido = server.takeRequest()
        assertEquals("PUT", pedido.method)
        assertEquals("/api/v1/viagens/$id", pedido.path)
    }

    @Test fun indisponibilizarSemCorpoNemEliminacao() = runBlocking {
        resposta(204)
        assertEquals(Resultado.Sucesso(Unit), viagens.indisponibilizar(id))
        val pedido = server.takeRequest()
        assertEquals("POST", pedido.method)
        assertEquals("/api/v1/viagens/$id/indisponibilizar", pedido.path)
    }

    @Test fun falhaDoServicoDeMapasTrazAMensagemDaApiRfF00217() = runBlocking {
        resposta(503, "{\"erro\":\"Serviço de mapas indisponível.\"}")
        val r = viagens.criar(dados) as Resultado.Falha
        assertEquals("Serviço de mapas indisponível.", r.mensagem)
        assertEquals(503, r.status)
    }

    @Test fun conflitoTrazOEstado() = runBlocking {
        resposta(409, "{\"erro\":\"A viagem já não está ativa.\"}")
        val r = viagens.alterar(id, dados) as Resultado.Falha
        assertEquals(409, r.status)
        assertEquals("A viagem já não está ativa.", r.mensagem)
    }
}
