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
import pt.ipca.g15.campuscarpool.domain.model.LigacaoPerfil
import retrofit2.Response

@OptIn(ExperimentalCoroutinesApi::class)
class PerfilViewModelTest {
    private val despachante = UnconfinedTestDispatcher()

    @Before fun preparar() = Dispatchers.setMain(despachante)

    @After fun limpar() = Dispatchers.resetMain()

    private fun TestScope.cenario() = Cenario(backgroundScope)

    private fun TestScope.comPerfil() = cenario().also {
        it.perfil.carregarProprio()
        runCurrent()
    }

    @Test fun carregaOProprioPerfil() = runTest(despachante) {
        val c = comPerfil()
        assertEquals("Ana", c.perfil.estado.value.perfil?.nome)
        assertFalse(c.perfil.estado.value.aCarregar)
    }

    @Test fun falhaAoCarregarPermiteRepetir() = runTest(despachante) {
        val c = cenario()
        c.api.perfilResposta = erroDaApi(500)
        c.perfil.carregarProprio()
        runCurrent()
        assertNull(c.perfil.estado.value.perfil)
        assertNotNull(c.perfil.estado.value.erro)
        c.api.perfilResposta = Response.success(perfilDeTeste)
        c.perfil.carregarProprio()
        runCurrent()
        assertEquals("Ana", c.perfil.estado.value.perfil?.nome)
        assertNull(c.perfil.estado.value.erro)
    }

    @Test fun guardarValidoAtualizaOPerfil() = runTest(despachante) {
        val c = comPerfil()
        c.perfil.guardar("Ana Lopes", "LEI", "")
        runCurrent()
        val estado = c.perfil.estado.value
        assertTrue(estado.guardado)
        assertEquals("Ana Lopes", estado.perfil?.nome)
        assertEquals("LEI", estado.perfil?.curso)
        assertNull(estado.perfil?.instituicao)
        c.perfil.consumirGuardado()
        assertFalse(c.perfil.estado.value.guardado)
    }

    @Test fun nomeVazioNaoChamaAApi() = runTest(despachante) {
        val c = comPerfil()
        c.perfil.guardar(" ", "", "")
        runCurrent()
        assertEquals(0, c.api.pedidosAtualizar)
        assertEquals("Indica o teu nome.", c.perfil.estado.value.errosEdicao["nome"])
        assertFalse(c.perfil.estado.value.guardado)
    }

    @Test fun recusaDaApiMantemOPerfil() = runTest(despachante) {
        val c = comPerfil()
        c.api.atualizarResposta = erroDaApi(400, "Nome inválido.")
        c.perfil.guardar("Ana", "", "")
        runCurrent()
        assertEquals("Nome inválido.", c.perfil.estado.value.erroEdicao)
        assertEquals("Ana", c.perfil.estado.value.perfil?.nome)
        assertFalse(c.perfil.estado.value.guardado)
    }

    @Test fun adicionarLigacaoAcrescentaALista() = runTest(despachante) {
        val c = comPerfil()
        c.perfil.adicionarLigacao("https://exemplo.test/novo", "")
        runCurrent()
        val estado = c.perfil.estado.value
        assertEquals(listOf("L1", "L2"), estado.perfil?.ligacoesExternas?.map { it.id })
        assertEquals(1, estado.ligacoesAdicionadas)
    }

    @Test fun enderecoInvalidoNaoChamaAApi() = runTest(despachante) {
        val c = comPerfil()
        c.perfil.adicionarLigacao("javascript:alert(1)", "")
        runCurrent()
        assertEquals(0, c.api.pedidosLigacao)
        assertNotNull(c.perfil.estado.value.errosLigacao["url"])
    }

    @Test fun maximoAtingidoNoutroDispositivoReleALista() = runTest(despachante) {
        val c = comPerfil()
        val cinco = (1..5).map { LigacaoPerfil("L$it", "https://a.test/$it", null) }
        c.api.perfilResposta = Response.success(perfilDeTeste.copy(ligacoesExternas = cinco))
        c.api.novaLigacaoResposta = erroDaApi(400, "Máximo de 5 ligações.")
        c.perfil.adicionarLigacao("https://exemplo.test/novo", "")
        runCurrent()
        assertEquals("Máximo de 5 ligações.", c.perfil.estado.value.erroLigacoes)
        assertEquals(5, c.perfil.estado.value.perfil?.ligacoesExternas?.size)
        assertFalse(c.perfil.estado.value.aAlterarLigacoes)
    }

    @Test fun removerLigacaoTiraDaLista() = runTest(despachante) {
        val c = comPerfil()
        c.perfil.removerLigacao("L1")
        runCurrent()
        assertTrue(c.perfil.estado.value.perfil!!.ligacoesExternas.isEmpty())
    }

    @Test fun ligacaoJaRemovidaAvisaEMostraOPerfilAtual() = runTest(despachante) {
        val c = comPerfil()
        c.api.removerResposta = erroDaApi(404, "Ligação não encontrada.")
        c.api.perfilResposta =
            Response.success(perfilDeTeste.copy(ligacoesExternas = emptyList()))
        c.perfil.removerLigacao("L1")
        runCurrent()
        assertEquals("Ligação não encontrada.", c.perfil.estado.value.erroLigacoes)
        assertTrue(c.perfil.estado.value.perfil!!.ligacoesExternas.isEmpty())
    }

    @Test fun outraFalhaAoRemoverMantemALista() = runTest(despachante) {
        val c = comPerfil()
        val pedidos = c.api.pedidosPerfil
        c.api.removerResposta = erroDaApi(500)
        c.perfil.removerLigacao("L1")
        runCurrent()
        assertNotNull(c.perfil.estado.value.erroLigacoes)
        assertEquals(1, c.perfil.estado.value.perfil?.ligacoesExternas?.size)
        assertEquals(pedidos, c.api.pedidosPerfil)
    }

    @Test fun perfilPublicoDeOutroUtilizador() = runTest(despachante) {
        val c = cenario()
        c.perfil.carregarPublico("B")
        runCurrent()
        assertEquals("Rui", c.perfil.publico.value.perfil?.nome)
        assertFalse(c.perfil.publico.value.aCarregar)
    }

    @Test fun perfilPublicoInexistente() = runTest(despachante) {
        val c = cenario()
        c.api.publicoResposta = erroDaApi(404, "Não encontrado.")
        c.perfil.carregarPublico("B")
        runCurrent()
        assertTrue(c.perfil.publico.value.naoEncontrado)
        assertNull(c.perfil.publico.value.perfil)
    }

    @Test fun perfilPublicoComFalhaDoServico() = runTest(despachante) {
        val c = cenario()
        c.api.publicoResposta = erroDaApi(500)
        c.perfil.carregarPublico("B")
        runCurrent()
        assertFalse(c.perfil.publico.value.naoEncontrado)
        assertNotNull(c.perfil.publico.value.erro)
    }
}
