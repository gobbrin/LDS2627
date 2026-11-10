package pt.ipca.g15.campuscarpool.domain

import java.time.Instant
import pt.ipca.g15.campuscarpool.domain.model.CamposViagem

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
    const val MAX_LOCAL = 200

    /** P-11: lugares oferecidos por viagem. */
    const val MIN_LUGARES = 1
    const val MAX_LUGARES = 8
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

    /** RF-F002-02, 03, 04 e 16; [ocupados] são as participações aceites (RF-F002-08). */
    fun viagem(
        campos: CamposViagem,
        ocupados: Int = 0,
        agora: Instant = Instant.now()
    ): Map<String, String> = buildMap {
        erroLocal(campos.origem, "a origem", "A origem")?.let { put("origem", it) }
        val erroDestino = erroLocal(campos.destino, "o destino", "O destino")
        when {
            erroDestino != null -> put("destino", erroDestino)
            mesmoLocal(campos.origem, campos.destino) ->
                put("destino", "A origem e o destino não podem ser o mesmo local.")
        }
        erroData(campos.data)?.let { put("data", it) }
        erroHora(campos.hora)?.let { put("hora", it) }
        val partida = DataHoraFormatter.instante(campos.data, campos.hora)
        if (partida != null && !partida.isAfter(agora)) {
            put("hora", "A data e a hora têm de ser posteriores ao momento atual.")
        }
        erroLugares(campos.lugares, ocupados)?.let { put("lugares", it) }
    }

    private fun erroLocal(valor: String, nome: String, nomeInicial: String): String? {
        val limpo = valor.trim()
        return when {
            limpo.isEmpty() -> "Indica $nome."
            limpo.length > MAX_LOCAL -> "$nomeInicial tem no máximo $MAX_LOCAL caracteres."
            else -> null
        }
    }

    private fun mesmoLocal(origem: String, destino: String) =
        origem.trim().lowercase() == destino.trim().lowercase()

    private fun erroData(data: String): String? = when {
        data.isBlank() -> "Indica a data da viagem."
        DataHoraFormatter.lerData(data) == null -> "Indica a data no formato dia/mês/ano."
        else -> null
    }

    private fun erroHora(hora: String): String? = when {
        hora.isBlank() -> "Indica a hora da viagem."
        DataHoraFormatter.lerHora(hora) == null -> "Indica a hora no formato hh:mm."
        else -> null
    }

    private fun erroLugares(lugares: String, ocupados: Int): String? {
        val limpo = lugares.trim()
        // Só algarismos de 0 a 9; o valor pode exceder um Int, e aí fica acima do máximo.
        val numero = if (limpo.all {
                it in '0'..'9'
            }
        ) {
            limpo.toIntOrNull() ?: Int.MAX_VALUE
        } else {
            null
        }
        return when {
            limpo.isEmpty() -> "Indica o número de lugares."
            numero == null -> "Indica um número inteiro de lugares."
            numero < MIN_LUGARES -> "Indica pelo menos $MIN_LUGARES lugar."
            numero > MAX_LUGARES -> "O máximo é $MAX_LUGARES lugares."
            numero < ocupados ->
                "Já tens $ocupados lugares ocupados nesta viagem. Não podes oferecer menos."
            else -> null
        }
    }

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
