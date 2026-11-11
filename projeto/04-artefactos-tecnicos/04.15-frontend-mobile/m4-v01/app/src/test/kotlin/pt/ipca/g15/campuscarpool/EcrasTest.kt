package pt.ipca.g15.campuscarpool

import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.test.assertCountEquals
import androidx.compose.ui.test.assertIsNotEnabled
import androidx.compose.ui.test.junit4.StateRestorationTester
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onAllNodesWithText
import androidx.compose.ui.test.onNodeWithTag
import androidx.compose.ui.test.onNodeWithText
import androidx.compose.ui.test.performClick
import androidx.compose.ui.test.performScrollTo
import androidx.compose.ui.test.performTextInput
import org.junit.Assert.assertEquals
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import pt.ipca.g15.campuscarpool.domain.model.LigacaoPerfil
import pt.ipca.g15.campuscarpool.ui.autenticacao.ConfirmacaoScreen
import pt.ipca.g15.campuscarpool.ui.autenticacao.EstadoConfirmacao
import pt.ipca.g15.campuscarpool.ui.autenticacao.EstadoLogin
import pt.ipca.g15.campuscarpool.ui.autenticacao.EstadoReenvio
import pt.ipca.g15.campuscarpool.ui.autenticacao.EstadoRegisto
import pt.ipca.g15.campuscarpool.ui.autenticacao.FaseConfirmacao
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginScreen
import pt.ipca.g15.campuscarpool.ui.autenticacao.ReenvioScreen
import pt.ipca.g15.campuscarpool.ui.autenticacao.RegistoScreen
import pt.ipca.g15.campuscarpool.ui.inicio.InicioScreen
import pt.ipca.g15.campuscarpool.ui.perfil.EditarPerfilScreen
import pt.ipca.g15.campuscarpool.ui.perfil.EstadoPerfil
import pt.ipca.g15.campuscarpool.ui.perfil.EstadoPerfilPublico
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilPublicoScreen
import pt.ipca.g15.campuscarpool.ui.perfil.PerfilScreen

@RunWith(RobolectricTestRunner::class)
class EcrasTest {
    @get:Rule val compose = createComposeRule()

    private fun mostrar(conteudo: @Composable () -> Unit) =
        compose.setContent { MaterialTheme { conteudo() } }

    private fun clicar(texto: String) =
        compose.onNodeWithText(texto).performScrollTo().performClick()

    private fun existe(texto: String) = compose.onNodeWithText(texto).assertExists()

    @Composable private fun Login(
        estado: EstadoLogin = EstadoLogin(),
        entrar: (String, String) -> Unit = { _, _ -> },
        pedirLigacao: (String) -> Unit = {}
    ) = LoginScreen(estado, entrar, {}, {}, pedirLigacao, {})

    @Composable private fun Registo(
        estado: EstadoRegisto = EstadoRegisto(),
        registar: (String, String, String, String) -> Unit = { _, _, _, _ -> },
        entrada: () -> Unit = {},
        pedirLigacao: (String) -> Unit = {}
    ) = RegistoScreen(estado, registar, entrada, pedirLigacao, {}, {})

    @Composable private fun Confirmacao(
        estado: EstadoConfirmacao,
        autenticado: Boolean = false,
        confirmar: (String) -> Unit = {},
        repetir: () -> Unit = {}
    ) = ConfirmacaoScreen(estado, autenticado, confirmar, repetir, {}, {}, {})

    @Composable private fun Editar(
        estado: EstadoPerfil,
        guardar: (String, String, String) -> Unit = { _, _, _ -> },
        adicionar: (String, String) -> Unit = { _, _ -> },
        remover: (String) -> Unit = {}
    ) = EditarPerfilScreen(estado, {}, guardar, adicionar, remover, {})

    @Test fun inicioDoVisitanteOfereceRegistoEEntrada() {
        var entrou = false
        var registou = false
        mostrar { InicioScreen(false, { entrou = true }, { registou = true }, {}, {}, false) }
        clicar("Entrar")
        clicar("Criar conta")
        assertEquals(true, entrou)
        assertEquals(true, registou)
    }

    @Test fun inicioComSessaoOferecePerfilESaida() {
        var perfil = false
        var saiu = false
        mostrar { InicioScreen(true, {}, {}, { perfil = true }, { saiu = true }, false) }
        clicar("Perfil")
        clicar("Terminar sessão")
        assertEquals(true, perfil)
        assertEquals(true, saiu)
    }

