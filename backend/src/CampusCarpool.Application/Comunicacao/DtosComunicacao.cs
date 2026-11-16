namespace CampusCarpool.Application.Comunicacao;

/// <summary>Aviso acabado de criar.</summary>
public sealed record AvisoCriado(Guid Id, string Conteudo, DateTime CriadoEm);

/// <summary>Aviso na lista de uma viagem.</summary>
/// <remarks><c>CondutorId</c> é o condutor da viagem: o aviso não o guarda (BE-33, DEF-BE-050).</remarks>
public sealed record AvisoDaViagem(Guid Id, Guid ViagemId, Guid CondutorId, string Conteudo, DateTime CriadoEm);

/// <summary>Mensagem individual, tal como é devolvida ao remetente e na conversa.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="RemetenteId">Autor.</param>
/// <param name="DestinatarioId">Destinatário.</param>
/// <param name="Conteudo">Texto.</param>
/// <param name="EnviadoEm">Data de envio.</param>
public sealed record MensagemIndividual(
    Guid Id,
    Guid RemetenteId,
    Guid DestinatarioId,
    string Conteudo,
    DateTime EnviadoEm);

/// <summary>Entrada da lista de conversas: o outro utilizador e a última mensagem trocada.</summary>
/// <remarks><c>UltimaMensagemId</c> (I112, BE-13) serve para o cliente saber se a conversa tem mensagens novas.</remarks>
public sealed record ResumoConversa(
    Guid OutroUtilizadorId,
    string OutroUtilizadorNome,
    Guid UltimaMensagemId,
    string UltimaMensagem,
    DateTime UltimaMensagemEm);
