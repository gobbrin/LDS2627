using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Comunicacao;

/// <summary>Resultado da criação de um aviso e das notificações associadas.</summary>
/// <remarks>O <c>AvisosController</c> traduz cada valor num código HTTP (201, 404, 403, 400).</remarks>
public enum ResultadoCriacaoAviso
{
    /// <summary>Aviso criado e passageiros notificados.</summary>
    Sucesso,
    /// <summary>A viagem não existe.</summary>
    ViagemNaoEncontrada,
    /// <summary>O utilizador não é o condutor da viagem (RF-F005-07).</summary>
    NaoAutorizado,
    /// <summary>Texto vazio ou com mais de 100 caracteres (P-28, RF-F005-10).</summary>
    ConteudoInvalido
}

/// <summary>Regras de aplicação para avisos do condutor.</summary>
/// <remarks>Implementado por <c>Infrastructure/Services/AvisoService.cs</c> (AP-04).</remarks>
public interface IAvisoService
{
    /// <summary>Cria um aviso e uma notificação para cada passageiro aceite.</summary>
    /// <remarks>RF-F005-05, RF-F006-05. O aviso é aceite mesmo que a viagem não esteja <c>Ativa</c> (DEF-BE-017, fica para m4).</remarks>
    Task<(ResultadoCriacaoAviso Resultado, Aviso? Aviso)> CriarAsync(Guid viagemId, Guid condutorId, string? conteudo, CancellationToken ct);

    /// <summary>Consulta os avisos de uma viagem para um utilizador autorizado.</summary>
    /// <remarks>Autorizados: o condutor e os passageiros com participação aceite (RF-F005-06).</remarks>
    Task<(ResultadoConsultaAvisos Resultado, IReadOnlyList<Aviso> Avisos)> ListarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);
}

/// <summary>Resultado da consulta de avisos por viagem.</summary>
public enum ResultadoConsultaAvisos
{
    /// <summary>Avisos devolvidos.</summary>
    Sucesso,
    /// <summary>A viagem não existe.</summary>
    ViagemNaoEncontrada,
    /// <summary>O utilizador não é o condutor nem passageiro aceite.</summary>
    NaoAutorizado
}
