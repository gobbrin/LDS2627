namespace CampusCarpool.Application.Rotas;

/// <summary>Configuração da estimativa de custo (secção <c>Custos</c>).</summary>
public sealed class OpcoesCustos
{
    /// <summary>Nome da secção na configuração.</summary>
    public const string Seccao = "Custos";

    /// <summary>Preço por quilómetro, em euros (P-06). Sem ele, a estimativa não pode ser calculada.</summary>
    public decimal? PrecoPorKm { get; set; }
}
