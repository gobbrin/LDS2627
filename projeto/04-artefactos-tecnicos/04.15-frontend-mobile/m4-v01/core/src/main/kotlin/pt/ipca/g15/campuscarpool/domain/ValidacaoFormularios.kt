package pt.ipca.g15.campuscarpool.domain

/**
 * Validação no cliente, só para usabilidade: quem decide é o backend, incluindo o domínio
 * académico do email, que vem de configuração e por isso não se fixa aqui.
 */
object ValidacaoFormularios {
    const val MAX_NOME = 100
    const val MAX_EMAIL = 255
    const val MAX_CURSO_INSTITUICAO = 150
    const val MAX_URL = 2048
    const val MAX_DESCRICAO = 100
    const val MAX_LIGACOES = 5
    private const val MIN_PASSWORD = 8
    private const val REGRA_PASSWORD = "Usa pelo menos 8 caracteres, com uma letra e um dígito."
    private val FORMATO_EMAIL = Regex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")
    private val FORMATO_URL = Regex("^https?://[^\\s/?#]+\\S*$", RegexOption.IGNORE_CASE)

    fun registo(
        nome: String,
        email: String,
        password: String,
        confirmacao: String
    ): Map<String, String> = buildMap {
        erroNome(nome)?.let { put("nome", it) }
        email(email)?.let { put("email", it) }
        erroPassword(password)?.let { put("password", it) }
        when {
            confirmacao.isEmpty() -> put("confirmacao", "Repete a palavra-passe.")
            confirmacao != password -> put("confirmacao", "As palavras-passe não coincidem.")
        }
    }

    fun email(email: String): String? {
        val limpo = email.trim()
        return when {
            limpo.isEmpty() -> "Indica o teu email académico."
            limpo.length > MAX_EMAIL || !FORMATO_EMAIL.matches(limpo) -> "Indica um email válido."
            else -> null
        }
    }

    fun perfil(nome: String, curso: String, instituicao: String): Map<String, String> = buildMap {
        erroNome(nome)?.let { put("nome", it) }
        if (curso.trim().length > MAX_CURSO_INSTITUICAO) {
            put("curso", "O curso tem no máximo $MAX_CURSO_INSTITUICAO caracteres.")
        }
        if (instituicao.trim().length > MAX_CURSO_INSTITUICAO) {
            put("instituicao", "A instituição tem no máximo $MAX_CURSO_INSTITUICAO caracteres.")
        }
    }

    fun ligacao(url: String, descricao: String): Map<String, String> = buildMap {
        val limpo = url.trim()
        when {
            limpo.isEmpty() -> put("url", "Indica o endereço da ligação.")
            limpo.length > MAX_URL -> put("url", "O endereço tem no máximo $MAX_URL caracteres.")
            !eLigacaoWeb(limpo) -> put(
                "url",
                "Indica um endereço completo, a começar por http:// ou https://."
            )
        }
        if (descricao.trim().length > MAX_DESCRICAO) {
            put("descricao", "A descrição tem no máximo $MAX_DESCRICAO caracteres.")
        }
    }

    /** Só os endereços http e https podem ser abertos a partir de um perfil. */
    fun eLigacaoWeb(url: String): Boolean = FORMATO_URL.matches(url.trim())

    private fun erroNome(nome: String): String? {
        val limpo = nome.trim()
        return when {
            limpo.isEmpty() -> "Indica o teu nome."
            limpo.length > MAX_NOME -> "O nome tem no máximo $MAX_NOME caracteres."
            else -> null
        }
    }

    private fun erroPassword(password: String): String? = when {
        password.isEmpty() -> "Indica uma palavra-passe."
        password.length < MIN_PASSWORD -> REGRA_PASSWORD
        !password.any(Char::isLetter) || !password.any(Char::isDigit) -> REGRA_PASSWORD
        else -> null
    }
}
