namespace CampusCarpool.Domain.Rules;

/// <summary>Conversão entre UTC, em que as datas são guardadas, e a hora de Portugal continental (RF-F003-01, P-12).</summary>
/// <remarks>
/// O utilizador pesquisa pelo dia e pela hora do relógio dele, que no verão estão 1 h à frente de UTC. A hora mínima da
/// pesquisa pode vir sem data, por isso a comparação tem de ser feita na BD com o minuto do dia já convertido
/// (<c>Viagem.PartidaMinutoDoDia</c>): SQLite e SQL Server não partilham uma conversão de fuso.
/// </remarks>
public static class HoraDePortugal
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");

    /// <summary>Instante UTC em que começa o dia indicado em Portugal.</summary>
    /// <param name="dia">Dia em Portugal.</param>
    public static DateTime InicioDoDiaEmUtc(DateOnly dia) =>
        TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Fuso);

    /// <summary>Minutos desde a meia-noite em Portugal, de 0 a 1439.</summary>
    /// <param name="utc">Instante em UTC; um valor sem <c>Kind</c> é tratado como UTC.</param>
    public static int MinutoDoDia(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Fuso);
        return local.Hour * 60 + local.Minute;
    }
}
