using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

//Define as operações de persistência das mensagens individuais.
public interface IRepositorioMensagens {

    //Guarda uma mensagem na base de dados.
    Task AdicionarAsync( Mensagem mensagem, CancellationToken ct);

    //Obtém as mensagens trocadas entre dois utilizadores,
    //ordenadas cronologicamente.
    Task<IReadOnlyList<Mensagem>> ObterConversaAsync( Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct);
}