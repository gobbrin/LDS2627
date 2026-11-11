package pt.ipca.g15.campuscarpool.ui.autenticacao

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.domain.ValidacaoFormularios
import pt.ipca.g15.campuscarpool.domain.model.LigacaoApp
import pt.ipca.g15.campuscarpool.domain.model.Resultado

data class EstadoRegisto(
    val ocupado: Boolean = false,
    val erros: Map<String, String> = emptyMap(),
    val erro: String? = null,
    /** 409: oferece a entrada ou uma nova ligação (UC-F001-01, E4). */
    val emailJaRegistado: Boolean = false,
    /** Mensagem do 202: o registo terminou e falta confirmar. */
    val concluido: String? = null,
    /** Mensagem do 503 da API: a conta existe, mas o email não saiu (UC-F001-01, E5). */
    val emailNaoEnviado: String? = null,
    val email: String = ""
)

enum class FaseConfirmacao { INICIAL, A_VERIFICAR, CONFIRMADA, FALHOU }

data class EstadoConfirmacao(
    val fase: FaseConfirmacao = FaseConfirmacao.INICIAL,
    val erro: String? = null,
    val erroEntrada: String? = null,
    /** Falha do serviço, e não da ligação: pode tentar-se de novo com a mesma ligação. */
    val repetivel: Boolean = false
)

data class EstadoReenvio(
    val ocupado: Boolean = false,
    val erroEmail: String? = null,
    val erro: String? = null,
    val concluido: String? = null
)

class RegistoViewModel(private val autenticacao: AutenticacaoRepository) : ViewModel() {
    private val atual = MutableStateFlow(EstadoRegisto())
    val estado = atual.asStateFlow()
    private val confirmacaoAtual = MutableStateFlow(EstadoConfirmacao())
    val confirmacao = confirmacaoAtual.asStateFlow()
    private val reenvioAtual = MutableStateFlow(EstadoReenvio())
    val reenvio = reenvioAtual.asStateFlow()
    private var ultimoToken: String? = null

    fun registar(nome: String, email: String, password: String, confirmacaoPassword: String) {
        if (atual.value.ocupado) return
        val erros = ValidacaoFormularios.registo(nome, email, password, confirmacaoPassword)
        if (erros.isNotEmpty()) {
            atual.update { EstadoRegisto(erros = erros) }
            return
        }
        atual.value = EstadoRegisto(ocupado = true)
        viewModelScope.launch {
            val novo = when (val r = autenticacao.registar(nome, email, password)) {
                is Resultado.Sucesso -> EstadoRegisto(concluido = r.valor, email = email.trim())
                is Resultado.Falha -> when {
                    r.status == 409 -> EstadoRegisto(
                        erro = r.mensagem,
                        emailJaRegistado = true,
                        email = email.trim()
                    )
                    r.status == 503 && r.daApi -> EstadoRegisto(
                        emailNaoEnviado = r.mensagem,
                        email = email.trim()
                    )
                    else -> EstadoRegisto(erro = r.mensagem)
                }
            }
            atual.value = novo
        }
    }

    /** Recebe a ligação completa do email ou só o código, escritos à mão ou colados. */
    fun confirmarTexto(texto: String) {
        val token = LigacaoApp.tokenDoTexto(texto)
        if (token == null) {
            confirmacaoAtual.update { it.copy(erroEntrada = LIGACAO_ILEGIVEL, erro = null) }
        } else {
            confirmar(token)
        }
    }

    fun confirmar(token: String) {
        val fase = confirmacaoAtual.value.fase
        if (fase == FaseConfirmacao.A_VERIFICAR || fase == FaseConfirmacao.CONFIRMADA) return
        ultimoToken = token
        confirmacaoAtual.value = EstadoConfirmacao(FaseConfirmacao.A_VERIFICAR)
        viewModelScope.launch {
            confirmacaoAtual.value = when (val r = autenticacao.confirmar(token)) {
                is Resultado.Sucesso -> EstadoConfirmacao(FaseConfirmacao.CONFIRMADA)
                is Resultado.Falha -> EstadoConfirmacao(
                    FaseConfirmacao.FALHOU,
                    erro = r.mensagem,
                    repetivel = r.status == 0 || r.status == 429 || r.status >= 500
                )
            }
        }
    }

    fun repetirConfirmacao() {
        ultimoToken?.let(::confirmar)
    }

    fun reenviar(email: String) {
        if (reenvioAtual.value.ocupado) return
        val erro = ValidacaoFormularios.email(email)
        if (erro != null) {
            reenvioAtual.value = EstadoReenvio(erroEmail = erro)
            return
        }
        reenvioAtual.value = EstadoReenvio(ocupado = true)
        viewModelScope.launch {
            reenvioAtual.value = when (val r = autenticacao.reenviarConfirmacao(email)) {
                is Resultado.Sucesso -> EstadoReenvio(concluido = r.valor)
                is Resultado.Falha -> EstadoReenvio(erro = r.mensagem)
            }
        }
    }

    private companion object {
        const val LIGACAO_ILEGIVEL = "Cola a ligação completa ou o código que recebeste por email."
    }
}
