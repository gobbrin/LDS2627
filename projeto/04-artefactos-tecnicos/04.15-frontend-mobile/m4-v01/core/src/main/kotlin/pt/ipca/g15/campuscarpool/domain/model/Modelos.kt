package pt.ipca.g15.campuscarpool.domain.model

data class Credenciais(val email: String, val password: String)
data class RegistoPedido(val nome: String, val email: String, val password: String)
data class ConfirmarPedido(val token: String)
data class ReenviarConfirmacaoPedido(val email: String)
data class MensagemResposta(val mensagem: String)
data class TokenAcesso(val accessToken: String, val expiresIn: Long)
data class SessaoAtual(val id: String)
data class Sessao(
    val accessToken: String,
    val utilizadorId: String,
    val expiraEm: Long,
    val backend: String
)
data class LigacaoPerfil(val id: String, val url: String, val descricao: String?)
data class PerfilProprio(
    val id: String,
    val nome: String,
    val email: String,
    val curso: String?,
    val instituicao: String?,
    val classificacaoMedia: Double?,
    val totalAvaliacoes: Int,
    val ligacoesExternas: List<LigacaoPerfil> = emptyList()
)

/** Campos de P-10. Não tem email, por isso a interface não o pode mostrar por engano. */
data class PerfilPublico(
    val id: String,
    val nome: String,
    val curso: String?,
    val instituicao: String?,
    val ligacoesExternas: List<LigacaoPerfil>,
    val classificacaoMedia: Double?,
    val totalAvaliacoes: Int
)
data class PerfilAtualizado(
    val id: String,
    val nome: String,
    val email: String,
    val curso: String?,
    val instituicao: String?
)
data class AtualizarPerfilPedido(val nome: String, val curso: String?, val instituicao: String?)
data class NovaLigacaoPedido(val url: String, val descricao: String?)
data class TerminarSessaoPedido(val tokenDispositivo: String? = null)
sealed interface Resultado<out T> {
    data class Sucesso<T>(val valor: T) : Resultado<T>
    data class Falha(
        val mensagem: String,
        val status: Int = 0,
        val campos: Map<String, List<String>> = emptyMap(),
        /** Verdadeiro se a mensagem veio da API, e não de uma resposta da infraestrutura. */
        val daApi: Boolean = false
    ) : Resultado<Nothing>
}
