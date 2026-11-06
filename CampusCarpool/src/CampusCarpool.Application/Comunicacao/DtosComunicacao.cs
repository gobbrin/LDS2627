namespace CampusCarpool.Application.Comunicacao;

/// <summary>Aviso acabado de criar.</summary>
public sealed record AvisoCriado(Guid Id, string Conteudo, DateTime CriadoEm);

/// <summary>Aviso na lista de uma viagem.</summary>
public sealed record AvisoDaViagem(Guid Id, Guid ViagemId, Guid CondutorId, string Conteudo, DateTime CriadoEm);

/// <summary>Mensagem individual, tal como é devolvida ao remetente e na conversa.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ViagemId">Viagem de contexto, se existir.</param>
/// <param name="RemetenteId">Autor.</param>
/// <param name="DestinatarioId">Destinatário.</param>
/// <param name="Conteudo">Texto.</param>
/// <param name="EnviadoEm">Data de envio.</param>
public sealed record MensagemIndividual(
    Guid Id,
    Guid? ViagemId,
    Guid RemetenteId,
    Guid DestinatarioId,
    string Conteudo,
    DateTime EnviadoEm);

/// <summary>Entrada da lista de conversas: o outro utilizador e a última mensagem trocada.</summary>
public sealed record ResumoConversa(
    Guid OutroUtilizadorId,
    string OutroUtilizadorNome,
    string UltimaMensagem,
    DateTime UltimaMensagemEm);
