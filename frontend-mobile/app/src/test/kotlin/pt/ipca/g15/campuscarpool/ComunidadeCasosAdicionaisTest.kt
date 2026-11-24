package pt.ipca.g15.campuscarpool

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import pt.ipca.g15.campuscarpool.ui.comunidade.HistoricoScreen
import pt.ipca.g15.campuscarpool.ui.comunidade.HistoricoVm
import pt.ipca.g15.campuscarpool.ui.comunidade.PesquisaVm
import pt.ipca.g15.campuscarpool.ui.comunidade.MensagensVm
import pt.ipca.g15.campuscarpool.ui.comunidade.ParticipacoesVm
import pt.ipca.g15.campuscarpool.ui.comunidade.NotificacoesVm
import pt.ipca.g15.campuscarpool.ui.comunidade.texto
import retrofit2.Response

private fun objetoJson(valor: String): JsonObject = JsonParser.parseString(valor).asJsonObject

/** Testes adicionais I092: sem rede, API controlada e ramos de validação. */
@OptIn(ExperimentalCoroutinesApi::class)
class ComunidadeCasosAdicionaisTest {
    private val dispatcher = UnconfinedTestDispatcher()

    @Before fun iniciar() { Dispatchers.setMain(dispatcher) }
    @After fun terminar() { Dispatchers.resetMain() }

    @Test fun historicoCarregaDuasPerspetivas() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        fake.respostas["historico"] = Response.success(
            objetoJson("""{"viagensComoCondutor":[{"id":"v1"}],"viagensComoPassageiro":[{"id":"v2"}]}""")
        )
        val vm = HistoricoVm(api = fake.api)
        vm.carregar()
        runCurrent()
        assertEquals(1, fake.vezes("historico"))
        assertEquals("v1", vm.historico?.getAsJsonArray("viagensComoCondutor")?.get(0)?.asJsonObject?.get("id")?.asString)
        assertEquals("v2", vm.historico?.getAsJsonArray("viagensComoPassageiro")?.get(0)?.asJsonObject?.get("id")?.asString)
    }

    @Test fun historicoDoCondutorCarregaPassageirosEValidaClassificacao() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        fake.respostas["passageiros"] = Response.success(
            listOf(objetoJson("""{"passageiroId":"B","nome":"Rui"}"""))
        )
        val vm = HistoricoVm(api = fake.api)
        vm.preparar(objetoJson("""{"id":"v1"}"""), true)
        runCurrent()
        assertEquals("v1", vm.viagem)
        assertEquals("B" to "Rui", vm.pessoas.single())
        vm.avaliar("B", 0, false)
        vm.avaliar("B", 6, false)
        vm.avaliar("desconhecido", 5, false)
        assertEquals(0, fake.vezes("avaliar"))
        vm.avaliar("B", 5, false)
        runCurrent()
        assertEquals(1, fake.vezes("avaliar"))
        val dados = fake.ultimo("avaliar").first() as JsonObject
        assertEquals("v1", dados.texto("viagemId"))
        assertEquals("B", dados.texto("avaliadoId"))
        assertEquals(5, dados.get("classificacao").asInt)
        assertEquals("Avaliação guardada.", vm.aviso)
    }

    @Test fun historicoDoPassageiroPermiteAlteracaoDeAvaliacao() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        fake.respostas["detalhe"] = Response.success(
            objetoJson("""{"id":"v2","condutorId":"A","condutor":{"nome":"Ana"}}""")
        )
        val vm = HistoricoVm(api = fake.api)
        vm.preparar(objetoJson("""{"id":"v2"}"""), false)
        runCurrent()
        assertEquals("A" to "Ana", vm.pessoas.single())
        vm.avaliar("A", 4, true)
        runCurrent()
        assertEquals(1, fake.vezes("alterarAvaliacao"))
        assertEquals("A", fake.ultimo("alterarAvaliacao").first())
        assertEquals(4, (fake.ultimo("alterarAvaliacao")[1] as JsonObject).get("classificacao").asInt)
        assertEquals("Avaliação atualizada.", vm.aviso)
    }

    @Test fun historicoSemSelecionarViagemNaoAceitaAvaliacao() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        val vm = HistoricoVm(api = fake.api)
        vm.avaliar("A", 4, false)
        runCurrent()
        assertEquals(0, fake.vezes("avaliar"))
        assertEquals(0, fake.vezes("alterarAvaliacao"))
    }

    @Test fun pesquisaComFalhaNaRotaPreservaDetalhe() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        fake.respostas["detalhe"] = Response.success(objetoJson("""{"id":"v1","origem":"Porto"}"""))
        fake.respostas["rota"] = erroDaApi<JsonObject>(503, "Rota temporariamente indisponível")
        val vm = PesquisaVm(api = fake.api)
        vm.abrir("v1")
        runCurrent()
        assertEquals("v1", vm.detalhe?.texto("id"))
        assertEquals(null, vm.rota)
        assertTrue(vm.erro.contains("Rota temporariamente indisponível"))
    }

    @Test fun pesquisarSemFiltrosEnviaSoPaginacao() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        val vm = PesquisaVm(api = fake.api)
        vm.procurar("  ", "", "", "")
        runCurrent()
        val filtros = fake.ultimo("pesquisar").first() as Map<*, *>
        assertFalse(filtros.containsKey("origem"))
        assertFalse(filtros.containsKey("destino"))
        assertEquals("1", filtros["pagina"])
        assertEquals("20", filtros["tamanho"])
    }

    @Test fun notificacoesFalhaAoMarcarTodasMantemErroVisivel() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        fake.respostas["lerTodas"] = erroDaApi<Unit>(503, "Indisponível")
        val vm = NotificacoesVm(api = fake.api)
        vm.todas()
        runCurrent()
        assertTrue(vm.erro.isNotBlank())
        assertEquals(1, fake.vezes("lerTodas"))
    }

    @Test fun mensagemLimiteMilCaracteresEEnviadaSemEspacosLaterais() = runTest(dispatcher) {
        val fake = ServicoSimulado()
        val vm = MensagensVm(api = fake.api, utilizadorId = { "A" })
        vm.abrir("B", "Rui")
        vm.enviar(" " + "x".repeat(998) + " ") {}
        runCurrent()
        assertEquals(1, fake.vezes("mensagem"))
        assertEquals(998, (fake.ultimo("mensagem").first() as JsonObject).texto("conteudo").length)
    }
}

