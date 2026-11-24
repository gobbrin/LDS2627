package pt.ipca.g15.campuscarpool

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTextClearance
import androidx.compose.ui.test.performTextInput
import androidx.test.core.app.ApplicationProvider
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import pt.ipca.g15.campuscarpool.data.remote.ComunidadeApi
import pt.ipca.g15.campuscarpool.di.AppContainer
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso
import pt.ipca.g15.campuscarpool.ui.comunidade.AvisosScreen
import pt.ipca.g15.campuscarpool.ui.comunidade.AvisosVm
import pt.ipca.g15.campuscarpool.ui.comunidade.HistoricoScreen
import pt.ipca.g15.campuscarpool.ui.comunidade.HistoricoVm
import pt.ipca.g15.campuscarpool.ui.comunidade.MensagensScreen
import pt.ipca.g15.campuscarpool.ui.comunidade.MensagensVm
import pt.ipca.g15.campuscarpool.ui.comunidade.NotificacoesScreen
import pt.ipca.g15.campuscarpool.ui.comunidade.NotificacoesVm
import pt.ipca.g15.campuscarpool.ui.comunidade.ParticipacoesScreen
import pt.ipca.g15.campuscarpool.ui.comunidade.ParticipacoesVm
import pt.ipca.g15.campuscarpool.ui.comunidade.PesquisaScreen
import pt.ipca.g15.campuscarpool.ui.comunidade.PesquisaVm
import pt.ipca.g15.campuscarpool.ui.comunidade.corpo
import pt.ipca.g15.campuscarpool.ui.comunidade.dataPt
import pt.ipca.g15.campuscarpool.ui.comunidade.lista
import pt.ipca.g15.campuscarpool.ui.comunidade.numero
import pt.ipca.g15.campuscarpool.ui.comunidade.texto
import retrofit2.Response

private fun json(texto: String) = JsonParser.parseString(texto).asJsonObject

