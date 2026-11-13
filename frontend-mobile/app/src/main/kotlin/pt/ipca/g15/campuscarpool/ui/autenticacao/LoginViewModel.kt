package pt.ipca.g15.campuscarpool.ui.autenticacao

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.domain.model.LigacaoApp
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.Sessao

data class EstadoLogin(
    val ocupado: Boolean = false,
    val erro: String? = null,
    val campos: Map<String, List<String>> = emptyMap(),
    /** Preenchido no 403: o email da tentativa, para pedir nova ligação (UC-F001-04, E2). */
    val emailPorConfirmar: String? = null,
    val aviso: String? = null,
    /** A sessão terminou sem o utilizador pedir: abre a entrada (UC-F001-05, A1). */
    val irParaEntrada: Boolean = false
)

/** Tem o âmbito da atividade: acompanha a sessão enquanto a navegação é recriada. */
class LoginViewModel(
    sessaoManager: SessaoManager,
    private val autenticacao: AutenticacaoRepository
) : ViewModel() {
    val sessao = sessaoManager.estado
    private val atual = MutableStateFlow(EstadoLogin())
    val estado = atual.asStateFlow()
    private val ligacao = MutableStateFlow<LigacaoApp?>(null)
    val ligacaoPendente = ligacao.asStateFlow()
    private var saidaPedida = false

    init {
        viewModelScope.launch {
            var anterior: Sessao? = null
            sessao.collect { s ->
                if (anterior != null && s == null && !saidaPedida) {
                    atual.update { it.copy(aviso = SESSAO_TERMINOU, irParaEntrada = true) }
                }
                if (s != null) saidaPedida = false
                anterior = s
            }
        }
        // O login já valida a sessão; aqui só falta a que foi restaurada do armazenamento.
        if (sessao.value != null) {
            viewModelScope.launch {
                val validacao = autenticacao.validar()
                if (validacao is Resultado.Falha) {
                    atual.update { it.copy(aviso = validacao.mensagem) }
                }
            }
        }
    }

    fun entrar(email: String, password: String) {
        if (atual.value.ocupado) return
        atual.update {
            it.copy(
                ocupado = true,
                erro = null,
                campos = emptyMap(),
                emailPorConfirmar = null,
                aviso = null
            )
        }
        viewModelScope.launch {
            try {
                val r = autenticacao.login(email, password)
                if (r is Resultado.Falha) {
                    atual.update {
                        it.copy(
                            erro = r.mensagem,
                            campos = r.campos,
                            emailPorConfirmar = email.trim().takeIf { r.status == 403 }
                        )
                    }
                }
            } finally {
                atual.update { it.copy(ocupado = false) }
            }
        }
    }

    fun terminarSessao() {
        if (atual.value.ocupado) return
        atual.update { it.copy(ocupado = true) }
        saidaPedida = true
        viewModelScope.launch {
            try {
                val r = autenticacao.logout()
                atual.update {
                    it.copy(aviso = if (r is Resultado.Falha) r.mensagem else SESSAO_ENCERRADA)
                }
            } finally {
                atual.update { it.copy(ocupado = false) }
            }
        }
    }

    fun limparErroEntrada() = atual.update {
        it.copy(erro = null, campos = emptyMap(), emailPorConfirmar = null)
    }

    fun mostrarAviso(mensagem: String) = atual.update { it.copy(aviso = mensagem) }

    fun limparAviso() = atual.update { it.copy(aviso = null) }

    fun consumirIdaParaEntrada() = atual.update { it.copy(irParaEntrada = false) }

    fun abrirLigacao(destino: LigacaoApp) {
        ligacao.value = destino
    }

    fun consumirLigacao() {
        ligacao.value = null
    }

    private companion object {
        const val SESSAO_TERMINOU = "A sessão terminou. Inicia sessão novamente."
        const val SESSAO_ENCERRADA = "Sessão terminada."
    }
}
