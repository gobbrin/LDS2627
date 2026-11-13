package pt.ipca.g15.campuscarpool.domain

import java.time.Instant
import java.time.LocalDate
import java.time.LocalTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.time.format.ResolverStyle
import java.util.Locale
import pt.ipca.g15.campuscarpool.domain.model.CamposPartida

/**
 * A API só aceita e devolve datas em UTC com `Z`; a interface trabalha na hora de Portugal
 * continental (RNF-FIA-03), seja qual for o fuso do telemóvel.
 */
object DataHoraFormatter {
    val PORTUGAL: ZoneId = ZoneId.of("Europe/Lisbon")
    private val DATA =
        DateTimeFormatter.ofPattern("dd/MM/uuuu").withResolverStyle(ResolverStyle.STRICT)
    private val HORA = DateTimeFormatter.ofPattern("HH:mm").withResolverStyle(ResolverStyle.STRICT)
    private val APRESENTACAO = DateTimeFormatter.ofPattern("dd/MM/uuuu 'às' HH:mm")
    private val PT = Locale.forLanguageTag("pt-PT")

    fun lerData(texto: String): LocalDate? =
        runCatching { LocalDate.parse(texto.trim(), DATA) }.getOrNull()

    fun lerHora(texto: String): LocalTime? =
        runCatching { LocalTime.parse(texto.trim(), HORA) }.getOrNull()

    /** O instante indicado nos campos, ou nulo se a data ou a hora forem inválidas. */
    fun instante(data: String, hora: String, zona: ZoneId = PORTUGAL): Instant? {
        val dia = lerData(data) ?: return null
        val horas = lerHora(hora) ?: return null
        return dia.atTime(horas).atZone(zona).toInstant()
    }

    /** Valor para a API, com `Z`, por exemplo `2026-12-01T09:30:00Z`. */
    fun paraUtc(data: String, hora: String, zona: ZoneId = PORTUGAL): String? =
        instante(data, hora, zona)?.toString()

    fun instanteDe(partidaEm: String): Instant? =
        runCatching { OffsetDateTime.parse(partidaEm).toInstant() }.getOrNull()

    /** Valores dos campos do formulário para uma partida devolvida pela API. */
    fun campos(partidaEm: String, zona: ZoneId = PORTUGAL): CamposPartida? =
        instanteDe(partidaEm)?.atZone(zona)?.let {
            CamposPartida(DATA.format(it), HORA.format(it))
        }

    /** Devolve o texto recebido se não o conseguir ler: é melhor mostrá-lo do que escondê-lo. */
    fun formatar(partidaEm: String, zona: ZoneId = PORTUGAL): String =
        instanteDe(partidaEm)?.atZone(zona)?.let(APRESENTACAO::format) ?: partidaEm

    /** O calendário devolve a meia-noite UTC do dia escolhido. */
    fun dataDoCalendario(millis: Long): String =
        DATA.format(Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC))

    fun paraCalendario(data: String): Long? =
        lerData(data)?.atStartOfDay(ZoneOffset.UTC)?.toInstant()?.toEpochMilli()

    fun hora(horas: Int, minutos: Int): String = HORA.format(LocalTime.of(horas, minutos))

    /** Texto curto da rota calculada; vazio se a API não tiver distância nem duração. */
    fun rota(distanciaKm: Double?, duracaoMinutos: Int?): String = listOfNotNull(
        distanciaKm?.let { String.format(PT, "%.1f km", it) },
        duracaoMinutos?.let {
            val horas = it / 60
            if (horas > 0) "cerca de $horas h ${it % 60} min" else "cerca de $it min"
        }
    ).joinToString(" · ")
}