/** Respostas controladas: estes testes exercitam UI/estado; não substituem a API real. */
private class ComunidadeControlada : ComunidadeApi {
    val chamadas = mutableListOf<String>()
    var enviado: JsonObject? = null
    var filtros: Map<String, String> = emptyMap()
    var falharMensagem = false
    var lidas = false
    var detalhe =
        json(
            """{"id":"v1","origem":"Porto","destino":"Campus","condutorId":"CD","estado":"Ativa","lugaresDisponiveis":2,"partidaEm":"2026-12-15T14:00:00Z","distanciaKm":25,"duracaoMinutos":30,"condutor":{"nome":"Condutor","classificacaoMedia":4.5},"estimativaCusto":{"custoPorParticipante":1.25,"moeda":"EUR","custoTotalEstimado":2.5}}"""
        )
    var historico =
        json(
            """{"pedidosEfetuados":[{"id":"p1","origem":"Porto","destino":"Campus","estado":"Pendente"}],"viagensComoCondutor":[{"id":"v1","origem":"Porto","destino":"Campus","estado":"Concluida","partidaEm":"2026-12-15T14:00:00Z"}],"viagensComoPassageiro":[],"pedidosRecebidos":[],"notificacoes":[{"titulo":"Pedido recebido"}],"avisos":[{"conteudo":"Encontro no portão"}]}"""
        )
    override suspend fun pesquisar(filtro: Map<String, String>): Response<JsonObject> {
        filtros = filtro
        chamadas += "pesquisa"
        return Response.success(json("""{"total":21,"viagens":[$detalhe]}"""))
    }
    override suspend fun detalhe(id: String): Response<JsonObject> {
        chamadas += "detalhe:$id"
        return Response.success(detalhe)
    }
    override suspend fun rota(id: String): Response<JsonObject> {
        chamadas += "rota:$id"
        return erroDaApi(503, "Serviço de mapas indisponível.")
    }
    override suspend fun historico() = Response.success(historico)
    override suspend fun pedir(dados: JsonObject): Response<JsonObject> {
        enviado = dados
        chamadas += "pedido"
        return Response.success(json("""{"id":"p2"}"""))
    }
    override suspend fun pendentes(id: String) = Response.success(
        listOf(json("""{"id":"p1","nome":"Passageiro","recolha":"Porto","destino":"Campus"}"""))
    )
    override suspend fun passageiros(id: String) = Response.success(
        listOf(json("""{"id":"p2","passageiroId":"PA","nome":"Ana"}"""))
    )
    override suspend fun simular(id: String, dados: JsonObject): Response<JsonObject> {
        chamadas += "simular"
        return Response.success(
            json(
                """{"pedidoId":"p1","distanciaOriginalKm":25,"duracaoOriginalMinutos":30,"distanciaSimuladaKm":27,"duracaoSimuladaMinutos":33,"desvioDistanciaKm":2,"desvioDuracaoMinutos":3,"ordemOriginal":["Porto","Campus"],"ordemSimulada":["Porto","Recolha","Campus"]}"""
            )
        )
    }
    override suspend fun decidir(id: String, acao: String): Response<Unit> {
        chamadas += "$acao:$id"
        return Response.success(Unit)
    }
    override suspend fun conversas() = Response.success(
        listOf(
            json(
                """{"outroUtilizadorId":"CD","outroUtilizadorNome":"Condutor","ultimaMensagem":"Olá"}"""
            )
        )
    )
    override suspend fun mensagens(id: String) = Response.success(
        listOf(
            json(
                """{"id":"m1","remetenteId":"CD","conteudo":"<script>não executar</script>","enviadoEm":"2027-07-15T13:00:00Z"}"""
            )
        )
    )
    override suspend fun mensagem(dados: JsonObject): Response<JsonObject> {
        enviado = dados
        chamadas += "mensagem"
        return if (falharMensagem) {
            erroDaApi(503, "Serviço indisponível.")
        } else {
            Response.success(json("""{"id":"m2"}"""))
        }
    }
    override suspend fun avisos(id: String) = Response.success(
        listOf(
            json(
                """{"conteudo":"Encontro no portão","criadoEm":"2026-12-15T14:00:00Z"}"""
            )
        )
    )
    override suspend fun aviso(id: String, dados: JsonObject): Response<JsonObject> {
        enviado = dados
        chamadas += "aviso:$id"
        return Response.success(json("""{"id":"a1"}"""))
    }
    override suspend fun notificacoes(pagina: Int, tamanho: Int): Response<JsonObject> {
        chamadas += "caixa:$pagina"
        return Response.success(
            json(
                """{"pagina":$pagina,"totalPaginas":2,"naoLidas":${if (lidas) 0 else 1},"itens":[{"id":"n1","titulo":"Pedido recebido","conteudo":"Novo passageiro","criadaEm":"2026-12-15T14:00:00Z","lidaEm":${if (lidas) "\"2026-12-15T15:00:00Z\"" else "null"}}]}"""
            )
        )
    }
    override suspend fun ler(id: String): Response<Unit> {
        chamadas += "ler:$id"
        lidas = true
        return Response.success(Unit)
    }
    override suspend fun lerTodas(): Response<JsonObject> {
        chamadas += "lerTodas"
        lidas = true
        return Response.success(json("{}"))
    }
    override suspend fun dispositivo(dados: JsonObject) = Response.success(Unit)
    override suspend fun avaliar(dados: JsonObject): Response<JsonObject> {
        enviado = dados
        chamadas += "avaliar"
        return Response.success(json("""{"id":"a1"}"""))
    }
    override suspend fun alterarAvaliacao(id: String, dados: JsonObject): Response<Unit> {
        enviado = dados
        chamadas += "alterar:$id"
        return Response.success(Unit)
    }
}

@RunWith(RobolectricTestRunner::class)
class ComunidadeEcrasTest {
    @get:Rule val compose = createComposeRule()
    private lateinit var api: ComunidadeControlada
    private lateinit var app: AppContainer

    @Before fun preparar() {
        api = ComunidadeControlada()
        app = AppContainer(ApplicationProvider.getApplicationContext(), api, SessaoEmMemoria())
        app.sessao.terminar()
    }

    @After fun limpar() = app.sessao.terminar()
    private fun mostrar(conteudo: @Composable () -> Unit) = compose.setContent {
        MaterialTheme { Column(Modifier.verticalScroll(rememberScrollState())) { conteudo() } }
    }
    private fun clicar(t: String) {
        val node = compose.onNodeWithText(t)
        if (t !in listOf("Confirmar", "Voltar")) node.performScrollTo()
        node.performClick()
    }
    private fun existe(t: String) = compose.onNodeWithText(t).assertExists()
    private fun escrever(rotulo: String, valor: String) {
        compose.onNodeWithText(rotulo).performScrollTo().performTextClearance()
        compose.onNodeWithText(rotulo).performTextInput(valor)
    }
    private fun condutor() {
        app.sessao.iniciar(
            TokenAcesso(tokenDeTeste("CD", System.currentTimeMillis() / 1000 + 3600), 3600)
        )
    }

