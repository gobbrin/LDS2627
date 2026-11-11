package pt.ipca.g15.campuscarpool

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.advanceTimeBy
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import pt.ipca.g15.campuscarpool.domain.model.LigacaoApp
import pt.ipca.g15.campuscarpool.domain.model.Sessao
import pt.ipca.g15.campuscarpool.domain.model.SessaoAtual
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso
import retrofit2.Response

@OptIn(ExperimentalCoroutinesApi::class)
class LoginViewModelTest {
    private val despachante = UnconfinedTestDispatcher()

    @Before fun preparar() = Dispatchers.setMain(despachante)

    @After fun limpar() = Dispatchers.resetMain()

    private fun TestScope.cenario(guardada: Sessao? = null) =
        Cenario(backgroundScope, guardada) { testScheduler.currentTime }

    private fun sessaoGuardada() = Sessao(tokenDeTeste(), "A", 3_600_000, BACKEND)

    @Test fun loginCorretoAbreSessaoComUmaSoValidacao() = runTest(despachante) {
        val c = cenario()
        c.login.entrar("ana@exemplo.test", "Segredo123")
        runCurrent()
        assertNotNull(c.login.sessao.value)
        assertFalse(c.login.estado.value.ocupado)
        assertEquals(1, c.api.pedidosSessao)
    }

    @Test fun credenciaisErradasFicamJuntoDoFormularioSemSessao() = runTest(despachante) {
        val c = cenario()
        c.api.loginResposta = erroDaApi(401, "Credenciais inválidas.")
        c.login.entrar("ana@exemplo.test", "errada")
        runCurrent()
        assertNull(c.login.sessao.value)
        assertEquals("Credenciais inválidas.", c.login.estado.value.erro)
        assertNull(c.login.estado.value.emailPorConfirmar)
        assertNull(c.login.estado.value.aviso)
        assertFalse(c.login.estado.value.ocupado)
    }

    @Test fun contaPorConfirmarGuardaOEmailParaPedirLigacao() = runTest(despachante) {
        val c = cenario()
        c.api.loginResposta = erroDaApi(403, "Conta por confirmar.")
        c.login.entrar(" ana@exemplo.test ", "Segredo123")
        runCurrent()
        assertNull(c.login.sessao.value)
        assertEquals("Conta por confirmar.", c.login.estado.value.erro)
        assertEquals("ana@exemplo.test", c.login.estado.value.emailPorConfirmar)
    }

    @Test fun limparErroEntradaRemoveOsDadosDaTentativa() = runTest(despachante) {
        val c = cenario()
        c.api.loginResposta = erroDaApi(403, "Conta por confirmar.")
        c.login.entrar("ana@exemplo.test", "Segredo123")
        runCurrent()
        c.login.limparErroEntrada()
        assertNull(c.login.estado.value.erro)
        assertNull(c.login.estado.value.emailPorConfirmar)
    }

    @Test fun sessaoRestauradaEValidadaUmaVez() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        assertNotNull(c.login.sessao.value)
        assertEquals(1, c.api.pedidosSessao)
        assertNull(c.login.estado.value.aviso)
    }

    @Test fun validacaoFalhadaAvisaEMantemASessao() = runTest(despachante) {
        val c = Cenario(backgroundScope, sessaoGuardada(), { atualResposta = erroDaApi(500) })
        runCurrent()
        assertNotNull(c.login.sessao.value)
        assertNotNull(c.login.estado.value.aviso)
    }

    @Test fun identidadeDivergenteTerminaSessaoComAviso() = runTest(despachante) {
        val c = Cenario(
            backgroundScope,
            sessaoGuardada(),
            { atualResposta = Response.success(SessaoAtual("B")) },
            { testScheduler.currentTime }
        )
        runCurrent()
        assertNull(c.login.sessao.value)
        assertNotNull(c.login.estado.value.aviso)
    }

    @Test fun terminarSessaoConfirmaEFicaNoInicio() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        c.login.terminarSessao()
        runCurrent()
        assertNull(c.login.sessao.value)
        assertNull(c.store.valor)
        assertEquals("Sessão terminada.", c.login.estado.value.aviso)
        assertFalse(c.login.estado.value.irParaEntrada)
        assertFalse(c.login.estado.value.ocupado)
    }

    @Test fun terminarSemRedeAvisaQueNaoHouveRevogacao() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        c.api.semRedeNoLogout = true
        c.login.terminarSessao()
        runCurrent()
        assertNull(c.login.sessao.value)
        assertEquals(
            "Sessão local terminada; sem ligação para confirmar a revogação no servidor.",
            c.login.estado.value.aviso
        )
    }

    @Test fun sessaoExpiradaAvisaEAbreAEntrada() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        advanceTimeBy(3_600_001)
        runCurrent()
        assertNull(c.login.sessao.value)
        assertEquals("A sessão terminou. Inicia sessão novamente.", c.login.estado.value.aviso)
        assertTrue(c.login.estado.value.irParaEntrada)
        c.login.consumirIdaParaEntrada()
        assertFalse(c.login.estado.value.irParaEntrada)
    }

    @Test fun novaSessaoDepoisDeSairVoltaAAvisarSeExpirar() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        c.login.terminarSessao()
        runCurrent()
        c.sessao.iniciar(TokenAcesso(tokenDeTeste(exp = 7200), 7200))
        runCurrent()
        c.sessao.terminar()
        runCurrent()
        assertEquals("A sessão terminou. Inicia sessão novamente.", c.login.estado.value.aviso)
    }

    @Test fun avisosPodemSerMostradosEFechados() = runTest(despachante) {
        val c = cenario()
        c.login.mostrarAviso("Perfil atualizado.")
        assertEquals("Perfil atualizado.", c.login.estado.value.aviso)
        c.login.limparAviso()
        assertNull(c.login.estado.value.aviso)
    }

    @Test fun ligacaoFicaPendenteAteSerConsumida() = runTest(despachante) {
        val c = cenario()
        c.login.abrirLigacao(LigacaoApp.Confirmar("abc"))
        assertEquals(LigacaoApp.Confirmar("abc"), c.login.ligacaoPendente.value)
        c.login.consumirLigacao()
        assertNull(c.login.ligacaoPendente.value)
    }
}
