package pt.ipca.g15.campuscarpool.ui.perfil

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.domain.model.PerfilPublico
import pt.ipca.g15.campuscarpool.domain.model.Resultado

data class EstadoPerfil(
    val aCarregar: Boolean = false,
    val perfil: PerfilProprio? = null,
    val erro: String? = null,
    val aGuardar: Boolean = false,
    val errosEdicao: Map<String, String> = emptyMap(),
    val erroEdicao: String? = null,
    val guardado: Boolean = false,
    val aAlterarLigacoes: Boolean = false,
    val errosLigacao: Map<String, String> = emptyMap(),
    val erroLigacoes: String? = null,
    /** Aumenta a cada ligação guardada, para o ecrã limpar o formulário. */
    val ligacoesAdicionadas: Int = 0
)

data class EstadoPerfilPublico(
    val aCarregar: Boolean = false,
    val perfil: PerfilPublico? = null,
    val naoEncontrado: Boolean = false,
    val erro: String? = null
)

/** Tem o âmbito de cada ecrã: desaparece com ele, e com ele os dados da sessão anterior. */
class PerfilViewModel(private val perfis: PerfilRepository) : ViewModel() {
    private val atual = MutableStateFlow(EstadoPerfil())
    val estado = atual.asStateFlow()
    private val publicoAtual = MutableStateFlow(EstadoPerfilPublico())
    val publico = publicoAtual.asStateFlow()

    fun carregarProprio() {
        if (atual.value.aCarregar) return
        atual.update { it.copy(aCarregar = true, erro = null) }
        viewModelScope.launch {
            val r = perfis.obterProprio()
            atual.update {
                when (r) {
                    is Resultado.Sucesso -> it.copy(aCarregar = false, perfil = r.valor)
                    is Resultado.Falha -> it.copy(aCarregar = false, erro = r.mensagem)
                }
            }
        }
    }

    fun carregarPublico(id: String) {
        if (publicoAtual.value.aCarregar) return
        publicoAtual.value = EstadoPerfilPublico(aCarregar = true)
        viewModelScope.launch {
            publicoAtual.value = when (val r = perfis.obterPublico(id)) {
                is Resultado.Sucesso -> EstadoPerfilPublico(perfil = r.valor)
                is Resultado.Falha -> EstadoPerfilPublico(
                    naoEncontrado = r.status == 404,
                    erro = r.mensagem
                )
            }
        }
    }

    fun guardar(nome: String, curso: String, instituicao: String) {
        if (atual.value.aGuardar) return
        val erros = ValidacaoFormularios.perfil(nome, curso, instituicao)
        atual.update { it.copy(errosEdicao = erros, erroEdicao = null) }
        if (erros.isNotEmpty()) return
        atual.update { it.copy(aGuardar = true) }
        viewModelScope.launch {
            val r = perfis.atualizar(nome, curso, instituicao)
            atual.update {
                when (r) {
                    is Resultado.Falha -> it.copy(aGuardar = false, erroEdicao = r.mensagem)
                    is Resultado.Sucesso -> it.copy(
                        aGuardar = false,
                        guardado = true,
                        perfil = it.perfil?.copy(
                            nome = r.valor.nome,
                            curso = r.valor.curso,
                            instituicao = r.valor.instituicao
                        )
                    )
                }
            }
        }
    }

    fun consumirGuardado() = atual.update { it.copy(guardado = false) }

    fun adicionarLigacao(url: String, descricao: String) {
        if (atual.value.aAlterarLigacoes) return
        val erros = ValidacaoFormularios.ligacao(url, descricao)
        atual.update { it.copy(errosLigacao = erros, erroLigacoes = null) }
        if (erros.isNotEmpty()) return
        atual.update { it.copy(aAlterarLigacoes = true) }
        viewModelScope.launch {
            when (val r = perfis.adicionarLigacao(url, descricao)) {
                is Resultado.Sucesso -> atual.update {
                    it.copy(
                        aAlterarLigacoes = false,
                        ligacoesAdicionadas = it.ligacoesAdicionadas + 1,
                        perfil = it.perfil?.let { p ->
                            p.copy(ligacoesExternas = p.ligacoesExternas + r.valor)
                        }
                    )
                }
                // Um 400 pode ser o máximo atingido noutro dispositivo: mostra a lista real.
                is Resultado.Falha -> falhaNasLigacoes(r, recarregar = r.status == 400)
            }
        }
    }

    fun removerLigacao(id: String) {
        if (atual.value.aAlterarLigacoes) return
        atual.update { it.copy(aAlterarLigacoes = true, erroLigacoes = null) }
        viewModelScope.launch {
            when (val r = perfis.removerLigacao(id)) {
                is Resultado.Sucesso -> atual.update {
                    it.copy(
                        aAlterarLigacoes = false,
                        perfil = it.perfil?.let { p ->
                            p.copy(ligacoesExternas = p.ligacoesExternas.filter { l -> l.id != id })
                        }
                    )
                }
                // 404: já tinha sido removida noutro dispositivo (UC-F001-09, E1).
                is Resultado.Falha -> falhaNasLigacoes(r, recarregar = r.status == 404)
            }
        }
    }

    private suspend fun falhaNasLigacoes(falha: Resultado.Falha, recarregar: Boolean) {
        val perfil = (if (recarregar) perfis.obterProprio() else null) as? Resultado.Sucesso
        atual.update {
            it.copy(
                aAlterarLigacoes = false,
                erroLigacoes = falha.mensagem,
                perfil = perfil?.valor ?: it.perfil
            )
        }
    }
}
