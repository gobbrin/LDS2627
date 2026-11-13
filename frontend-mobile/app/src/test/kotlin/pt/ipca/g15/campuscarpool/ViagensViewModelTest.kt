package pt.ipca.g15.campuscarpool

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.TestScope
import kotlinx.coroutines.test.UnconfinedTestDispatcher
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
import pt.ipca.g15.campuscarpool.domain.model.CamposViagem
import pt.ipca.g15.campuscarpool.domain.model.ViagemDoCondutor
import retrofit2.Response

@OptIn(ExperimentalCoroutinesApi::class)
class ViagensViewModelTest {
    private val despachante = UnconfinedTestDispatcher()
    private val validos = CamposViagem("Barcelos", "Braga", "25/12/2026", "09:30", "3")

    @Before fun preparar() = Dispatchers.setMain(despachante)

    @After fun limpar() = Dispatchers.resetMain()

    private fun TestScope.cenario() = Cenario(backgroundScope)

    private fun TestScope.aGerir(c: Cenario = cenario()) = c.also {
        it.viagemForm.abrir(ID_VIAGEM)
        runCurrent()
    }

    @Test fun listaComEstadoELugares() = runTest(despachante) {
        val c = cenario()
        c.minhasViagens.carregar()
        runCurrent()
        val estado = c.minhasViagens.estado.value
        assertTrue(estado.carregada)
        assertFalse(estado.aCarregar)
        assertEquals(2, estado.viagens.single().lugaresDisponiveis)
    }

    @Test fun listaVaziaDistingueDeAindaACarregar() = runTest(despachante) {
        val c = cenario()
        c.api.minhasViagensResposta = Response.success(emptyList<ViagemDoCondutor>())
        assertFalse(c.minhasViagens.estado.value.carregada)
        c.minhasViagens.carregar()
        runCurrent()
        assertTrue(c.minhasViagens.estado.value.carregada)
        assertTrue(c.minhasViagens.estado.value.viagens.isEmpty())
    }

    @Test fun falhaNaListaPermiteRepetir() = runTest(despachante) {
        val c = cenario()
        c.api.minhasViagensResposta = erroDaApi(500)
        c.minhasViagens.carregar()
        runCurrent()
        assertNotNull(c.minhasViagens.estado.value.erro)
        c.api.minhasViagensResposta = Response.success(listOf(viagemDoCondutorDeTeste))
        c.minhasViagens.carregar()
        runCurrent()
        assertNull(c.minhasViagens.estado.value.erro)
        assertEquals(1, c.minhasViagens.estado.value.viagens.size)
    }

    @Test fun criarEnviaPartidaEmUtc() = runTest(despachante) {
        val c = cenario()
        c.viagemForm.criar(validos.copy(origem = " Barcelos ", lugares = " 3 "))
        runCurrent()
        val enviados = c.api.dadosViagemEnviados!!
        assertEquals("Barcelos", enviados.origem)
        assertEquals("2026-12-25T09:30:00Z", enviados.partidaEm)
        assertEquals(3, enviados.lugares)
        val estado = c.viagemForm.estado.value
        assertTrue(estado.criada)
        assertEquals("Viagem criada. Rota: 21,5 km · cerca de 25 min.", estado.aviso)
        assertFalse(estado.aGuardar)
    }

    @Test fun avisoECriadaConsomemSe() = runTest(despachante) {
        val c = cenario()
        c.viagemForm.criar(validos)
        runCurrent()
        c.viagemForm.consumirAviso()
        c.viagemForm.consumirCriada()
        assertNull(c.viagemForm.estado.value.aviso)
        assertFalse(c.viagemForm.estado.value.criada)
    }

    @Test fun criarInvalidoNaoChamaAApi() = runTest(despachante) {
        val c = cenario()
        c.viagemForm.criar(validos.copy(destino = "barcelos", lugares = "9"))
        runCurrent()
        assertEquals(0, c.api.pedidosCriarViagem)
        val erros = c.viagemForm.estado.value.errosCampos
        assertEquals("A origem e o destino não podem ser o mesmo local.", erros["destino"])
        assertEquals("O máximo é 8 lugares.", erros["lugares"])
        assertFalse(c.viagemForm.estado.value.criada)
    }