    @Test fun pesquisaPaginaEDetalheSobrevivemAFalhaDeMapas() {
        val vm = PesquisaVm(app)
        mostrar { PesquisaScreen(vm) }
        escrever("Origem", " Porto ")
        escrever("Destino", "Campus")
        escrever("Data (AAAA-MM-DD)", "2026-12-15")
        escrever("Hora mínima (HH:mm)", "14:00")
        clicar("Pesquisar")
        existe("21 viagens encontradas")
        assertEquals("Porto", api.filtros["origem"])
        clicar("Seguinte")
        assertEquals("2", api.filtros["pagina"])
        clicar("Anterior")
        clicar("Consultar detalhe")
        existe("Serviço de mapas indisponível.")
        existe("25 km · 30 min")
        assertEquals("v1", vm.detalhe?.texto("id"))
        assertFalse(vm.ocupado)
    }

    @Test fun passageiroEnviaPontosECancelaApenasAposConfirmacao() {
        val vm = ParticipacoesVm(app)
        mostrar { ParticipacoesScreen("v1", vm) }
        escrever("Recolha (opcional)", " Praça ")
        escrever("Destino (opcional)", " Campus ")
        clicar("Enviar pedido")
        assertEquals("Praça", api.enviado?.texto("recolha"))
        clicar("Cancelar")
        clicar("Voltar")
        assertFalse(api.chamadas.contains("cancelar:p1"))
        clicar("Cancelar")
        clicar("Confirmar")
        assertTrue(api.chamadas.contains("cancelar:p1"))
    }

    @Test fun condutorSimulaAntesDeAceitarERejeitaComConfirmacao() {
        condutor()
        val vm = ParticipacoesVm(app)
        mostrar { ParticipacoesScreen("v1", vm) }
        compose.onNodeWithText("Aceitar").assertIsNotEnabled()
        clicar("Simular")
        existe("Original: 25 km / 30 min")
        existe("Simulada: 27 km / 33 min")
        clicar("Aceitar")
        assertFalse(api.chamadas.contains("aceitar:p1"))
        clicar("Confirmar")
        assertTrue(api.chamadas.indexOf("simular") < api.chamadas.indexOf("aceitar:p1"))
        clicar("Rejeitar")
        clicar("Confirmar")
        assertTrue(api.chamadas.contains("rejeitar:p1"))
        clicar("Cancelar participação")
        clicar("Confirmar")
        assertTrue(api.chamadas.contains("cancelar:p2"))
    }

    @Test fun viagemSemLugaresBloqueiaPedido() {
        api.detalhe.addProperty("lugaresDisponiveis", 0)
        val vm = ParticipacoesVm(app)
        mostrar { ParticipacoesScreen("v1", vm) }
        compose.onNodeWithText("Enviar pedido").assertIsNotEnabled()
    }

    @Test fun mensagemETextoLiteralEMantidaQuandoFalha() {
        val vm = MensagensVm(app)
        mostrar { MensagensScreen(vm = vm) }
        clicar("Condutor · Olá")
        existe("<script>não executar</script>")
        existe("15/07/2027 14:00")
        escrever("Mensagem", "Chego às 14h")
        api.falharMensagem = true
        clicar("Enviar")
        existe("Serviço indisponível.")
        existe("Chego às 14h")
        api.falharMensagem = false
        clicar("Enviar")
        assertEquals("CD", api.enviado?.texto("destinatarioId"))
        assertEquals("Chego às 14h", api.enviado?.texto("conteudo"))
        compose.onNodeWithText("Enviar").assertIsNotEnabled()
        clicar("Atualizar mensagens")
        clicar("Atualizar conversas")
    }

    @Test fun contactoDiretoEValidacoesDeMensagem() {
        val vm = MensagensVm(app)
        mostrar { MensagensScreen("CD", vm) }
        compose.runOnIdle {
            vm.enviar(" ") { error("Não limpar conteúdo inválido") }
            vm.enviar("a".repeat(1001)) { error("Não limpar conteúdo inválido") }
        }
        assertFalse(api.chamadas.contains("mensagem"))
    }

