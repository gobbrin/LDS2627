package pt.ipca.g15.campuscarpool.domain.model

import java.net.URLDecoder

/** Destino pedido por uma ligação externa à aplicação (`campuscarpool://…`). */
sealed interface LigacaoApp {
    data class Confirmar(val token: String) : LigacaoApp
    data class Perfil(val id: String) : LigacaoApp

    companion object {
        private const val ESQUEMA = "campuscarpool://"
        private val TOKEN = Regex("[?&]token=([^&#\\s]+)")
        private val ID_UTILIZADOR = Regex(
            "utilizadores/([0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})/?"
        )

        fun interpretar(uri: String?): LigacaoApp? {
            val caminho = uri?.trim()?.takeIf { it.startsWith(ESQUEMA) }?.removePrefix(ESQUEMA)
                ?: return null
            ID_UTILIZADOR.matchEntire(caminho)?.let { return Perfil(it.groupValues[1]) }
            if (!caminho.startsWith("confirmar-conta")) return null
            return tokenDaLigacao(caminho)?.let(::Confirmar)
        }

        /** Aceita a ligação completa recebida por email ou apenas o código. */
        fun tokenDoTexto(texto: String): String? {
            val limpo = texto.trim()
            return when {
                limpo.isEmpty() -> null
                limpo.contains("token=") -> tokenDaLigacao(limpo)
                limpo.contains("://") || limpo.any(Char::isWhitespace) -> null
                else -> limpo
            }
        }

        private fun tokenDaLigacao(ligacao: String): String? =
            TOKEN.find(ligacao)?.groupValues?.get(1)?.let { valor ->
                runCatching { URLDecoder.decode(valor, Charsets.UTF_8.name()) }.getOrNull()
            }?.takeIf { it.isNotBlank() }
    }
}
