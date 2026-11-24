namespace CampusCarpool.Application.Comum;

/// <summary>
/// Regras das listas paginadas (critério «produto minimamente viável»: nenhuma lista cresce sem limite).
/// </summary>
/// <remarks>
/// Os controllers validam a página e o tamanho com <see cref="Validar"/> antes de chamar o serviço (BE-41): os serviços
/// recebem valores já válidos. Convenção para as listas com páginas numeradas (BE-34, D-4): parâmetros <c>pagina</c>
/// (a contar de 1) e <c>tamanho</c>, e a resposta traz <c>total</c>, <c>pagina</c> e <c>tamanho</c>. A conversa usa
/// cursor (<c>antesDe</c>) e só valida o tamanho (<see cref="ValidarTamanho"/>).
/// </remarks>
public static class Paginacao
{
    /// <summary>Máximo de elementos por página, em todas as listas.</summary>
    public const int TamanhoMaximo = 100;

    /// <summary>Elementos por página quando o cliente não indica o tamanho.</summary>
    public const int TamanhoPorOmissao = 20;

    /// <summary>Valida a página e o tamanho de uma lista com páginas numeradas.</summary>
    /// <param name="pagina">Página pedida, a contar de 1.</param>
    /// <param name="tamanho">Elementos por página, de 1 a <see cref="TamanhoMaximo"/>.</param>
    /// <returns>O erro (400) da primeira regra que falha, com uma mensagem própria; <c>null</c> se estiver tudo certo.</returns>
    public static Erro? Validar(int pagina, int tamanho)
    {
        if (pagina < 1)
            return new Erro(TipoErro.DadosInvalidos, "A página começa em 1.");

        if (ValidarTamanho(tamanho) is { } tamanhoInvalido)
            return tamanhoInvalido;

        // O número de elementos a saltar ((pagina - 1) * tamanho) tem de caber num int, para a consulta à BD.
        if ((long)(pagina - 1) * tamanho > int.MaxValue)
            return new Erro(TipoErro.DadosInvalidos, "A página pedida não existe.");

        return null;
    }

    /// <summary>Valida só o tamanho da página (usado também pela conversa, paginada por cursor).</summary>
    /// <param name="tamanho">Elementos por página.</param>
    /// <returns>O erro (400) se o tamanho estiver fora de 1 a <see cref="TamanhoMaximo"/>; <c>null</c> se estiver certo.</returns>
    public static Erro? ValidarTamanho(int tamanho)
    {
        if (tamanho < 1 || tamanho > TamanhoMaximo)
            return new Erro(TipoErro.DadosInvalidos, "O tamanho da página tem de estar entre 1 e 100.");

        return null;
    }

    /// <summary>Número de elementos a saltar para chegar à página pedida (para o <c>Skip</c> da consulta).</summary>
    /// <param name="pagina">Página, a contar de 1 (já validada).</param>
    /// <param name="tamanho">Elementos por página (já validado).</param>
    public static int Saltar(int pagina, int tamanho) => (pagina - 1) * tamanho;
}

/// <summary>Uma página de uma lista: os elementos pedidos e o total de elementos da lista inteira.</summary>
/// <typeparam name="T">Tipo dos elementos.</typeparam>
/// <param name="Itens">Elementos da página pedida.</param>
/// <param name="Total">Número de elementos da lista inteira (para o cliente saber quantas páginas há).</param>
public sealed record PaginaDe<T>(IReadOnlyList<T> Itens, int Total);