    @Test fun condutorPublicaAvisoEPassageiroConsultaTexto() {
        val vm = AvisosVm(app)
        mostrar { AvisosScreen("v1", true, vm) }
        existe("Encontro no portão")
        escrever("Aviso aos passageiros aceites", " Vamos sair ")
        clicar("Publicar")
        assertEquals("Vamos sair", api.enviado?.texto("conteudo"))
        compose.onNodeWithText("Publicar").assertIsNotEnabled()
        clicar("Atualizar avisos")
        compose.runOnIdle {
            api.chamadas.clear()
            vm.enviar("v1", "a".repeat(101)) { error("Não aceitar aviso extenso") }
        }
        assertFalse(api.chamadas.contains("aviso:v1"))
    }

    @Test fun passageiroNaoPodePublicarAviso() {
        val vm = AvisosVm(app)
        mostrar { AvisosScreen("v1", false, vm) }
        existe("Encontro no portão")
        compose.onNodeWithText("Publicar").assertDoesNotExist()
    }

    @Test fun caixaPaginaMarcaIndividualETodasComoLidas() {
        val vm = NotificacoesVm(app)
        mostrar { NotificacoesScreen(vm) }
        existe("1 por ler")
        clicar("Seguinte")
        assertTrue(api.chamadas.contains("caixa:2"))
        clicar("Anterior")
        clicar("Marcar como lida")
        existe("0 por ler")
        clicar("Marcar todas como lidas")
        assertTrue(api.chamadas.contains("lerTodas"))
        clicar("Atualizar")
        clicar("Desativar push")
        existe("Push desativado neste dispositivo.")
        assertFalse(app.push.permitido())
    }

    @Test fun historicoPermiteCriarEAlterarAvaliacaoValida() {
        val vm = HistoricoVm(app)
        mostrar { HistoricoScreen(vm) }
        existe("Viagens como condutor")
        existe("15/12/2026 14:00")
        clicar("Avaliar participante")
        compose.onNodeWithText("Guardar").assertIsNotEnabled()
        clicar("Ana")
        escrever("Classificação de 1 a 5", "6")
        compose.onNodeWithText("Guardar").assertIsNotEnabled()
        escrever("Classificação de 1 a 5", "4")
        clicar("Guardar")
        existe("Avaliação guardada.")
        assertEquals("PA", api.enviado?.texto("avaliadoId"))
        clicar("Modo: nova avaliação")
        clicar("Guardar")
        existe("Avaliação atualizada.")
        assertTrue(api.chamadas.contains("alterar:PA"))
        clicar("Atualizar")
    }

    @Test fun historicoDoPassageiroSelecionaCondutorEDefendeParametros() {
        api.historico =
            json(
                """{"viagensComoCondutor":[],"viagensComoPassageiro":[{"id":"v1","origem":"Porto","destino":"Campus","estado":"Concluida","partidaEm":"2026-12-15T14:00:00Z"}]}"""
            )
        val vm = HistoricoVm(app)
        mostrar { HistoricoScreen(vm) }
        clicar("Avaliar participante")
        clicar("Condutor")
        compose.runOnIdle {
            vm.avaliar("CD", 0, false)
            vm.avaliar("terceiro", 5, false)
        }
        assertFalse(api.chamadas.contains("avaliar"))
        clicar("Guardar")
        assertEquals("CD", api.enviado?.texto("avaliadoId"))
    }

    @Test fun datasDePortugalEJsonParcialNaoDependemDoFusoDoDispositivo() {
        assertEquals("15/12/2026 14:00", dataPt("2026-12-15T14:00:00Z"))
        assertEquals("15/07/2027 14:00", dataPt("2027-07-15T13:00:00Z"))
        assertEquals("indisponível", dataPt("indisponível"))
        val parcial = json("""{"a":null,"b":{},"c":[1,{},null],"d":"texto"}""")
        assertEquals("", parcial.texto("a"))
        assertEquals("", parcial.texto("b"))
        assertEquals(null, parcial.numero("d"))
        assertEquals(1, parcial.lista("c").size)
        assertEquals(true, corpo("aceite" to true).get("aceite").asBoolean)
    }
}
