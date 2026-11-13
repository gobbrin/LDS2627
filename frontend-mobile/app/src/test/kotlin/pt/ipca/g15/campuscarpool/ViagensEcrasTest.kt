package pt.ipca.g15.campuscarpool

import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTextClearance
import androidx.compose.ui.test.performTextInput
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import pt.ipca.g15.campuscarpool.domain.model.CamposViagem
import pt.ipca.g15.campuscarpool.domain.model.ViagemDoCondutor
import pt.ipca.g15.campuscarpool.ui.viagens.CriarViagemScreen
import pt.ipca.g15.campuscarpool.ui.viagens.EstadoMinhasViagens
import pt.ipca.g15.campuscarpool.ui.viagens.EstadoViagemForm
import pt.ipca.g15.campuscarpool.ui.viagens.GerirViagemScreen
import pt.ipca.g15.campuscarpool.ui.viagens.MinhasViagensScreen

@RunWith(RobolectricTestRunner::class)
class ViagensEcrasTest {
    @get:Rule val compose = createComposeRule()

    private val aGerir =
        EstadoViagemForm(viagem = detalhesViagemDeTeste, oferecidos = 3, versao = 1)

    private fun mostrar(conteudo: @Composable () -> Unit) =
        compose.setContent { MaterialTheme { conteudo() } }

    private fun clicar(texto: String) =
        compose.onNodeWithText(texto).performScrollTo().performClick()

    private fun existe(texto: String) = compose.onNodeWithText(texto).assertExists()

    private fun escrever(etiqueta: String, texto: String) {
        compose.onNodeWithTag(etiqueta).performScrollTo().performTextClearance()
        compose.onNodeWithTag(etiqueta).performTextInput(texto)
    }

    @Composable private fun Lista(
        estado: EstadoMinhasViagens,
        repetir: () -> Unit = {},
        criar: () -> Unit = {},
        gerir: (String) -> Unit = {}
    ) = MinhasViagensScreen(estado, repetir, criar, gerir, {})

    @Composable private fun Gerir(
        estado: EstadoViagemForm,
        alterar: (CamposViagem) -> Unit = {},
        indisponibilizar: () -> Unit = {}
    ) = GerirViagemScreen(estado, {}, alterar, indisponibilizar, {})

    @Test fun listaACarregar() {
        mostrar { Lista(EstadoMinhasViagens(aCarregar = true)) }
        existe("A carregar as tuas viagens…")
    }

    @Test fun listaVaziaOfereceCriar() {
        var criou = false
        mostrar { Lista(EstadoMinhasViagens(carregada = true), criar = { criou = true }) }
        existe("Ainda não tens viagens ativas.")
        clicar("Criar viagem")
        assertEquals(true, criou)
    }

    @Test fun erroNaListaPermiteRepetir() {
        var repetiu = false
        mostrar { Lista(EstadoMinhasViagens(erro = "Sem resposta."), repetir = { repetiu = true }) }
        existe("Sem resposta.")
        clicar("Tentar novamente")
        assertEquals(true, repetiu)
    }

    @Test fun listaMostraEstadoELugaresEAbreAGestao() {
        var gerida: String? = null
        val viagens: List<ViagemDoCondutor> = listOf(viagemDoCondutorDeTeste)
        mostrar {
            Lista(EstadoMinhasViagens(carregada = true, viagens = viagens), gerir = { gerida = it })
        }
        existe("Barcelos → Braga")
        existe("Ativa")
        existe("01/12/2099 às 09:30")
        existe("Lugares disponíveis: 2 de 3")
        clicar("Gerir viagem")
        assertEquals(ID_VIAGEM, gerida)
    }

    @Test fun criarEnviaOQueFoiPreenchido() {
        var enviado: CamposViagem? = null
        mostrar { CriarViagemScreen(EstadoViagemForm(), { enviado = it }, {}) }
        escrever("viagem-origem", "Barcelos")
        escrever("viagem-destino", "Braga")
        escrever("viagem-data", "25/12/2026")
        escrever("viagem-hora", "09:30")
        escrever("viagem-lugares", "3")
        compose.onNodeWithTag("viagem-guardar").performScrollTo().performClick()
        assertEquals(CamposViagem("Barcelos", "Braga", "25/12/2026", "09:30", "3"), enviado)
    }

    @Test fun errosAparecemJuntoDosCampos() {
        val erros = mapOf("origem" to "Indica a origem.", "lugares" to "O máximo é 8 lugares.")
        mostrar {
            CriarViagemScreen(EstadoViagemForm(errosCampos = erros, erro = "Mapas em baixo."), {
            }, {})
        }
        existe("Indica a origem.")
        existe("O máximo é 8 lugares.")
        existe("Mapas em baixo.")
    }

    @Test fun aGuardarDesativaOFormulario() {
        mostrar { CriarViagemScreen(EstadoViagemForm(aGuardar = true), {}, {}) }
        compose.onNodeWithTag("viagem-guardar").assertIsNotEnabled()
        existe("A criar a viagem…")
    }

    @Test fun gerirMostraOsDadosEPreencheOFormulario() {
        var alterado: CamposViagem? = null
        mostrar { Gerir(aGerir, alterar = { alterado = it }) }
        existe("Estado: Ativa")
        existe("Lugares disponíveis: 2 de 3")
        existe("Rota: 21,5 km · cerca de 25 min")
        compose.onNodeWithTag("viagem-guardar").performScrollTo().performClick()
        assertEquals(CamposViagem("Barcelos", "Braga", "01/12/2099", "09:30", "3"), alterado)
    }

    @Test fun indisponibilizarPedeConfirmacao() {
        var pedidos = 0
        mostrar { Gerir(aGerir, indisponibilizar = { pedidos++ }) }
        compose.onNodeWithTag("viagem-indisponibilizar").performScrollTo().performClick()
        assertEquals(0, pedidos)
        existe("Sim, indisponibilizar")
        compose.onNodeWithTag("viagem-confirmar-indisponibilizar").performScrollTo().performClick()
        assertEquals(1, pedidos)
    }

    @Test fun recusarAConfirmacaoNaoIndisponibiliza() {
        var pedidos = 0
        mostrar { Gerir(aGerir, indisponibilizar = { pedidos++ }) }
        compose.onNodeWithTag("viagem-indisponibilizar").performScrollTo().performClick()
        clicar("Não")
        assertEquals(0, pedidos)
        existe("Indisponibilizar")
    }

    @Test fun viagemIndisponibilizadaSoSeConsulta() {
        val estado = aGerir.copy(viagem = detalhesViagemDeTeste.copy(estado = "Indisponibilizada"))
        mostrar { Gerir(estado) }
        existe("Estado: Indisponibilizada")
        existe("Esta viagem já não pode ser alterada.")
        compose.onNodeWithTag("viagem-guardar").assertDoesNotExist()
        compose.onNodeWithTag("viagem-indisponibilizar").assertDoesNotExist()
    }

    @Test fun naoHaOpcaoDeEliminar() {
        mostrar { Gerir(aGerir) }
        compose.onNodeWithText("Eliminar", substring = true).assertDoesNotExist()
    }

    @Test fun viagemNaoEncontradaNaoRepete() {
        mostrar { Gerir(EstadoViagemForm(erroCarregar = "Viagem não encontrada.")) }
        existe("Viagem não encontrada.")
        compose.onNodeWithText("Tentar novamente").assertDoesNotExist()
        existe("Voltar às minhas viagens")
    }

    @Test fun gerirACarregar() {
        mostrar { Gerir(EstadoViagemForm(aCarregar = true)) }
        existe("A carregar a viagem…")
    }
}
