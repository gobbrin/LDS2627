package pt.ipca.g15.campuscarpool.ui
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.di.AppContainer
import pt.ipca.g15.campuscarpool.domain.model.PerfilProprio
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.Sessao

data class EstadoUi(
    val ocupado: Boolean = false,
    val mensagem: String? = null,
    val perfil: PerfilProprio? = null,
    val aCarregarPerfil: Boolean = false,
    val campos: Map<String, List<String>> = emptyMap()
)
class AppViewModel(
    private val sessaoManager: SessaoManager,
    val online: StateFlow<Boolean>,
    private val autenticacao: AutenticacaoRepository,
    private val perfis: PerfilRepository
) : ViewModel() {
    val sessao = sessaoManager.estado
    private val atual = MutableStateFlow(EstadoUi())
    val estado = atual.asStateFlow()
    private var saidaPedida = false
    init {
        viewModelScope.launch {
            var anterior: Sessao? = null
            sessao.collectLatest { s ->
                atual.value = atual.value.copy(perfil = null)
                // Sem isto, uma sessão expirada ou revogada volta ao início sem explicação.
                if (anterior != null && s == null && !saidaPedida) {
                    atual.value = atual.value.copy(mensagem = SESSAO_TERMINOU)
                }
                anterior = s
                if (s != null) {
                    saidaPedida = false
                    when (val validacao = autenticacao.validar()) {
                        is Resultado.Falha ->
                            atual.value =
                                atual.value.copy(mensagem = validacao.mensagem)
                        is Resultado.Sucesso -> carregarPerfilInterno()
                    }
                }
            }
        }
    }
    fun entrar(email: String, password: String) {
        if (atual.value.ocupado) return
        atual.value = atual.value.copy(ocupado = true, mensagem = null, campos = emptyMap())
        viewModelScope.launch {
            try {
                val r = autenticacao.login(email, password)
                if (r is Resultado.Falha) {
                    atual.value =
                        atual.value.copy(mensagem = r.mensagem, campos = r.campos)
                }
            } finally {
                atual.value = atual.value.copy(ocupado = false)
            }
        }
    }
    fun sair() {
        if (atual.value.ocupado) return
        atual.value = atual.value.copy(ocupado = true)
        saidaPedida = true
        viewModelScope.launch {
            try {
                val r = autenticacao.logout()
                atual.value =
                    atual.value.copy(
                        perfil = null,
                        mensagem = if (r is Resultado.Falha) r.mensagem else "Sessão terminada."
                    )
            } finally {
                atual.value = atual.value.copy(ocupado = false)
            }
        }
    }
    fun limparMensagem() {
        atual.value = atual.value.copy(mensagem = null)
    }
    fun carregarPerfil() {
        if (!atual.value.aCarregarPerfil) viewModelScope.launch { carregarPerfilInterno() }
    }
    private suspend fun carregarPerfilInterno() {
        val token = sessaoManager.obterToken() ?: return
        atual.value = atual.value.copy(aCarregarPerfil = true)
        try {
            when (val r = perfis.obterProprio()) {
                is Resultado.Sucesso -> if (sessaoManager.obterToken() ==
                    token
                ) {
                    atual.value = atual.value.copy(perfil = r.valor)
                }
                is Resultado.Falha ->
                    atual.value =
                        atual.value.copy(perfil = null, mensagem = r.mensagem)
            }
        } finally {
            atual.value = atual.value.copy(aCarregarPerfil = false)
        }
    }
    class Factory(private val container: AppContainer) : ViewModelProvider.Factory {
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            require(modelClass.isAssignableFrom(AppViewModel::class.java))
            @Suppress("UNCHECKED_CAST")
            return AppViewModel(
                container.sessao,
                container.conectividade.online,
                container.autenticacao,
                container.perfil
            ) as T
        }
    }
    private companion object {
        const val SESSAO_TERMINOU = "A sessão terminou. Inicia sessão novamente."
    }
}