    @Test fun inicioOcupadoIndicaSaidaEmCurso() {
        mostrar { InicioScreen(true, {}, {}, {}, {}, true) }
        compose.onNodeWithText("A terminar sessão…").assertIsNotEnabled()
    }

    @Test fun loginSemDadosNaoSubmete() {
        mostrar { Login() }
        compose.onNodeWithTag("login-submit").assertIsNotEnabled()
    }

    @Test fun loginEntregaCredenciais() {
        var recebidas: Pair<String, String>? = null
        mostrar { Login(entrar = { e, p -> recebidas = e to p }) }
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        compose.onNodeWithTag("login-submit").performClick()
        assertEquals("ana@exemplo.test" to "Segredo123", recebidas)
    }

    @Test fun loginMostraErrosPorCampo() {
        val campos = mapOf("email" to listOf("Email inválido"), "Password" to listOf("Obrigatória"))
        mostrar { Login(EstadoLogin(campos = campos)) }
        existe("Email inválido")
        existe("Obrigatória")
    }

    @Test fun loginOcupadoIndicaProgresso() {
        mostrar { Login(EstadoLogin(ocupado = true)) }
        compose.onNodeWithText("A entrar…").assertIsNotEnabled()
    }

    @Test fun loginMantemEmailMasNaoPalavraPasseAoRecriar() {
        val restauro = StateRestorationTester(compose)
        restauro.setContent { MaterialTheme { Login() } }
        compose.onNodeWithTag("login-email").performTextInput("ana@exemplo.test")
        compose.onNodeWithTag("login-password").performTextInput("Segredo123")
        restauro.emulateSavedInstanceStateRestore()
        existe("ana@exemplo.test")
        compose.onNodeWithTag("login-submit").assertIsNotEnabled()
    }

    @Test fun loginComCredenciaisErradasMostraAMensagem() {
        mostrar { Login(EstadoLogin(erro = "Credenciais inválidas.")) }
        existe("Credenciais inválidas.")
    }

    @Test fun contaPorConfirmarOfereceNovaLigacaoComOEmail() {
        var pedido: String? = null
        val estado = EstadoLogin(erro = "Conta por confirmar.", emailPorConfirmar = "ana@ipca.pt")
        mostrar { Login(estado, pedirLigacao = { pedido = it }) }
        existe("Conta por confirmar.")
        existe("Abre a ligação que recebeste por email antes de iniciar sessão.")
        compose.onAllNodesWithText("Pedir nova ligação de confirmação").assertCountEquals(1)
        clicar("Pedir nova ligação de confirmação")
        assertEquals("ana@ipca.pt", pedido)
    }

    @Test fun registoEntregaOsQuatroCampos() {
        var recebido: List<String>? = null
        mostrar { Registo(registar = { n, e, p, c -> recebido = listOf(n, e, p, c) }) }
        compose.onNodeWithTag("registo-nome").performTextInput("Ana")
        compose.onNodeWithTag("registo-email").performTextInput("ana@ipca.pt")
        compose.onNodeWithTag("registo-password").performTextInput("Segredo123")
        compose.onNodeWithTag("registo-confirmacao").performTextInput("Segredo123")
        compose.onNodeWithTag("registo-submit").performScrollTo().performClick()
        assertEquals(listOf("Ana", "ana@ipca.pt", "Segredo123", "Segredo123"), recebido)
    }

    @Test fun registoMostraOsErrosDeValidacao() {
        val erros = mapOf("nome" to "Indica o teu nome.", "email" to "Indica um email válido.")
        mostrar { Registo(EstadoRegisto(erros = erros)) }
        existe("Indica o teu nome.")
        existe("Indica um email válido.")
    }

    @Test fun emailRegistadoOfereceOsDoisCaminhos() {
        var entrou = false
        var pedido: String? = null
        val estado = EstadoRegisto(
            erro = "Email já registado.",
            emailJaRegistado = true,
            email = "ana@ipca.pt"
        )
        mostrar { Registo(estado, entrada = { entrou = true }, pedirLigacao = { pedido = it }) }
        existe("Email já registado.")
        clicar("Entrar")
        clicar("Pedir nova ligação de confirmação")
        assertEquals(true, entrou)
        assertEquals("ana@ipca.pt", pedido)
    }

    @Test fun registoConcluidoPedeParaVerOEmail() {
        mostrar { Registo(EstadoRegisto(concluido = "Enviámos uma ligação.")) }
        existe("Verifica o teu email")
        existe("Enviámos uma ligação.")
    }

