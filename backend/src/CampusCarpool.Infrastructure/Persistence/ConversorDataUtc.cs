using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CampusCarpool.Infrastructure.Persistence;

/// <summary>Faz todas as datas lidas da BD saírem com <c>Kind = Utc</c> e, por isso, com <c>Z</c> no JSON (DC-42).</summary>
/// <remarks>As colunas são <c>datetime2</c>, sem fuso: guardam sempre UTC, e sem isto a leitura perdia essa informação.</remarks>
internal sealed class ConversorDataUtc() : ValueConverter<DateTime, DateTime>(
    valor => valor.Kind == DateTimeKind.Local ? valor.ToUniversalTime() : valor,
    valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
