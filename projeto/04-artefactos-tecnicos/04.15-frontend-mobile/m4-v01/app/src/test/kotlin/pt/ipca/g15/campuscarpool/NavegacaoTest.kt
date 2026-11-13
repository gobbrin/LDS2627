package pt.ipca.g15.campuscarpool

import android.content.Context
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import androidx.compose.ui.test.junit4.createAndroidComposeRule
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
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
import pt.ipca.g15.campuscarpool.ui.AppNavigation

@RunWith(RobolectricTestRunner::class)
class NavegacaoTest {
    @get:Rule val compose = createComposeRule()
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)

    @After fun limpar() = scope.cancel()

    private fun cenario() = Cenario(scope, agora = { 0 }).also {
        compose.setContent { AppNavigation(it.vm) }
    }

    private fun entrar() {
        compose.onNodeWithText("Entrar").performClick()
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        compose.onNodeWithTag("login-submit").performClick()
        compose.waitUntil(5_000) {
            compose.onAllNodes(hasTextPerfil()).fetchSemanticsNodes().isNotEmpty()
        }
    }

    private fun hasTextPerfil() = androidx.compose.ui.test.hasText("Perfil")

    @Test fun visitanteEntraConsultaPerfilETermina() {
        val c = cenario()
        entrar()
        compose.onNodeWithText("Perfil").performClick()
        compose.waitUntil(5_000) {
            compose.onAllNodes(androidx.compose.ui.test.hasText("Ana"))
                .fetchSemanticsNodes().isNotEmpty()
        }
        compose.onNodeWithText("Voltar").performClick()
        compose.onNodeWithText("Terminar sessão").performClick()
        compose.waitUntil(5_000) { c.vm.sessao.value == null }
        compose.waitForIdle()
        compose.onNodeWithText("Entrar").assertExists()
        compose.onNodeWithText("Sessão terminada.").assertExists()
        assertNull(c.store.valor)
    }

    @Test fun loginVoltaAoInicio() {
        cenario()
        compose.onNodeWithText("Entrar").performClick()
        compose.onNodeWithText("Voltar").performClick()
        compose.onNodeWithText("O caminho para o campus faz-se em companhia.").assertExists()
    }

    @Test fun avisoDeErroPodeSerFechado() {
        val c = Cenario(scope, agora = { 0 })
        c.api.loginResposta = erroDaApi(401, "Credenciais inválidas.")
        compose.setContent { AppNavigation(c.vm) }
        compose.onNodeWithText("Entrar").performClick()
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("errada")
        compose.onNodeWithTag("login-submit").performClick()
        compose.waitUntil(5_000) { c.vm.estado.value.mensagem != null }
        compose.onNodeWithText("Credenciais inválidas.").assertExists()
        compose.onNodeWithText("Fechar mensagem").performClick()
        compose.onNodeWithText("Credenciais inválidas.").assertDoesNotExist()
    }

    @Test fun semRedeMostraIndicacao() {
        val c = cenario()
        c.online.value = false
        compose.waitForIdle()
        compose.onNodeWithText("Sem ligação à Internet. Verifica a rede e tenta novamente.")
            .assertExists()
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
