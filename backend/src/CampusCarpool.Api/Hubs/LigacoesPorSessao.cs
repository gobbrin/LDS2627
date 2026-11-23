using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace CampusCarpool.Api.Hubs;

/// <summary>
/// Lista das ligações abertas ao <see cref="PedidosHub"/>, cada uma com o <c>jti</c> do token com que entrou, para o
/// fim de sessão fechar logo as ligações dessa sessão (BE-18, DEF-BE-056, RNF-SEG-03).
/// </summary>
/// <remarks>
/// Registada como Singleton: o hub acrescenta e retira ligações; o <c>AutenticacaoController</c> fecha as da sessão
/// terminada. Sem isto, uma ligação aberta continuava a receber eventos até o token expirar (até 60 min).
/// Só em memória: chega porque as ligações também só existem neste processo.
/// </remarks>
public sealed class LigacoesPorSessao
{
    // Chave: id da ligação SignalR. Valor: o jti da sessão e o contexto que permite fechar a ligação.
    private readonly ConcurrentDictionary<string, (string Jti, HubCallerContext Contexto)> _ligacoes = new();

    /// <summary>Regista uma ligação acabada de abrir. Uma ligação sem <c>jti</c> não é registada.</summary>
    /// <param name="contexto">Contexto da ligação, dado pelo hub.</param>
    public void Registar(HubCallerContext contexto)
    {
        string? jti = contexto.User?.FindFirstValue(JwtRegisteredClaimNames.Jti);

        if (string.IsNullOrEmpty(jti))
            return;

        _ligacoes[contexto.ConnectionId] = (jti, contexto);
    }

    /// <summary>Esquece uma ligação que fechou.</summary>
    /// <param name="idLigacao">Id da ligação SignalR.</param>
    public void Remover(string idLigacao)
    {
        _ligacoes.TryRemove(idLigacao, out _);
    }

    /// <summary>Fecha todas as ligações abertas com o token da sessão terminada.</summary>
    /// <param name="jti">Identificador do token revogado no fim de sessão.</param>
    /// <returns>Quantas ligações foram fechadas.</returns>
    public int FecharDaSessao(string jti)
    {
        int fechadas = 0;

        foreach (KeyValuePair<string, (string Jti, HubCallerContext Contexto)> ligacao in _ligacoes)
        {
            if (ligacao.Value.Jti != jti)
                continue;

            // Abort fecha a ligação do lado do servidor; o OnDisconnectedAsync do hub retira-a da lista.
            ligacao.Value.Contexto.Abort();
            _ligacoes.TryRemove(ligacao.Key, out _);
            fechadas++;
        }

        return fechadas;
    }
}
