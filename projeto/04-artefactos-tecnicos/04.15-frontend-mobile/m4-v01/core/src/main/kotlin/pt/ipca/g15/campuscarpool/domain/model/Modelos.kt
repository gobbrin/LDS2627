package pt.ipca.g15.campuscarpool.domain.model

data class Credenciais(val email: String, val password: String)
data class TokenAcesso(val accessToken: String, val expiresIn: Long)
data class SessaoAtual(val id: String)
data class Sessao(
    val accessToken: String,
    val utilizadorId: String,
    val expiraEm: Long,
    val backend: String
)
data class PerfilProprio(
    val id: String,
    val nome: String,
    val email: String,
    val curso: String?,
    val instituicao: String?,
    val classificacaoMedia: Double?,
    val totalAvaliacoes: Int
)
data class TerminarSessaoPedido(val tokenDispositivo: String? = null)
sealed interface Resultado<out T> {
    data class Sucesso<T>(val valor: T) : Resultado<T>
    data class Falha(
        val mensagem: String,
        val status: Int = 0,
        val campos: Map<String, List<String>> = emptyMap()
    ) : Resultado<Nothing>
}