    @Test fun partidaPassadaNaoChamaAApi() = runTest(despachante) {
        val c = cenario()
        c.viagemForm.criar(validos.copy(data = "10/11/2026", hora = "09:59"))
        runCurrent()
        assertEquals(0, c.api.pedidosCriarViagem)
        assertNotNull(c.viagemForm.estado.value.errosCampos["hora"])
    }

    @Test fun mapasEmBaixoMantemOsDadosEMostraAMensagemRfF00217() = runTest(despachante) {
        val c = cenario()
        c.api.criarViagemResposta = erroDaApi(503, "Serviço de mapas indisponível.")
        c.viagemForm.criar(validos)
        runCurrent()
        val estado = c.viagemForm.estado.value
        assertEquals("Serviço de mapas indisponível.", estado.erro)
        assertFalse(estado.criada)
        assertFalse(estado.aGuardar)
    }

    @Test fun abrirLeUmaSoVez() = runTest(despachante) {
        val c = aGerir()
        c.viagemForm.abrir(ID_VIAGEM)
        runCurrent()
        assertEquals(1, c.api.pedidosViagem)
        val estado = c.viagemForm.estado.value
        assertEquals(1, estado.ocupados)
        assertEquals(3, estado.oferecidos)
        assertEquals(1, estado.versao)
        assertTrue(estado.alteravel())
    }

    @Test fun camposIniciaisEmHoraDePortugal() = runTest(despachante) {
        val c = aGerir()
        assertEquals(
            CamposViagem("Barcelos", "Braga", "01/12/2099", "09:30", "3"),
            c.viagemForm.estado.value.camposIniciais()
        )
    }

    @Test fun viagemDeOutroCondutorNaoSeGere() = runTest(despachante) {
        val c = cenario().also { it.utilizador = "B" }
        aGerir(c)
        val estado = c.viagemForm.estado.value
        assertNull(estado.viagem)
        assertEquals("Só o condutor pode gerir esta viagem.", estado.erroCarregar)
        assertFalse(estado.podeRepetir)
    }

    @Test fun condutorComparaSemDistinguirMaiusculas() = runTest(despachante) {
        val c = cenario().also { it.utilizador = "a" }
        aGerir(c)
        assertNotNull(c.viagemForm.estado.value.viagem)
    }

    @Test fun viagemInexistenteNaoOfereceRepeticao() = runTest(despachante) {
        val c = cenario()
        c.api.viagemResposta = erroDaApi(404, "Não encontrada.")
        aGerir(c)
        assertEquals("Viagem não encontrada.", c.viagemForm.estado.value.erroCarregar)
        assertFalse(c.viagemForm.estado.value.podeRepetir)
    }

    @Test fun falhaAoLerPermiteRepetir() = runTest(despachante) {
        val c = cenario()
        c.api.viagemResposta = erroDaApi(500)
        aGerir(c)
        assertTrue(c.viagemForm.estado.value.podeRepetir)
        c.api.viagemResposta = Response.success(detalhesViagemDeTeste)
        c.viagemForm.carregar(ID_VIAGEM)
        runCurrent()
        assertNull(c.viagemForm.estado.value.erroCarregar)
        assertNotNull(c.viagemForm.estado.value.viagem)
    }

    @Test fun falhaNaListaAoAbrirPermiteRepetir() = runTest(despachante) {
        val c = cenario()
        c.api.minhasViagensResposta = erroDaApi(500)
        aGerir(c)
        assertNull(c.viagemForm.estado.value.viagem)
        assertTrue(c.viagemForm.estado.value.podeRepetir)
    }

    @Test fun viagemIndisponibilizadaNaoEAlteravel() = runTest(despachante) {
        val c = cenario()
        c.api.viagemResposta =
            Response.success(detalhesViagemDeTeste.copy(estado = "Indisponibilizada"))
        c.api.minhasViagensResposta = Response.success(emptyList<ViagemDoCondutor>())
        aGerir(c)
        val estado = c.viagemForm.estado.value
        assertFalse(estado.alteravel())
        assertEquals(0, estado.ocupados)
    }

