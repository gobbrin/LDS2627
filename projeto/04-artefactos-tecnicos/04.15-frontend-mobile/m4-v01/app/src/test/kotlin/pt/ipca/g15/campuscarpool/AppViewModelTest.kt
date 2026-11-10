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
import org.junit.Before
import org.junit.Test
import pt.ipca.g15.campuscarpool.domain.model.Sessao
import pt.ipca.g15.campuscarpool.domain.model.SessaoAtual
import retrofit2.Response

@OptIn(ExperimentalCoroutinesApi::class)
class AppViewModelTest {
    private val despachante = UnconfinedTestDispatcher()

    @Before fun preparar() = Dispatchers.setMain(despachante)

    @After fun limpar() = Dispatchers.resetMain()

    private fun TestScope.cenario(guardada: Sessao? = null) =
        Cenario(backgroundScope, guardada) { testScheduler.currentTime }

    private fun sessaoGuardada() = Sessao(tokenDeTeste(), "A", 3_600_000, BACKEND)

    @Test fun loginCorretoAbreSessaoECarregaPerfil() = runTest(despachante) {
        val c = cenario()
        c.vm.entrar("ana@exemplo.test", "Segredo123")
        runCurrent()
        assertNotNull(c.vm.sessao.value)
        assertEquals("Ana", c.vm.estado.value.perfil?.nome)
        assertFalse(c.vm.estado.value.ocupado)
    }

    @Test fun loginRecusadoMostraMensagemDaApiSemSessao() = runTest(despachante) {
        val c = cenario()
        c.api.loginResposta = erroDaApi(401, "Credenciais inválidas.")
        c.vm.entrar("ana@exemplo.test", "errada")
        runCurrent()
        assertNull(c.vm.sessao.value)
        assertEquals("Credenciais inválidas.", c.vm.estado.value.mensagem)
        assertFalse(c.vm.estado.value.ocupado)
    }

    @Test fun sessaoRestauradaCarregaPerfil() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        assertEquals("Ana", c.vm.estado.value.perfil?.nome)
    }

    @Test fun validacaoFalhadaMostraMensagem() = runTest(despachante) {
        val c = Cenario(backgroundScope, sessaoGuardada(), { atualResposta = erroDaApi(500) })
        runCurrent()
        assertNotNull(c.vm.sessao.value)
        assertNull(c.vm.estado.value.perfil)
        assertNotNull(c.vm.estado.value.mensagem)
    }

    @Test fun identidadeDivergenteTerminaSessaoComAviso() = runTest(despachante) {
        val c = Cenario(
            backgroundScope,
            sessaoGuardada(),
            preparar = {
                atualResposta = Response.success(SessaoAtual("B"))
            }
        )
        runCurrent()
        assertNull(c.vm.sessao.value)
        assertNull(c.store.valor)
        assertNull(c.vm.estado.value.perfil)
        assertNotNull(c.vm.estado.value.mensagem)
    }

    @Test fun terminarSessaoConfirmaAoUtilizador() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        c.vm.sair()
        runCurrent()
        assertNull(c.vm.sessao.value)
        assertNull(c.vm.estado.value.perfil)
        assertEquals("Sessão terminada.", c.vm.estado.value.mensagem)
        assertFalse(c.vm.estado.value.ocupado)
    }

    @Test fun terminarSemRedeAvisaQueNaoHouveRevogacao() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        c.api.semRedeNoLogout = true
        c.vm.sair()
        runCurrent()
        assertNull(c.vm.sessao.value)
        assertEquals(
            "Sessão local terminada; sem ligação para confirmar a revogação no servidor.",
            c.vm.estado.value.mensagem
        )
    }

    @Test fun sessaoExpiradaAvisaOUtilizador() = runTest(despachante) {
        val c = cenario(sessaoGuardada())
        runCurrent()
        advanceTimeBy(3_600_001)
        runCurrent()
        assertNull(c.vm.sessao.value)
        assertEquals("A sessão terminou. Inicia sessão novamente.", c.vm.estado.value.mensagem)
    }

    @Test fun falhaNoPerfilMostraMensagemEPermiteRepetir() = runTest(despachante) {
        val c = cenario()
        c.api.perfilResposta = erroDaApi(500)
        c.vm.entrar("ana@exemplo.test", "Segredo123")
        runCurrent()
        assertNull(c.vm.estado.value.perfil)
        assertNotNull(c.vm.estado.value.mensagem)
        c.api.perfilResposta = Response.success(perfilDeTeste)
        c.vm.carregarPerfil()
        runCurrent()
        assertEquals("Ana", c.vm.estado.value.perfil?.nome)
    }

    @Test fun semSessaoNaoPedePerfil() = runTest(despachante) {
        val c = cenario()
        c.vm.carregarPerfil()
        runCurrent()
        assertEquals(0, c.api.pedidosPerfil)
    }

    @Test fun limparMensagemRemoveOAviso() = runTest(despachante) {
        val c = cenario()
        c.api.loginResposta = erroDaApi(401)
        c.vm.entrar("a", "b")
        runCurrent()
        c.vm.limparMensagem()
        assertNull(c.vm.estado.value.mensagem)
    }
}
