package pt.ipca.g15.campuscarpool

import android.content.Context
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import androidx.compose.ui.test.hasTestTag
import androidx.compose.ui.test.hasText
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTextInput
import androidx.test.core.app.ApplicationProvider
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.Shadows
import org.robolectric.shadows.ShadowNetworkCapabilities
import pt.ipca.g15.campuscarpool.data.remote.ConnectivityMonitor
import pt.ipca.g15.campuscarpool.domain.model.LigacaoApp
import pt.ipca.g15.campuscarpool.domain.model.Sessao
import pt.ipca.g15.campuscarpool.ui.AppNavigation

@RunWith(RobolectricTestRunner::class)
class NavegacaoTest {
    @get:Rule val compose = createComposeRule()
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private val idOutro = "3fa85f64-5717-4562-b3fc-2c963f66afa6"

    @After fun limpar() = scope.cancel()

    private fun cenario(guardada: Sessao? = null) = Cenario(scope, guardada, agora = { 0 }).also {
        compose.setContent { AppNavigation(it.login, it.online, it.fabrica) }
    }

    private fun esperar(texto: String) = compose.waitUntil(5_000) {
        compose.onAllNodes(hasText(texto)).fetchSemanticsNodes().isNotEmpty()
    }

    private fun clicar(texto: String) =
        compose.onNodeWithText(texto).performScrollTo().performClick()

    private fun sessao() = Sessao(tokenDeTeste(), "A", 3_600_000, BACKEND)

    private fun entrar() {
        clicar("Entrar")
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        compose.onNodeWithTag("login-submit").performClick()
        esperar("Perfil")
    }

    @Test fun visitanteEntraConsultaPerfilETermina() {
        val c = cenario()
        entrar()
        clicar("Perfil")
        esperar("Ana")
        clicar("Voltar")
        clicar("Terminar sessão")
        compose.waitUntil(5_000) { c.login.sessao.value == null }
        compose.waitForIdle()
        compose.onNodeWithText("Entrar").assertExists()
        compose.onNodeWithText("Sessão terminada.").assertExists()
        assertNull(c.store.valor)
    }

    @Test fun loginVoltaAoInicio() {
        cenario()
        clicar("Entrar")
        clicar("Voltar")
        compose.onNodeWithText("O caminho para o campus faz-se em companhia.").assertExists()
    }

    @Test fun avisoPodeSerFechado() {
        val c = cenario()
        c.login.mostrarAviso("Perfil atualizado.")
        esperar("Perfil atualizado.")
        compose.onNodeWithText("Fechar mensagem").performClick()
        compose.onNodeWithText("Perfil atualizado.").assertDoesNotExist()
    }

    @Test fun semRedeMostraIndicacao() {
        val c = cenario()
        c.online.value = false
        compose.waitForIdle()
        compose.onNodeWithText("Sem ligação à Internet. Verifica a rede e tenta novamente.")
            .assertExists()
    }

    @Test fun registoConcluidoLevaAConfirmar() {
        cenario()
        clicar("Criar conta")
        compose.onNodeWithTag("registo-nome").performTextInput("Ana")
        compose.onNodeWithTag("registo-email").performTextInput("ana@ipca.pt")
        compose.onNodeWithTag("registo-password").performTextInput("Segredo123")
        compose.onNodeWithTag("registo-confirmacao").performTextInput("Segredo123")
        compose.onNodeWithTag("registo-submit").performScrollTo().performClick()
        esperar("Verifica o email.")
        clicar("Já tenho a ligação")
        compose.onNodeWithTag("confirmacao-texto").assertExists()
    }

    @Test fun contaPorConfirmarAbreOReenvioComOEmail() {
        val c = Cenario(scope, null, { loginResposta = erroDaApi(403, "Conta por confirmar.") })
        compose.setContent { AppNavigation(c.login, c.online, c.fabrica) }
        clicar("Entrar")
        compose.onNodeWithTag("login-email").performTextInput("ana@ipca.pt")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        compose.onNodeWithTag("login-submit").performClick()
        esperar("Conta por confirmar.")
        clicar("Pedir nova ligação de confirmação")
        esperar("ana@ipca.pt")
        compose.onNodeWithTag("reenvio-submit").assertExists()
    }

    @Test fun erroDaEntradaDesapareceAoSair() {
        val c = Cenario(scope, null, { loginResposta = erroDaApi(401, "Credenciais inválidas.") })
        compose.setContent { AppNavigation(c.login, c.online, c.fabrica) }
        clicar("Entrar")
        compose.onNodeWithTag("login-email").performTextInput("ana@ipca.pt")
        compose.onNodeWithTag("login-password").performTextInput("Errada123")
        compose.onNodeWithTag("login-submit").performClick()
        esperar("Credenciais inválidas.")
        clicar("Voltar")
        compose.waitForIdle()
        clicar("Entrar")
        compose.onNodeWithText("Credenciais inválidas.").assertDoesNotExist()
    }

