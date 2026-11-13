using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Identidade;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using CampusCarpool.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>Registo com outro pedido em simultâneo ou com o Serviço de Email em baixo (API-07, DC-43, TE-07 (3) e (6)).</summary>
/// <remarks>
/// O interceptor grava o registo concorrente por outro contexto mesmo antes do <c>SaveChanges</c>, depois de o serviço já
/// ter verificado que o email estava livre: é a corrida real, sem depender do tempo.
/// </remarks>
public sealed class RegistoResilienteTests : IDisposable
{
    private const string Endereco = "corrida@estg.ipp.pt";

    private readonly SqliteConnection _ligacao = new("Data Source=:memory:");
    private readonly DbContextOptions<CampusCarpoolDbContext> _opcoes;

    public RegistoResilienteTests()
    {
        _ligacao.Open();
        _opcoes = new DbContextOptionsBuilder<CampusCarpoolDbContext>().UseBaseDeTeste(_ligacao).Options;

        using var db = new CampusCarpoolDbContext(_opcoes);
        db.Database.CriarEsquemaDeTeste();
    }

    public void Dispose() => _ligacao.Dispose();

    /// <summary>Executa uma gravação por outro contexto antes da primeira gravação do contexto observado.</summary>
    private sealed class OutroPedido(Action acao) : SaveChangesInterceptor
    {
        private bool _feito;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_feito)
            {
                _feito = true;
                acao();
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class EmailComFalha(Exception falha) : IServicoEmail
    {
        public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct) => throw falha;
    }

    private sealed class EmailComInterruptor : IServicoEmail
    {
        public bool EmBaixo { get; set; }
        public string? UltimoToken { get; private set; }

