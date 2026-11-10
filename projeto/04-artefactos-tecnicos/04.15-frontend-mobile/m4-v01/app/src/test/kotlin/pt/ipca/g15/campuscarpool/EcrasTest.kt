package pt.ipca.g15.campuscarpool

import androidx.compose.material3.MaterialTheme
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.StateRestorationTester
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performTextInput
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginScreen
import pt.ipca.g15.campuscarpool.ui.inicio.InicioScreen
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilScreen

@RunWith(RobolectricTestRunner::class)
class EcrasTest {
    @get:Rule val compose = createComposeRule()

    @Test fun inicioDoVisitanteOfereceEntrada() {
        var entrou = false
        compose.setContent {
            MaterialTheme { InicioScreen(false, { entrou = true }, {}, {}, false) }
        }
        compose.onNodeWithText("Entrar").performClick()
        assertEquals(true, entrou)
    }

    @Test fun inicioComSessaoOferecePerfilESaida() {
        var perfil = false
        var saiu = false
        compose.setContent {
            MaterialTheme { InicioScreen(true, {}, { perfil = true }, { saiu = true }, false) }
        }
        compose.onNodeWithText("Perfil").performClick()
        compose.onNodeWithText("Terminar sessão").performClick()
        assertEquals(true, perfil)
        assertEquals(true, saiu)
    }

    @Test fun inicioOcupadoIndicaSaidaEmCurso() {
        compose.setContent { MaterialTheme { InicioScreen(true, {}, {}, {}, true) } }
        compose.onNodeWithText("A terminar sessão…").assertIsNotEnabled()
    }

    @Test fun loginSemDadosNaoSubmete() {
        compose.setContent {
            MaterialTheme { LoginScreen(false, emptyMap(), { _, _ -> error("Não submete") }, {}) }
        }
        compose.onNodeWithTag("login-submit").assertIsNotEnabled()
    }

    @Test fun loginEntregaCredenciais() {
        var recebidas: Pair<String, String>? = null
        compose.setContent {
            MaterialTheme { LoginScreen(false, emptyMap(), { e, p -> recebidas = e to p }, {}) }
        }
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        compose.onNodeWithTag("login-submit").performClick()
        assertEquals("ana@exemplo.test" to "Segredo123", recebidas)
    }

    @Test fun loginMostraErrosPorCampo() {
        val campos = mapOf("email" to listOf("Email inválido"), "Password" to listOf("Obrigatória"))
        compose.setContent { MaterialTheme { LoginScreen(false, campos, { _, _ -> }, {}) } }
        compose.onNodeWithText("Email inválido").assertExists()
        compose.onNodeWithText("Obrigatória").assertExists()
    }

    @Test fun loginOcupadoIndicaProgresso() {
        compose.setContent { MaterialTheme { LoginScreen(true, emptyMap(), { _, _ -> }, {}) } }
        compose.onNodeWithText("A entrar…").assertIsNotEnabled()
    }

    @Test fun loginMantemEmailMasNaoPalavraPasseAoRecriar() {
        val restauro = StateRestorationTester(compose)
        restauro.setContent { MaterialTheme { LoginScreen(false, emptyMap(), { _, _ -> }, {}) } }
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        restauro.emulateSavedInstanceStateRestore()
        compose.onNodeWithText("ana@exemplo.test").assertExists()
        compose.onNodeWithTag("login-submit").assertIsNotEnabled()
    }

    @Test fun perfilACarregar() {
        compose.setContent { MaterialTheme { PerfilScreen(null, true, {}, {}) } }
        compose.onNodeWithText("A carregar…").assertExists()
    }

    @Test fun perfilComDados() {
        compose.setContent {
            MaterialTheme {
                PerfilScreen(
                    PerfilProprio("A", "Ana", "ana@exemplo.test", "LEI", "IPCA", 4.5, 2),
                    false,
                    {},
                    {}
                )
            }
        }
        compose.onNodeWithText("Ana").assertExists()
        compose.onNodeWithText("Curso: LEI").assertExists()
        compose.onNodeWithText("Instituição: IPCA").assertExists()
    }

    @Test fun perfilSemCursoNemAvaliacoes() {
        compose.setContent {
            MaterialTheme {
                PerfilScreen(
                    PerfilProprio("A", "Ana", "ana@exemplo.test", null, null, null, 0),
                    false,
                    {},
                    {}
                )
            }
        }
        compose.onNodeWithText("Curso: Não indicado").assertExists()
        compose.onNodeWithText("Sem avaliações").assertExists()
    }

    @Test fun perfilEmErroPermiteRepetirEVoltar() {
        var repetiu = false
        var voltou = false
        compose.setContent {
            MaterialTheme { PerfilScreen(null, false, { repetiu = true }, { voltou = true }) }
        }
        compose.onNodeWithText("Tentar novamente").performClick()
        compose.onNodeWithText("Voltar").performClick()
        assertEquals(true, repetiu)
        assertEquals(true, voltou)
    }
}
