using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Domain.Rules;

/// <summary>Quem pode ver o quê numa viagem (AD-003, P-13).</summary>
public static class PoliticaDeVisibilidade
{
    /// <summary>
    /// A7, G20: o condutor consulta sempre a sua viagem e os outros utilizadores consultam-na enquanto for pesquisável.
    /// Quando isto é falso, ainda falta ver se o utilizador tem algum pedido na viagem, que também dá acesso, para o
    /// passageiro não a perder quando ela enche.
    /// </summary>
    public static bool VisivelSemPedido(Viagem viagem, Guid? utilizadorId, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return viagem.CondutorId == utilizadorId || viagem.EstaDisponivelParaPesquisa(agora);
    }

    /// <summary>
    /// P-13, RF-F008-09: só o condutor e os passageiros aceites veem os pontos das participações, a lista de passageiros
    /// e os avisos da viagem (RF-F003-07, RF-F005-06).
    /// </summary>
    /// <param name="viagem">Viagem.</param>
    /// <param name="utilizadorId">Utilizador.</param>
    /// <param name="passageiroAceite">O utilizador tem participação aceite na viagem.</param>
    public static bool ParticipaNaViagem(Viagem viagem, Guid utilizadorId, bool passageiroAceite)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return viagem.CondutorId == utilizadorId || passageiroAceite;
    }
}
