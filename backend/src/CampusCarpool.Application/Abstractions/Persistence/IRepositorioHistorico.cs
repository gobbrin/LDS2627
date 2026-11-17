using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Abstractions.Persistence;

/// <summary>Consultas só de leitura do histórico pessoal (F009), cada uma paginada (D-4).</summary>
/// <remarks>
/// Devolve entidades sem seguimento; o <c>HistoricoService</c> monta as respostas da API (BE-07: o repositório não
/// conhece os DTOs). Cada método recebe a página (a contar de 1) e o tamanho, já validados pelo controller, e devolve
/// os elementos dessa página com o total da lista inteira.
/// </remarks>
public interface IRepositorioHistorico
{
    /// <summary>Viagens em que o utilizador foi condutor, da partida mais recente para a mais antiga (RF-F009-02, RF-F009-06).</summary>
    Task<PaginaDe<Viagem>> ListarViagensComoCondutorAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>
    /// Participações em que o utilizador chegou a ser aceite como passageiro, com a viagem carregada, da partida mais
    /// recente para a mais antiga (RF-F009-03, D-029).
    /// </summary>
    Task<PaginaDe<Participacao>> ListarParticipacoesComoPassageiroAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Pedidos feitos pelo utilizador, em todos os estados, com a viagem carregada, do mais recente para o mais antigo (RF-F009-04).</summary>
    Task<PaginaDe<Participacao>> ListarPedidosEfetuadosAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Pedidos recebidos nas viagens do utilizador, com a viagem carregada, do mais recente para o mais antigo (RF-F009-04).</summary>
    Task<PaginaDe<Participacao>> ListarPedidosRecebidosAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Notificações pessoais, das mais recentes para as mais antigas (RF-F009-05).</summary>
    Task<PaginaDe<Notificacao>> ListarNotificacoesAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct);

    /// <summary>Avisos das viagens em que o utilizador é condutor ou chegou a ser aceite como passageiro, mesmo que tenha cancelado depois (RF-F009-05, DEF-BE-024).</summary>
    Task<PaginaDe<Aviso>> ListarAvisosAsync(Guid utilizadorId, int pagina, int tamanho, CancellationToken ct);
}