    @Test fun ligacaoDeConfirmacaoConfirmaUmaVez() {
        val c = cenario()
        c.login.abrirLigacao(LigacaoApp.Confirmar("abc"))
        esperar("A tua conta está confirmada. Já podes iniciar sessão.")
        assertEquals(1, c.api.pedidosConfirmar)
        assertNull(c.login.ligacaoPendente.value)
    }

    @Test fun ligacaoDeConfirmacaoFuncionaComSessaoAberta() {
        val c = cenario(sessao())
        esperar("Perfil")
        c.login.abrirLigacao(LigacaoApp.Confirmar("abc"))
        esperar("A tua conta está confirmada. Já podes iniciar sessão.")
        assertEquals(1, c.api.pedidosConfirmar)
    }

    @Test fun ligacaoDePerfilComSessaoAbreOPerfilPublico() {
        val c = cenario(sessao())
        esperar("Perfil")
        c.login.abrirLigacao(LigacaoApp.Perfil(idOutro))
        esperar("Rui")
        compose.onNodeWithText("Perfil público").assertExists()
    }

    @Test fun ligacaoDePerfilSemSessaoPedeEntradaEDepoisAbre() {
        val c = cenario()
        c.login.abrirLigacao(LigacaoApp.Perfil(idOutro))
        compose.waitUntil(5_000) {
            compose.onAllNodes(hasTestTag("login-email"))
                .fetchSemanticsNodes().isNotEmpty()
        }
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        compose.onNodeWithTag("login-submit").performClick()
        esperar("Rui")
    }

    @Test fun sessaoTerminadaPeloServidorAbreAEntrada() {
        val c = cenario(sessao())
        esperar("Perfil")
        c.sessao.terminar()
        esperar("A sessão terminou. Inicia sessão novamente.")
        compose.waitForIdle()
        compose.onNodeWithTag("login-email").assertExists()
    }

    @Test fun edicaoGuardadaVoltaAoPerfilEAvisa() {
        val c = cenario(sessao())
        esperar("Perfil")
        clicar("Perfil")
        esperar("Editar perfil")
        clicar("Editar perfil")
        compose.waitUntil(5_000) {
            compose.onAllNodes(hasTestTag("perfil-curso"))
                .fetchSemanticsNodes().isNotEmpty()
        }
        compose.onNodeWithTag("perfil-curso").performTextInput("LEI")
        compose.onNodeWithTag("perfil-guardar").performScrollTo().performClick()
        esperar("Perfil atualizado.")
        assertEquals(1, c.api.pedidosAtualizar)
    }

    @Test fun meuPerfilPublicoAbreSemEmail() {
        cenario(sessao())
        esperar("Perfil")
        clicar("Perfil")
        esperar("Ver o meu perfil público")
        clicar("Ver o meu perfil público")
        esperar("Rui")
        compose.onNodeWithText("ana@exemplo.test").assertDoesNotExist()
    }
}

@RunWith(RobolectricTestRunner::class)
class ArranqueTest {
    @get:Rule val compose = createAndroidComposeRule<MainActivity>()

    @Test fun aplicacaoArrancaSemSessaoNoInicio() {
        compose.onNodeWithText("Entrar").assertExists()
    }
}

@RunWith(RobolectricTestRunner::class)
class ConnectivityMonitorTest {
    private val contexto = ApplicationProvider.getApplicationContext<Context>()
    private val gestor = contexto.getSystemService(ConnectivityManager::class.java)

    private fun comValidacao(validada: Boolean) = ShadowNetworkCapabilities.newInstance().also {
        if (validada) {
            Shadows.shadowOf(it).addCapability(NetworkCapabilities.NET_CAPABILITY_VALIDATED)
        }
    }

    @Test fun acompanhaAsCapacidadesRecebidasEAPerda() {
        val monitor = ConnectivityMonitor(contexto)
        val rede = gestor.activeNetwork!!
        val callback = Shadows.shadowOf(gestor).networkCallbacks.single()
        callback.onCapabilitiesChanged(rede, comValidacao(true))
        assertEquals(true, monitor.online.value)
        callback.onCapabilitiesChanged(rede, comValidacao(false))
        assertEquals(false, monitor.online.value)
        callback.onCapabilitiesChanged(rede, comValidacao(true))
        callback.onLost(rede)
        assertEquals(false, monitor.online.value)
    }
}