@RunWith(RobolectricTestRunner::class)
class HistoricoEcraAdicionalTest {
    @get:Rule val compose = createComposeRule()

    @Test fun historicoListaViagensConcluidasEIniciaSelecao() {
        val fake = ServicoSimulado()
        fake.respostas["historico"] = Response.success(
            objetoJson("""{"viagensComoCondutor":[{"id":"v1","origem":"Porto","destino":"Braga","estado":"Concluida","partidaEm":"2026-11-01T10:00:00Z"}],"viagensComoPassageiro":[],"pedidosEfetuados":[],"pedidosRecebidos":[],"notificacoes":[],"avisos":[]}""")
        )
        fake.respostas["passageiros"] = Response.success(
            listOf(objetoJson("""{"passageiroId":"B","nome":"Rui"}"""))
        )
        val vm = HistoricoVm(api = fake.api)
        compose.setContent {
            MaterialTheme {
                Column(Modifier.verticalScroll(rememberScrollState())) { HistoricoScreen(vm) }
            }
        }
        compose.waitUntil(5000) { vm.historico != null }
        compose.onNodeWithText("Histórico e avaliações").assertExists()
        compose.onNodeWithText("Viagens como condutor").assertExists()
        compose.onNodeWithText("Porto → Braga · Concluida").assertExists()
        compose.onNodeWithText("Avaliar participante").performScrollTo().performClick()
        compose.waitUntil(5000) { vm.pessoas.isNotEmpty() }
        compose.onNodeWithText("Selecionar participante").assertExists()
        compose.onNodeWithText("Guardar").assertIsNotEnabled()
    }
}
