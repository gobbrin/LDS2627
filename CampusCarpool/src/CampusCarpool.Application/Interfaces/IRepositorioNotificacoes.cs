using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

public interface IRepositorioNotificacoes {
    
    //Guarda uma nova notificação na base de dados.
    Task AdicionarAsync(Notificacao notificacao, CancellationToken ct);

    //Consulta as notificações de um utilizador, ordenadas da mais recente para a mais antiga.
    Task<IReadOnlyList<Notificacao>> ListarAsync(Guid utilizadorId, int pagina, int tamanhoPagina, CancellationToken ct);

    //Obtém uma notificação pertencente ao utilizador indicado.
    Task<Notificacao?> ObterAsync(Guid notificacaoId, Guid utilizadorId, CancellationToken ct);

    //Obtém as notificações ainda não lidas de um utilizador.
    Task<IReadOnlyList<Notificacao>> ListarNaoLidasAsync(Guid utilizadorId, CancellationToken ct);

    //Conta as notificações de um utilizador.
    Task<int> ContarAsync(Guid utilizadorId, CancellationToken ct);

    //Conta as notificações não lidas de um utilizador.
    Task<int> ContarNaoLidasAsync(Guid utilizadorId, CancellationToken ct);

    //Guarda as alterações efetuadas às notificações.
    Task GuardarAlteracoesAsync(CancellationToken ct);
}