    @Test fun viagemQueJaPartiuNaoEAlteravel() = runTest(despachante) {
        val c = cenario()
        c.api.viagemResposta =
            Response.success(detalhesViagemDeTeste.copy(partidaEm = "2020-01-01T10:00:00Z"))
        aGerir(c)
        assertFalse(c.viagemForm.estado.value.alteravel())
    }

    @Test fun alterarVoltaALerDoServidor() = runTest(despachante) {
        val c = aGerir()
        c.viagemForm.alterar(ID_VIAGEM, validos.copy(destino = "Porto"))
        runCurrent()
        assertEquals("Porto", c.api.dadosViagemEnviados?.destino)
        assertEquals(2, c.api.pedidosViagem)
        val estado = c.viagemForm.estado.value
        assertEquals("Viagem atualizada.", estado.aviso)
        assertEquals(2, estado.versao)
        assertFalse(estado.aGuardar)
    }

    @Test fun naoOfereceMenosLugaresDoQueOsOcupados() = runTest(despachante) {
        val c = cenario()
        c.api.viagemResposta = Response.success(detalhesViagemDeTeste.copy(lugaresDisponiveis = 1))
        aGerir(c)
        c.viagemForm.alterar(ID_VIAGEM, validos.copy(lugares = "1"))
        runCurrent()
        assertEquals(0, c.api.pedidosAlterarViagem)
        assertEquals(
            "Já tens 2 lugares ocupados nesta viagem. Não podes oferecer menos.",
            c.viagemForm.estado.value.errosCampos["lugares"]
        )
    }

    @Test fun recusaDaApiNaoRelê() = runTest(despachante) {
        val c = aGerir()
        c.api.alterarViagemResposta = erroDaApi(422, "Local não encontrado.")
        c.viagemForm.alterar(ID_VIAGEM, validos)
        runCurrent()
        assertEquals("Local não encontrado.", c.viagemForm.estado.value.erro)
        assertEquals(1, c.api.pedidosViagem)
        assertNotNull(c.viagemForm.estado.value.viagem)
    }

    @Test fun conflitoAoAlterarMostraOMotivoEODadosAtuais() = runTest(despachante) {
        val c = aGerir()
        c.api.alterarViagemResposta = erroDaApi(409, "A viagem já não está ativa.")
        c.viagemForm.alterar(ID_VIAGEM, validos)
        runCurrent()
        assertEquals("A viagem já não está ativa.", c.viagemForm.estado.value.erro)
        assertEquals(2, c.api.pedidosViagem)
        assertEquals(2, c.viagemForm.estado.value.versao)
    }

    @Test fun indisponibilizarAtualizaOEstado() = runTest(despachante) {
        val c = aGerir()
        c.api.viagemResposta =
            Response.success(detalhesViagemDeTeste.copy(estado = "Indisponibilizada"))
        c.viagemForm.indisponibilizar(ID_VIAGEM)
        runCurrent()
        assertEquals(1, c.api.pedidosIndisponibilizar)
        val estado = c.viagemForm.estado.value
        assertEquals("Indisponibilizada", estado.viagem?.estado)
        assertEquals("Viagem indisponibilizada.", estado.aviso)
        assertFalse(estado.aIndisponibilizar)
    }

    @Test fun conflitoAoIndisponibilizarReleEExplica() = runTest(despachante) {
        val c = aGerir()
        c.api.indisponibilizarResposta = erroDaApi(409, "A viagem já não está ativa.")
        c.viagemForm.indisponibilizar(ID_VIAGEM)
        runCurrent()
        assertEquals("A viagem já não está ativa.", c.viagemForm.estado.value.erro)
        assertEquals(2, c.api.pedidosViagem)
    }

    @Test fun outraFalhaAoIndisponibilizarFicaNaZona() = runTest(despachante) {
        val c = aGerir()
        c.api.indisponibilizarResposta = erroDaApi(500)
        c.viagemForm.indisponibilizar(ID_VIAGEM)
        runCurrent()
        assertNotNull(c.viagemForm.estado.value.erroIndisponibilizar)
        assertNull(c.viagemForm.estado.value.erro)
        assertEquals(1, c.api.pedidosViagem)
    }
}