        public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct)
        {
            if (EmBaixo) throw new HttpRequestException("Brevo em baixo.");
            UltimoToken = token;
            return Task.CompletedTask;
        }
    }

    private sealed class EmailSemFalha : IServicoEmail
    {
        public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class HashEspiao : IHashPalavraPasse
    {
        public int Calculos { get; private set; }

        public string Gerar(Utilizador utilizador, string palavraPasse)
        {
            Calculos++;
            return "hash";
        }

        public bool Verificar(Utilizador utilizador, string palavraPasse)
        {
            Calculos++;
            return false;
        }
    }

    private CampusCarpoolDbContext Contexto(Action? outroPedido = null)
    {
        var construtor = new DbContextOptionsBuilder<CampusCarpoolDbContext>(_opcoes);
        if (outroPedido is not null) construtor.AddInterceptors(new OutroPedido(outroPedido));
        return new CampusCarpoolDbContext(construtor.Options);
    }

    private static AutenticacaoController Controller(CampusCarpoolDbContext db, IServicoEmail email) =>
        ServicosDeTeste.CriarAutenticacaoController(db, new ConfigurationBuilder().Build(), email);

    private void GravarRevogado(string jti, DateTime expiraEm)
    {
        using var outro = Contexto();
        outro.TokensRevogados.Add(new TokenRevogado { Jti = jti, ExpiraEm = expiraEm });
        outro.SaveChanges();
    }

    private static AutenticacaoService ServicoDe(CampusCarpoolDbContext db) =>
        ServicosDeTeste.CriarAutenticacaoService(db, new ConfigurationBuilder().Build(), new EmailSemFalha());

    private void GravarUtilizador(string email)
    {
        using var outro = Contexto();
        outro.Utilizadores.Add(new Utilizador { Nome = "Outro", Email = email });
        outro.SaveChanges();
    }

    [Fact]
    public async Task I110_124_DoisRegistosSimultaneos_OSegundoRecebe409()
    {
        await using var db = Contexto(() => GravarUtilizador(Endereco));

        var resposta = await Controller(db, new EmailSemFalha()).Registar(
            new("Aluno", Endereco, "abcdefg1"), CancellationToken.None);

        var conflito = Assert.IsType<ConflictObjectResult>(resposta);
        Assert.Equal("Email já registado.", Assert.IsType<CorpoErro>(conflito.Value).Erro);
    }

    [Fact]
    public async Task I110_125_DoisRegistosDoMesmoDispositivo_NaoFalha()
    {
        Guid utilizadorId;

        await using (var preparar = Contexto())
        {
            var utilizador = new Utilizador { Nome = "Aluno", Email = "aluno@estg.ipp.pt" };
            preparar.Utilizadores.Add(utilizador);
            await preparar.SaveChangesAsync();
            utilizadorId = utilizador.Id;
        }

        await using var db = Contexto(() =>
        {
            using var outro = Contexto();
            var utilizador = new Utilizador { Nome = "Outro", Email = Endereco };
            outro.Utilizadores.Add(utilizador);
            outro.Dispositivos.Add(new Dispositivo { UtilizadorId = utilizador.Id, Token = "token-fcm", Ativo = false });
            outro.SaveChanges();
        });

        await new RepositorioDispositivo(db).RegistarAsync(utilizadorId, "token-fcm", CancellationToken.None);

        await using var verificar = Contexto();
        var dispositivo = await verificar.Dispositivos.SingleAsync();
        Assert.Equal(utilizadorId, dispositivo.UtilizadorId);
        Assert.True(dispositivo.Ativo);
    }

    public static TheoryData<string> FalhasDoEmail => ["recusa", "tempo", "configuracao"];

    [Theory]
    [MemberData(nameof(FalhasDoEmail))]
    public async Task I110_126_FalhaNoEnvioDoEmail_Devolve503EAContaFica(string caso)
    {
        Exception falha = caso switch
        {
            "recusa" => new HttpRequestException("Brevo recusou o envio. HTTP 401."),
            "tempo" => new TaskCanceledException("Tempo esgotado."),
            _ => new InvalidOperationException("Brevo por configurar.")
        };

        await using var db = Contexto();

        var resposta = await Controller(db, new EmailComFalha(falha)).Registar(
            new("Aluno", Endereco, "abcdefg1"), CancellationToken.None);

        Assert.Equal(503, Assert.IsType<ObjectResult>(resposta).StatusCode);
        Assert.True(await db.Utilizadores.AnyAsync(u => u.Email == Endereco));
    }

    [Fact]
    public async Task I110_144_ReenvioComEmailEmBaixo_MantemALigacaoAnterior()
    {
        await using var db = Contexto();
        var email = new EmailComInterruptor();
        var controller = Controller(db, email);

        await controller.Registar(new("Aluno", Endereco, "abcdefg1"), CancellationToken.None);
        var ligacao = email.UltimoToken!;

        foreach (var confirmacao in await db.ConfirmacoesConta.ToListAsync())
            confirmacao.ExpiraEm -= TimeSpan.FromMinutes(2); // Passa o limite de um reenvio por minuto.
        await db.SaveChangesAsync();

        email.EmBaixo = true;
        Assert.IsType<AcceptedResult>(await controller.ReenviarConfirmacaoEmail(new(Endereco), CancellationToken.None));

        Assert.IsType<NoContentResult>(await controller.ConfirmarConta(new(ligacao), CancellationToken.None));
    }

    [Fact]
    public async Task I110_127_LoginDeEmailInexistente_TambemCalculaUmHash()
    {
        await using var db = Contexto();
        var hash = new HashEspiao();
        var servico = new AutenticacaoService(
            new RepositorioUtilizadores(db),
            new RepositorioConfirmacoesConta(db),
            new RepositorioTokensRevogados(db),
            new RepositorioDispositivo(db),
            new UnidadeDeTrabalhoEfCore(db),
            new EmailSemFalha(),
            hash,
            new GeradorTokensJwt(new ConfigurationBuilder().Build()),
            ServicosDeTeste.CriarValidadorEmail(),
            NullLogger<AutenticacaoService>.Instance);

        var resultado = await servico.IniciarSessaoAsync("ninguem@estg.ipp.pt", "abcdefg1", CancellationToken.None);

        Assert.NotNull(resultado.Erro);
        Assert.Equal(1, hash.Calculos);
    }

    [Fact]
    public async Task I110_147_DoisLogoutsSimultaneos_ComOMesmoToken_NaoFalham()
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(30);
        await using var db = Contexto(() => GravarRevogado("jti-repetido", expiraEm));

        await ServicoDe(db).TerminarSessaoAsync("jti-repetido", expiraEm, null, null, CancellationToken.None);

        await using var leitura = Contexto();
        Assert.Equal(1, await leitura.TokensRevogados.CountAsync(t => t.Jti == "jti-repetido"));
    }

    [Fact]
    public async Task I110_148_Logout_SeABdRecusarSemHaverRevogacao_NaoFingeQueTerminou()
    {
        await using var db = Contexto(() => throw new DbUpdateException("BD em baixo."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ServicoDe(db).TerminarSessaoAsync("jti-novo", DateTime.UtcNow.AddMinutes(30), null, null, CancellationToken.None));
    }

    [Fact]
    public async Task I110_149_Limpeza_GuardaOsTokensAindaAceitesPelaMargemDoRelogio()
    {
        GravarRevogado("dentro-da-margem", DateTime.UtcNow.AddSeconds(-10));
        GravarRevogado("fora-da-margem", DateTime.UtcNow.AddMinutes(-5));

        await using var db = Contexto();
        await ServicoDe(db).TerminarSessaoAsync("outro", null, null, null, CancellationToken.None);

        await using var leitura = Contexto();
        Assert.True(await leitura.TokensRevogados.AnyAsync(t => t.Jti == "dentro-da-margem"));
        Assert.False(await leitura.TokensRevogados.AnyAsync(t => t.Jti == "fora-da-margem"));
    }
}
