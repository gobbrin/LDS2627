package pt.ipca.g15.campuscarpool
import androidx.compose.material3.MaterialTheme
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTextInput
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import pt.ipca.g15.campuscarpool.ui.autenticacao.EstadoLogin
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginScreen
class LoginScreenTest {
    @get:Rule val compose = createComposeRule()

    @Test fun credenciaisVaziasNaoSubmetem() {
        compose.setContent {
            MaterialTheme {
                LoginScreen(EstadoLogin(), { _, _ -> error("Não deve submeter") }, {}, {}, {}, {})
            }
        }
        compose.onNodeWithTag("login-submit").assertIsNotEnabled()
    }

    @Test fun credenciaisValidasSaoEntreguesAoViewModel() {
        var recebidas: Pair<String, String>? = null
        compose.setContent {
            MaterialTheme {
                LoginScreen(EstadoLogin(), { e, p -> recebidas = e to p }, {}, {}, {}, {})
            }
        }
        compose.onNodeWithTag("login-email").performTextInput("aluno@example.test")
        compose.onNodeWithTag("login-password").performTextInput("Teste1234")
        compose.onNodeWithTag("login-submit").performClick()
        assertEquals("aluno@example.test" to "Teste1234", recebidas)
    }
}
