using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Domain.Rules;

/// <summary>Quem pode consultar uma viagem (AD-003 §5.1, RF-F003-02).</summary>
public static class PoliticaDeVisibilidade
{
    /// <summary>Glossário: Ativa, com data e hora futuras e pelo menos um lugar disponível (RF-F003-02).</summary>
    public static bool Pesquisavel(Viagem viagem, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return viagem.Estado == nameof(EstadoViagem.Ativa)
            && viagem.PartidaEm > agora
            && viagem.LugaresDisponiveis > 0;
    }

    /// <summary>
    /// A7, G20: o condutor consulta sempre a sua viagem e os outros utilizadores consultam-na enquanto for pesquisável.
    /// Quando isto é falso, ainda falta ver se o utilizador tem algum pedido na viagem, que também dá acesso, para o
    /// passageiro não a perder quando ela enche.
    /// </summary>
    public static bool VisivelSemPedido(Viagem viagem, Guid? utilizadorId, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return viagem.CondutorId == utilizadorId || Pesquisavel(viagem, agora);
    }
}
