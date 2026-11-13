package pt.ipca.g15.campuscarpool

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.UnconfinedTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runCurrent
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import pt.ipca.g15.campuscarpool.ui.autenticacao.FaseConfirmacao
import retrofit2.Response

@OptIn(ExperimentalCoroutinesApi::class)
class RegistoViewModelTest {
    private val despachante = UnconfinedTestDispatcher()

    @Before fun preparar() = Dispatchers.setMain(despachante)

    @After fun limpar() = Dispatchers.resetMain()

    private fun TestScope.cenario() = Cenario(backgroundScope)

    private fun Cenario.registarValido() =
        registo.registar("Ana", " ana@ipca.pt ", "Segredo123", "Segredo123")

    @Test fun dadosInvalidosNaoChamamAApi() = runTest(despachante) {
        val c = cenario()
        c.registo.registar("", "ana", "curta", "outra")
        runCurrent()
        assertEquals(0, c.api.pedidosRegisto)
        assertEquals(
            setOf("nome", "email", "password", "confirmacao"),
            c.registo.estado.value.erros.keys
        )
    }

    @Test fun registoConcluidoMostraAMensagemDaApi() = runTest(despachante) {
        val c = cenario()
        c.registarValido()
        runCurrent()
        val estado = c.registo.estado.value
        assertEquals("Verifica o email.", estado.concluido)
        assertEquals("ana@ipca.pt", estado.email)
        assertFalse(estado.ocupado)
    }

    @Test fun emailRegistadoOfereceEntradaENovaLigacao() = runTest(despachante) {
        val c = cenario()
        c.api.registoResposta = erroDaApi(409, "Email já registado.")
        c.registarValido()
        runCurrent()
        val estado = c.registo.estado.value
        assertEquals("Email já registado.", estado.erro)
        assertTrue(estado.emailJaRegistado)
        assertNull(estado.concluido)
    }

    @Test fun dominioRecusadoMostraAMensagemDaApi() = runTest(despachante) {
        val c = cenario()
        c.api.registoResposta = erroDaApi(400, "Email académico inválido.")
        c.registarValido()
        runCurrent()
        assertEquals("Email académico inválido.", c.registo.estado.value.erro)
        assertFalse(c.registo.estado.value.emailJaRegistado)
    }

    @Test fun servicoDeEmailEmBaixoOfereceNovaLigacao() = runTest(despachante) {
        val c = cenario()
        c.api.registoResposta = erroDaApi(503, "Conta criada, mas o email falhou.")
        c.registarValido()
        runCurrent()
        assertEquals("Conta criada, mas o email falhou.", c.registo.estado.value.emailNaoEnviado)
        assertEquals("ana@ipca.pt", c.registo.estado.value.email)
    }

    @Test fun servidorIndisponivelNaoDizQueAContaFoiCriada() = runTest(despachante) {
        val c = cenario()
        c.api.registoResposta = Response.error(
            503,
            "<html></html>".toResponseBody("text/html".toMediaType())
        )
        c.registarValido()
        runCurrent()
        assertNull(c.registo.estado.value.emailNaoEnviado)
        assertNotNull(c.registo.estado.value.erro)
    }

    @Test fun confirmacaoComLigacaoColada() = runTest(despachante) {
        val c = cenario()
        c.registo.confirmarTexto("http://localhost:4200/confirmar-conta?token=abc")
        runCurrent()
        assertEquals(FaseConfirmacao.CONFIRMADA, c.registo.confirmacao.value.fase)
        assertEquals(1, c.api.pedidosConfirmar)
    }

    @Test fun textoIlegivelNaoChamaAApi() = runTest(despachante) {
        val c = cenario()
        c.registo.confirmarTexto("https://exemplo.test/sem-codigo")
        runCurrent()
        assertEquals(0, c.api.pedidosConfirmar)
        assertEquals(FaseConfirmacao.INICIAL, c.registo.confirmacao.value.fase)
        assertNotNull(c.registo.confirmacao.value.erroEntrada)
    }

    @Test fun ligacaoRecusadaMostraOMotivoSemRepetir() = runTest(despachante) {
        val c = cenario()
        c.api.confirmarResposta = erroDaApi(400, "A ligação expirou.")
        c.registo.confirmar("abc")
        runCurrent()
        val estado = c.registo.confirmacao.value
        assertEquals(FaseConfirmacao.FALHOU, estado.fase)
        assertEquals("A ligação expirou.", estado.erro)
        assertFalse(estado.repetivel)
    }

    @Test fun semRedePermiteRepetirComAMesmaLigacao() = runTest(despachante) {
        val c = cenario()
        c.api.semRedeNaConfirmacao = true
        c.registo.confirmar("abc")
        runCurrent()
        assertTrue(c.registo.confirmacao.value.repetivel)
        c.api.semRedeNaConfirmacao = false
        c.registo.repetirConfirmacao()
        runCurrent()
        assertEquals(FaseConfirmacao.CONFIRMADA, c.registo.confirmacao.value.fase)
        assertEquals(2, c.api.pedidosConfirmar)
    }

    @Test fun contaConfirmadaNaoVoltaAChamarAApi() = runTest(despachante) {
        val c = cenario()
        c.registo.confirmar("abc")
        runCurrent()
        c.registo.confirmar("abc")
        runCurrent()
        assertEquals(1, c.api.pedidosConfirmar)
    }

    @Test fun repetirSemLigacaoNaoFazNada() = runTest(despachante) {
        val c = cenario()
        c.registo.repetirConfirmacao()
        runCurrent()
        assertEquals(0, c.api.pedidosConfirmar)
    }

    @Test fun reenvioValidaOEmailAntesDeChamar() = runTest(despachante) {
        val c = cenario()
        c.registo.reenviar("ana")
        runCurrent()
        assertEquals(0, c.api.pedidosReenvio)
        assertEquals("Indica um email válido.", c.registo.reenvio.value.erroEmail)
    }

    @Test fun reenvioMostraARespostaNeutra() = runTest(despachante) {
        val c = cenario()
        c.registo.reenviar("ana@ipca.pt")
        runCurrent()
        assertEquals("Se existir, foi enviada.", c.registo.reenvio.value.concluido)
    }

    @Test fun reenvioRecusadoMostraErro() = runTest(despachante) {
        val c = cenario()
        c.api.reenvioResposta = erroDaApi(429, "Demasiados pedidos.")
        c.registo.reenviar("ana@ipca.pt")
        runCurrent()
        assertEquals("Demasiados pedidos.", c.registo.reenvio.value.erro)
    }
}
