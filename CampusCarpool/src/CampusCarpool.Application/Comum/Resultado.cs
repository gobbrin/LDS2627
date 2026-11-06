namespace CampusCarpool.Application.Comum;

/// <summary>Motivo pelo qual uma operação foi recusada; a Api traduz cada valor num código HTTP.</summary>
public enum TipoErro
{
    /// <summary>Os dados não cumprem as regras (400).</summary>
    DadosInvalidos,
    /// <summary>Credenciais inválidas (401).</summary>
    NaoAutenticado,
    /// <summary>O utilizador não tem permissão (403).</summary>
    Proibido,
    /// <summary>O recurso não existe ou o utilizador não o pode ver (404).</summary>
    NaoEncontrado,
    /// <summary>O estado atual não permite a operação (409).</summary>
    Conflito,
    /// <summary>Os dados estão bem formados, mas não têm correspondência, como um local desconhecido (422).</summary>
    NaoProcessavel,
    /// <summary>Um serviço externo respondeu com erro (502).</summary>
    FalhaServicoExterno,
    /// <summary>Um serviço externo não está disponível nem configurado (503).</summary>
    ServicoIndisponivel,
    /// <summary>Um serviço externo não respondeu a tempo (504).</summary>
    TempoEsgotado
}

/// <summary>Erro previsto de uma operação.</summary>
/// <param name="Tipo">Motivo.</param>
/// <param name="Mensagem">Texto para o cliente; sem ele, a resposta HTTP não tem corpo.</param>
public sealed record Erro(TipoErro Tipo, string? Mensagem = null);

/// <summary>Resultado de uma operação sem valor a devolver: sucesso ou um <see cref="Comum.Erro"/>.</summary>
/// <remarks>
/// Os serviços usam isto para os erros previstos (decisão D3 da reestruturação), em vez das exceções do modelo de
/// classes §6: as respostas HTTP mantêm-se. As exceções ficam para o que é inesperado.
/// </remarks>
public class Resultado
{
    /// <summary>Cria o resultado; sem erro, é um sucesso.</summary>
    protected Resultado(Erro? erro) => Erro = erro;

    /// <summary>Operação concluída.</summary>
    public static Resultado Sucesso { get; } = new(null);

    /// <summary>O erro, ou <c>null</c> em caso de sucesso.</summary>
    public Erro? Erro { get; }

    /// <summary>Um erro é um resultado falhado.</summary>
    public static implicit operator Resultado(Erro erro) => new(erro);
}

/// <summary>Resultado de uma operação que devolve um valor: o valor ou um <see cref="Comum.Erro"/>.</summary>
/// <typeparam name="T">Tipo do valor.</typeparam>
public sealed class Resultado<T> : Resultado
{
    private readonly T? valor;

    private Resultado(T? valor, Erro? erro)
        : base(erro)
    {
        this.valor = valor;
    }

    /// <summary>O valor; só existe em caso de sucesso.</summary>
    /// <exception cref="InvalidOperationException">O resultado é um erro.</exception>
    public T Valor => Erro is null
        ? valor!
        : throw new InvalidOperationException("Um resultado com erro não tem valor.");

    /// <summary>Um valor é um resultado com sucesso.</summary>
    public static implicit operator Resultado<T>(T valor) => new(valor, null);

    /// <summary>Um erro é um resultado falhado.</summary>
    public static implicit operator Resultado<T>(Erro erro) => new(default, erro);
}