    @Test fun registoSemEmailOfereceNovaLigacao() {
        var pedido: String? = null
        val estado = EstadoRegisto(emailNaoEnviado = "O email falhou.", email = "ana@ipca.pt")
        mostrar { Registo(estado, pedirLigacao = { pedido = it }) }
        existe("Falta a confirmação")
        clicar("Pedir nova ligação de confirmação")
        assertEquals("ana@ipca.pt", pedido)
    }

    @Test fun confirmacaoEntregaOTextoColado() {
        var recebido: String? = null
        mostrar { Confirmacao(EstadoConfirmacao(), confirmar = { recebido = it }) }
        compose.onNodeWithTag("confirmacao-submit").assertIsNotEnabled()
        compose.onNodeWithTag("confirmacao-texto").performTextInput("abc")
        compose.onNodeWithTag("confirmacao-submit").performScrollTo().performClick()
        assertEquals("abc", recebido)
    }

    @Test fun confirmacaoEmCursoEConcluida() {
        mostrar { Confirmacao(EstadoConfirmacao(FaseConfirmacao.A_VERIFICAR)) }
        existe("A confirmar a conta…")
    }

    @Test fun contaConfirmadaOfereceEntrada() {
        mostrar { Confirmacao(EstadoConfirmacao(FaseConfirmacao.CONFIRMADA)) }
        existe("A tua conta está confirmada. Já podes iniciar sessão.")
        existe("Entrar")
    }

    @Test fun contaConfirmadaComSessaoNaoOfereceEntrada() {
        mostrar { Confirmacao(EstadoConfirmacao(FaseConfirmacao.CONFIRMADA), true) }
        compose.onNodeWithText("Entrar").assertDoesNotExist()
    }

    @Test fun ligacaoRecusadaOfereceOsDoisCaminhos() {
        mostrar {
            Confirmacao(EstadoConfirmacao(FaseConfirmacao.FALHOU, erro = "A ligação expirou."))
        }
        existe("A ligação expirou.")
        existe("Pedir nova ligação de confirmação")
        existe("Ir para a entrada")
        compose.onNodeWithText("Tentar novamente").assertDoesNotExist()
    }

    @Test fun falhaDeRedePermiteRepetir() {
        var repetiu = false
        val estado =
            EstadoConfirmacao(FaseConfirmacao.FALHOU, erro = "Sem ligação.", repetivel = true)
        mostrar { Confirmacao(estado, repetir = { repetiu = true }) }
        clicar("Tentar novamente")
        assertEquals(true, repetiu)
    }

    @Test fun reenvioPreencheOEmailEEntrega() {
        var recebido: String? = null
        mostrar {
            ReenvioScreen(EstadoReenvio(), "ana@ipca.pt", { recebido = it }, {}, {}, {})
        }
        compose.onNodeWithTag("reenvio-submit").performClick()
        assertEquals("ana@ipca.pt", recebido)
    }

    @Test fun reenvioConcluidoMostraARespostaNeutra() {
        mostrar {
            ReenvioScreen(EstadoReenvio(concluido = "Se existir, foi enviada."), "", {}, {}, {}, {})
        }
        existe("Se existir, foi enviada.")
        compose.onNodeWithTag("reenvio-email").assertDoesNotExist()
    }

    @Test fun perfilACarregar() {
        mostrar { PerfilScreen(EstadoPerfil(aCarregar = true), {}, {}, {}, {}) }
        existe("A carregar…")
    }

    @Test fun perfilComDados() {
        val perfil = perfilDeTeste.copy(curso = "LEI", instituicao = "IPCA")
        var editou = false
        var publico: String? = null
        mostrar {
            PerfilScreen(EstadoPerfil(perfil = perfil), {}, { editou = true }, { publico = it }, {})
        }
        existe("Ana")
        existe("ana@exemplo.test")
        existe("Curso: LEI")
        existe("Instituição: IPCA")
        existe("Portefólio")
        clicar("Editar perfil")
        clicar("Ver o meu perfil público")
        assertEquals(true, editou)
        assertEquals("A", publico)
    }

    @Test fun perfilSemCursoNemAvaliacoes() {
        val perfil = perfilDeTeste.copy(classificacaoMedia = null, ligacoesExternas = emptyList())
        mostrar { PerfilScreen(EstadoPerfil(perfil = perfil), {}, {}, {}, {}) }
        existe("Curso: Não indicado")
        existe("Sem avaliações")
        existe("Ainda não há ligações externas.")
    }

