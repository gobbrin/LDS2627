using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>
/// Testes das correções da I112 que não mudam uma resposta da API: regras da análise estática (BE-19), código e
/// configuração retirados (BE-21, BE-22, BE-23, BE-43) e a verificação de conta confirmada só no login (BE-24).
/// Cada teste falha se alguém voltar a pôr o que foi retirado ou tirar a regra que foi posta.
/// </summary>
public sealed partial class CorrecoesDeConfiguracaoECodigoTests
{
    // «CA1502: 15» → limite 15.
    [GeneratedRegex(@"^CA1502:\s*(\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex LimiteCa1502();

    /// <summary>Pasta <c>backend/</c>: sobe a partir da pasta dos binários até encontrar a solução.</summary>
    private static string PastaBackend()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);

        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "CampusCarpool.slnx")))
            pasta = pasta.Parent;

        Assert.NotNull(pasta);
        return pasta!.FullName;
    }

    private static string Ler(string ficheiro) => File.ReadAllText(Path.Combine(PastaBackend(), ficheiro));

    /// <summary>
    /// Devolve as linhas da secção <paramref name="secao"/> do <c>.editorconfig</c> (até à secção seguinte),
    /// sem comentários.
    /// </summary>
    private static List<string> LinhasDaSecao(string editorconfig, string secao)
    {
        var linhas = new List<string>();
        var dentro = false;

        foreach (var linha in editorconfig.Split('\n').Select(l => l.Trim()))
        {
            if (linha.StartsWith('['))
            {
                dentro = linha == secao;
                continue;
            }

            if (dentro && linha.Length > 0 && !linha.StartsWith('#')) linhas.Add(linha.Replace(" ", "", StringComparison.Ordinal));
        }

        return linhas;
    }

    /// <summary>
    /// BE-19: o código de produção (<c>[src/**.cs]</c>) tem as regras de complexidade (CA1502, limite 15 ou menos),
    /// manutenção (CA1505), acoplamento (CA1506, só aviso) e código morto (IDE0051, IDE0052, IDE0060), e o ficheiro dos
    /// limites entra na análise.
    /// </summary>
    [Fact]
    public void I112_101_AnaliseEstatica_TemAsRegrasDeComplexidadeECodigoMorto()
    {
        var regras = LinhasDaSecao(Ler(".editorconfig"), "[src/**.cs]");

        foreach (var regra in new[] { "CA1502", "CA1505", "CA1506", "IDE0051", "IDE0052", "IDE0060" })
            Assert.Contains($"dotnet_diagnostic.{regra}.severity=warning", regras);

        // O limite da complexidade não pode subir acima de 15 (CodeMetricsConfig.txt).
        var limite = LimiteCa1502().Match(Ler("CodeMetricsConfig.txt"));
        Assert.True(limite.Success, "CodeMetricsConfig.txt sem a linha «CA1502: <limite>».");
        Assert.InRange(int.Parse(limite.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), 1, 15);

        // Os avisos param o build, exceto o CA1506; e o ficheiro dos limites é lido pelos analisadores.
        var props = Ler("Directory.Build.props");
        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", props, StringComparison.Ordinal);
        Assert.Contains("<WarningsNotAsErrors>$(WarningsNotAsErrors);CA1506</WarningsNotAsErrors>", props, StringComparison.Ordinal);
        Assert.Contains("<AdditionalFiles Include=\"$(MSBuildThisFileDirectory)CodeMetricsConfig.txt\" />", props, StringComparison.Ordinal);
    }

    /// <summary>
    /// BE-21 e BE-43: os métodos de repositório sem uso foram retirados (<c>ListarPorPassageiroAsync</c> das
    /// participações; <c>ListarRecebidasAsync</c> das avaliações, que só servia a lista que BE-43 tirou da resposta).
    /// </summary>
    [Theory]
    [InlineData(typeof(IRepositorioParticipacoes), "ListarPorPassageiroAsync")]
    [InlineData(typeof(IRepositorioAvaliacoes), "ListarRecebidasAsync")]
    public void I112_102_MetodoDeRepositorioSemUso_NaoExiste(Type repositorio, string metodo)
    {
        Assert.Null(repositorio.GetMethod(metodo, BindingFlags.Public | BindingFlags.Instance));
    }

    /// <summary>
    /// BE-22 e BE-23 (DEF-BE-050/051): <c>Mensagem.ViagemId</c> e <c>Aviso.CondutorId</c> não existem na classe nem no
    /// modelo da base de dados, e <c>Maps:Provider</c>/<c>Maps:BaseUrl</c>, que não tinham efeito, saíram da configuração.
    /// </summary>
    [Fact]
    public void I112_103_CamposEConfiguracaoSemUso_ForamRetirados()
    {
        Assert.Null(typeof(Mensagem).GetProperty("ViagemId"));
        Assert.Null(typeof(Aviso).GetProperty("CondutorId"));

        using var db = new CampusCarpoolDbContext(new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlServer("Server=(local);Database=SemLigacao;")
            .Options);
        Assert.Null(db.Model.FindEntityType(typeof(Mensagem))!.FindProperty("ViagemId"));
        Assert.Null(db.Model.FindEntityType(typeof(Aviso))!.FindProperty("CondutorId"));

        using var configuracao = JsonDocument.Parse(Ler("src/CampusCarpool.Api/appsettings.json"));
        var maps = configuracao.RootElement.GetProperty("Maps");
        Assert.False(maps.TryGetProperty("Provider", out _));
        Assert.False(maps.TryGetProperty("BaseUrl", out _));
    }

    /// <summary>
    /// BE-24: a conta por confirmar é barrada no login (403), por isso nenhum serviço precisa de a verificar outra vez.
    /// Sem sessão, criar uma viagem dá 401; com a conta confirmada, o mesmo pedido cria a viagem.
    /// </summary>
    [Fact]
    public async Task I112_104_ContaPorConfirmar_SoOLoginABarra()
    {
        using var app = new AplicacaoReal();
        var porConfirmar = app.CriarUtilizador("Por confirmar", confirmado: false);
        using var cliente = app.CreateClient();

        using var login = await cliente.PostAsJsonAsync(
            "/api/v1/autenticacao/login",
            new { email = porConfirmar.Email, password = AplicacaoReal.PalavraPasse });
        await RespostasDeErro.AssertErroAsync(login, HttpStatusCode.Forbidden, "Conta por confirmar.");

        // Sem o token que o login recusou, a API não deixa criar nada.
        var viagem = new { origem = "Porto", destino = "Braga", partidaEm = DateTime.UtcNow.AddDays(2), lugares = 3 };
        using var semSessao = await cliente.PostAsJsonAsync("/api/v1/viagens", viagem);
        Assert.Equal(HttpStatusCode.Unauthorized, semSessao.StatusCode);

        // Uma conta confirmada cria a viagem sem nenhuma verificação extra.
        using var confirmado = app.ClienteDe(app.CriarUtilizador("Confirmado"));
        using var criada = await confirmado.PostAsJsonAsync("/api/v1/viagens", viagem);
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
    }
}
