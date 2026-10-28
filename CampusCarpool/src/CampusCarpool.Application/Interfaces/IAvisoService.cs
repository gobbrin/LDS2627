using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Interfaces;

/// <summary>Resultado da criação de um aviso e das notificações associadas.</summary>
public enum ResultadoCriacaoAviso
{
	Sucesso,
	ViagemNaoEncontrada,
	NaoAutorizado,
	ConteudoInvalido
}

/// <summary>Regras de aplicação para avisos do condutor.</summary>
public interface IAvisoService
{
	/// <summary>Cria um aviso e uma notificação para cada passageiro aceite.</summary>
	Task<(ResultadoCriacaoAviso Resultado, Aviso? Aviso)> CriarAsync(Guid viagemId, Guid condutorId, string? conteudo, CancellationToken ct);

	/// <summary>Consulta os avisos de uma viagem para um utilizador autorizado.</summary>
	Task<(ResultadoConsultaAvisos Resultado, IReadOnlyList<Aviso> Avisos)> ListarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);
}

/// <summary>Resultado da consulta de avisos por viagem.</summary>
public enum ResultadoConsultaAvisos
{
	Sucesso,
	ViagemNaoEncontrada,
	NaoAutorizado
}