    @Test fun perfilEmErroPermiteRepetirEVoltar() {
        var repetiu = false
        var voltou = false
        mostrar { PerfilScreen(EstadoPerfil(), { repetiu = true }, {}, {}, { voltou = true }) }
        clicar("Tentar novamente")
        clicar("Voltar")
        assertEquals(true, repetiu)
        assertEquals(true, voltou)
    }

    @Test fun perfilPublicoNaoMostraEmail() {
        mostrar {
            PerfilPublicoScreen(EstadoPerfilPublico(perfil = perfilPublicoDeTeste), {}, {})
        }
        existe("Rui")
        existe("Curso: LEI")
        existe("Sem avaliações")
        compose.onNodeWithText("ana@exemplo.test").assertDoesNotExist()
    }

    @Test fun perfilPublicoInexistente() {
        mostrar { PerfilPublicoScreen(EstadoPerfilPublico(naoEncontrado = true), {}, {}) }
        existe("Este perfil não foi encontrado.")
        compose.onNodeWithText("Tentar novamente").assertDoesNotExist()
    }

    @Test fun perfilPublicoComFalhaPermiteRepetir() {
        var repetiu = false
        mostrar {
            PerfilPublicoScreen(EstadoPerfilPublico(erro = "Falhou."), { repetiu = true }, {})
        }
        clicar("Tentar novamente")
        assertEquals(true, repetiu)
    }

    @Test fun edicaoVemPreenchidaEEntregaAsAlteracoes() {
        var guardado: List<String>? = null
        val perfil = perfilDeTeste.copy(curso = "LEI")
        mostrar {
            Editar(EstadoPerfil(perfil = perfil), guardar = { n, c, i ->
                guardado = listOf(n, c, i)
            })
        }
        compose.onNodeWithTag("perfil-instituicao").performTextInput("IPCA")
        compose.onNodeWithTag("perfil-guardar").performScrollTo().performClick()
        assertEquals(listOf("Ana", "LEI", "IPCA"), guardado)
    }

    @Test fun edicaoMostraOsErros() {
        val estado = EstadoPerfil(
            perfil = perfilDeTeste,
            errosEdicao = mapOf("nome" to "Indica o teu nome."),
            erroEdicao = "Recusado pela API."
        )
        mostrar { Editar(estado) }
        existe("Indica o teu nome.")
        existe("Recusado pela API.")
    }

    @Test fun removerLigacaoPedeConfirmacao() {
        var removida: String? = null
        mostrar { Editar(EstadoPerfil(perfil = perfilDeTeste), remover = { removida = it }) }
        clicar("Remover")
        existe("Remover esta ligação?")
        assertEquals(null, removida)
        clicar("Remover")
        assertEquals("L1", removida)
    }

    @Test fun cancelarRemocaoMantemALigacao() {
        var removida: String? = null
        mostrar { Editar(EstadoPerfil(perfil = perfilDeTeste), remover = { removida = it }) }
        clicar("Remover")
        compose.onAllNodesWithText("Cancelar")[1].performScrollTo().performClick()
        compose.onNodeWithText("Remover esta ligação?").assertDoesNotExist()
        assertEquals(null, removida)
    }

    @Test fun adicionarLigacaoEntregaOsDados() {
        var recebida: Pair<String, String>? = null
        mostrar {
            Editar(EstadoPerfil(perfil = perfilDeTeste), adicionar = { u, d -> recebida = u to d })
        }
        compose.onNodeWithTag("ligacao-url").performScrollTo().performTextInput("https://a.pt")
        compose.onNodeWithTag("ligacao-descricao").performScrollTo().performTextInput("Site")
        compose.onNodeWithTag("ligacao-adicionar").performScrollTo().performClick()
        assertEquals("https://a.pt" to "Site", recebida)
    }

    @Test fun maximoDeLigacoesEscondeOFormulario() {
        val cinco = (1..5).map { LigacaoPerfil("L$it", "https://a.test/$it", null) }
        mostrar { Editar(EstadoPerfil(perfil = perfilDeTeste.copy(ligacoesExternas = cinco))) }
        compose.onNodeWithTag("ligacao-url").assertDoesNotExist()
        existe("Atingiste o máximo de ligações (5). Remove uma para acrescentar outra.")
    }
}
