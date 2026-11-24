using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Infrastructure.Integrations;

/// <summary>
/// Descodifica a «Flexible Polyline» da HERE: o texto compacto com que o HERE Routing v8 devolve a linha da rota.
/// </summary>
/// <remarks>
/// Formato (documentação da HERE, repositório <c>heremaps/flexible-polyline</c>):
/// <list type="number">
/// <item>Cada carácter vale 6 bits, pela ordem de <see cref="Alfabeto"/>. Os 5 bits de baixo são dados; o bit 0x20
/// diz que o número continua no carácter seguinte.</item>
/// <item>O primeiro número é a versão (1); o segundo é o cabeçalho: precisão nos bits 0–3 (casas decimais) e
/// terceira dimensão (altitude, por exemplo) nos bits 4–6.</item>
/// <item>Depois vêm, para cada ponto, a diferença da latitude e da longitude em relação ao ponto anterior (e da
/// terceira dimensão, se houver), como números com sinal.</item>
/// </list>
/// </remarks>
public static class PolilinhaFlexivel
{
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    /// <summary>Converte o texto da polilinha na lista de coordenadas.</summary>
    /// <param name="texto">Polilinha devolvida pela HERE (campo <c>polyline</c> de cada secção).</param>
    /// <returns>Os pontos da linha, pela ordem do percurso.</returns>
    /// <exception cref="FormatException">Texto vazio, versão desconhecida, carácter inválido ou número incompleto.</exception>
    public static List<Coordenadas> Descodificar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            throw new FormatException("Polilinha vazia.");

        // Posição do próximo carácter a ler; os métodos de leitura avançam-na.
        int posicao = 0;

        long versao = LerSemSinal(texto, ref posicao);
        if (versao != 1)
            throw new FormatException($"Versão da polilinha desconhecida: {versao}.");

        long cabecalho = LerSemSinal(texto, ref posicao);
        int casasDecimais = (int)(cabecalho & 15);
        bool temTerceiraDimensao = ((cabecalho >> 4) & 7) != 0;

        decimal divisor = 1m;
        for (int i = 0; i < casasDecimais; i++)
            divisor *= 10m;

        var pontos = new List<Coordenadas>();
        long latitude = 0;
        long longitude = 0;

        while (posicao < texto.Length)
        {
            // Cada ponto guarda só a diferença para o anterior.
            latitude += LerComSinal(texto, ref posicao);
            longitude += LerComSinal(texto, ref posicao);

            // A altitude (ou outra terceira dimensão) não interessa para desenhar a linha: lê-se e ignora-se.
            if (temTerceiraDimensao)
                LerComSinal(texto, ref posicao);

            pontos.Add(new Coordenadas(latitude / divisor, longitude / divisor));
        }

        return pontos;
    }

    /// <summary>Lê um número sem sinal, 5 bits por carácter, até a um carácter sem o bit de continuação.</summary>
    private static long LerSemSinal(string texto, ref int posicao)
    {
        long resultado = 0;
        int deslocamento = 0;

        while (true)
        {
            if (posicao >= texto.Length)
                throw new FormatException("Polilinha incompleta.");

            int valor = Alfabeto.IndexOf(texto[posicao]);
            posicao++;

            if (valor < 0)
                throw new FormatException("Carácter inválido na polilinha.");

            resultado |= (long)(valor & 0x1F) << deslocamento;

            // Sem o bit 0x20, este carácter é o último do número.
            if ((valor & 0x20) == 0)
                return resultado;

            deslocamento += 5;
        }
    }

    /// <summary>Lê um número com sinal: o bit mais baixo diz se é negativo (codificação «zig-zag»).</summary>
    private static long LerComSinal(string texto, ref int posicao)
    {
        long valor = LerSemSinal(texto, ref posicao);

        if ((valor & 1) != 0)
            valor = ~valor;

        return valor >> 1;
    }
}
