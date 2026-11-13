package pt.ipca.g15.campuscarpool.ui.viagens

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.data.repository.ViagemRepository
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.ViagemDoCondutor

data class EstadoMinhasViagens(
    val aCarregar: Boolean = false,
    /** Distingue «ainda sem resposta» de «sem viagens». */
    val carregada: Boolean = false,
    val viagens: List<ViagemDoCondutor> = emptyList(),
    val erro: String? = null
)

class MinhasViagensViewModel(private val viagens: ViagemRepository) : ViewModel() {
    private val atual = MutableStateFlow(EstadoMinhasViagens())
    val estado = atual.asStateFlow()

    fun carregar() {
        if (atual.value.aCarregar) return
        atual.update { it.copy(aCarregar = true, erro = null) }
        viewModelScope.launch {
            val r = viagens.listarMinhas()
            atual.update {
                when (r) {
                    is Resultado.Sucesso ->
                        it.copy(aCarregar = false, carregada = true, viagens = r.valor)
                    is Resultado.Falha -> it.copy(aCarregar = false, erro = r.mensagem)
                }
            }
        }
    }
}
