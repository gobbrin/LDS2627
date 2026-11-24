using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CampusCarpool.Api.Hubs;

/// <summary>
/// Bilhetes de ligação ao <see cref="PedidosHub"/> (DEF-BE-055): um valor aleatório, de uso único e válido 30 s, que
/// o cliente põe em <c>?bilhete=</c> no lugar do JWT.
/// </summary>
/// <remarks>
/// O WebSocket do browser não envia cabeçalhos, por isso a credencial tem de ir no endereço, que fica nos logs de
/// acesso. Com o JWT ali, quem lesse os logs podia usá-lo durante 60 min em toda a API. O bilhete só serve uma vez,
/// só no hub e só durante 30 s. Cada bilhete guarda o JWT do pedido que o emitiu: ao ligar, o <c>Program.cs</c>
/// troca o bilhete pelo JWT, que o JwtBearer valida como sempre (assinatura, validade, revogação e <c>jti</c> para o
/// fim de sessão fechar a ligação, BE-18).
/// Registado como Singleton e só em memória, como a <see cref="LigacoesPorSessao"/>: as ligações também só existem
/// neste processo. O relógio é injetado para os testes da validade não terem de esperar.
/// </remarks>
/// <param name="relogio">Relógio do servidor (<see cref="TimeProvider.System"/>; nos testes, um relógio controlado).</param>
public sealed class BilhetesHub(TimeProvider relogio)
{
    /// <summary>Tempo de vida de um bilhete: chega para o cliente abrir a ligação logo a seguir a pedi-lo.</summary>
    public static readonly TimeSpan Validade = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Máximo de bilhetes por usar guardados ao mesmo tempo, para a memória não crescer sem limite. Com o limite de
    /// 100 pedidos por minuto por utilizador, só se atinge com centenas de utilizadores a pedir bilhetes sem os usar.
    /// </summary>
    public const int MaximoDeBilhetes = 10_000;

    // Chave: o bilhete. Valor: o JWT do pedido que o emitiu e o fim da validade.
    private readonly ConcurrentDictionary<string, (string Jwt, DateTimeOffset ExpiraEm)> _bilhetes = new();

    /// <summary>Cria um bilhete novo para o JWT indicado.</summary>
    /// <param name="jwt">JWT, já validado, do pedido autenticado que pede o bilhete.</param>
    /// <returns>O bilhete e o fim da validade (UTC), ou <c>null</c> se já há <see cref="MaximoDeBilhetes"/> por usar.</returns>
    public (string Bilhete, DateTimeOffset ExpiraEm)? Emitir(string jwt)
    {
        DateTimeOffset agora = relogio.GetUtcNow();

        // Antes de recusar por falta de espaço, retira os que já expiraram sem ser usados.
        if (_bilhetes.Count >= MaximoDeBilhetes)
            RemoverExpirados(agora);

        if (_bilhetes.Count >= MaximoDeBilhetes)
            return null;

        // 32 bytes aleatórios de um gerador criptográfico: impossível de adivinhar. Base64Url, para ir no endereço sem
        // ser codificado.
        string bilhete = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        DateTimeOffset expiraEm = agora + Validade;

        _bilhetes[bilhete] = (jwt, expiraEm);

        return (bilhete, expiraEm);
    }

    /// <summary>Gasta um bilhete: devolve o JWT associado e apaga o bilhete, para não voltar a servir.</summary>
    /// <param name="bilhete">Valor recebido em <c>?bilhete=</c>.</param>
    /// <returns>O JWT, ou <c>null</c> se o bilhete não existe, já foi usado ou expirou.</returns>
    public string? Consumir(string bilhete)
    {
        // TryRemove é atómico: dois pedidos com o mesmo bilhete ao mesmo tempo, só um o obtém.
        if (!_bilhetes.TryRemove(bilhete, out (string Jwt, DateTimeOffset ExpiraEm) guardado))
            return null;

        if (relogio.GetUtcNow() >= guardado.ExpiraEm)
            return null;

        return guardado.Jwt;
    }

    // Apaga os bilhetes cuja validade já acabou.
    private void RemoverExpirados(DateTimeOffset agora)
    {
        foreach (KeyValuePair<string, (string Jwt, DateTimeOffset ExpiraEm)> entrada in _bilhetes)
        {
            if (agora >= entrada.Value.ExpiraEm)
                _bilhetes.TryRemove(entrada.Key, out _);
        }
    }
}
