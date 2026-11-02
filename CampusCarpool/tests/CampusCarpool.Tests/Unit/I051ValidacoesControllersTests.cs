using CampusCarpool.Api.Controllers;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Testes unitários das validações executadas antes de aceder às dependências externas.</summary>
public sealed class I051ValidacoesControllersTests
{
    [Theory]
    [InlineData("", "aluno@estg.ipp.pt", "password123456")]
    [InlineData("Aluno", "aluno@gmail.com", "password123456")]
    [InlineData("Aluno", "aluno@estg.ipp.pt", "curta")]
    [InlineData("Aluno", "", "password123456")]
    public async Task F001_RegistoInvalido_Devolve400(string nome, string email, string password)
    {
        var controller = new AutenticacaoController(null!, null!, null!);
        var result = await controller.Registar(new AutenticacaoController.Registo(nome, email, password), default);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("", "senha")]
    [InlineData("aluno@estg.ipp.pt", "")]
    public async Task F001_LoginSemCredenciais_Devolve401(string email, string password)
    {
        var controller = new AutenticacaoController(null!, null!, null!);
        var result = await controller.IniciarSessao(new AutenticacaoController.Login(email, password), default);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task F001_LoginValido_DevolveTokenDeAcesso()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Aluno Teste",
            Email = "aluno@estg.ipp.pt",
            EmailConfirmado = true
        };

        utilizador.PasswordHash = new PasswordHasher<Utilizador>()
            .HashPassword(utilizador, "PasswordSegura123!");

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ChaveDeTesteComPeloMenosTrintaEDoisBytes!",
                ["Jwt:Issuer"] = "CampusCarpool.Tests",
                ["Jwt:Audience"] = "CampusCarpool.Tests"
            })
            .Build();

        var emailFake = new EmailFake();
        var controller = new AutenticacaoController(db, configuracao, emailFake);

        var resultado = await controller.IniciarSessao(
            new AutenticacaoController.Login(
                "aluno@estg.ipp.pt",
                "PasswordSegura123!"),
            CancellationToken.None);

        var resposta = Assert.IsType<OkObjectResult>(resultado);

        Assert.NotNull(resposta.Value);

        var tipoResposta = resposta.Value.GetType();

        var token = tipoResposta
            .GetProperty("accessToken")?
            .GetValue(resposta.Value) as string;

        var expiracao = tipoResposta
            .GetProperty("expiresIn")?
            .GetValue(resposta.Value);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3600, expiracao);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("CampusCarpool.Tests", jwt.Issuer);
        Assert.Contains("CampusCarpool.Tests", jwt.Audiences);
        Assert.Equal(utilizador.Id.ToString(),
            jwt.Claims.First(c => c.Type == "sub").Value);
    }

    [Fact]
    public async Task F001_ConfirmacaoSemToken_Devolve400()
    {
        var controller = new AutenticacaoController(null!, null!, null!);
        Assert.IsType<BadRequestResult>(await controller.ConfirmarConta(new AutenticacaoController.Confirmar(" "), default));
    }

    [Fact]
    public async Task F002_CriarViagemSemIdentidade_Devolve401()
    {
        var controller = new ViagensController(null!, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = await controller.Criar(new ViagensController.CriarViagem("Porto", "Braga", DateTime.UtcNow.AddDays(1), 3), default);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(-1, 20)]
    public async Task F003_PesquisaComPaginacaoInvalida_Devolve400(int pagina, int tamanho)
    {
        var controller = new PesquisaController(null!);
        var result = await controller.Pesquisar(null, null, null, null, pagina, tamanho);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task F001_ConsultaPerfilSemIdentidade_Devolve401()
    {
        var controller = new UtilizadoresController(null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        Assert.IsType<UnauthorizedResult>(await controller.ConsultarPerfil(default));
    }

    [Fact]
    public async Task F001_AtualizacaoPerfilSemIdentidade_Devolve401()
    {
        var controller = new UtilizadoresController(null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = await controller.Atualizar(new UtilizadoresController.AtualizarPerfil("Aluno", null, null), default);
        Assert.IsType<UnauthorizedResult>(result);
    }

    
    [Fact]
    public async Task F001_AtualizacaoPerfilValida_AtualizaDadosDoUtilizador()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Nome Original",
            Email = "aluno@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true,
            Curso = "Curso Original",
            Instituicao = "Instituicao Original"
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        utilizador.Id.ToString())
                },
                "TestAuthentication"));

        var controller = new UtilizadoresController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var resultado = await controller.Atualizar(
            new UtilizadoresController.AtualizarPerfil(
                "  Novo Nome  ",
                "  Engenharia Informática  ",
                "  ESTG  "),
            CancellationToken.None);

        var resposta = Assert.IsType<OkObjectResult>(resultado);
        Assert.NotNull(resposta.Value);

        var utilizadorAtualizado = await db.Utilizadores
            .AsNoTracking()
            .SingleAsync(u => u.Id == utilizador.Id);

        Assert.Equal("Novo Nome", utilizadorAtualizado.Nome);
        Assert.Equal("Engenharia Informática", utilizadorAtualizado.Curso);
        Assert.Equal("ESTG", utilizadorAtualizado.Instituicao);

        // Os restantes dados não devem ser alterados.
        Assert.Equal("aluno@estg.ipp.pt", utilizadorAtualizado.Email);
        Assert.Equal("hash-de-teste", utilizadorAtualizado.PasswordHash);
        Assert.True(utilizadorAtualizado.EmailConfirmado);
    }

    [Fact]
    public async Task F001_RegistoValido_CriaContaPendenteEEnviaConfirmacao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var emailFake = new EmailFake();
        var config = new ConfigurationBuilder().Build();

        var controller = new AutenticacaoController(db, config, emailFake);

        var input = new AutenticacaoController.Registo(
            "Aluno Teste",
            "ALUNO@ESTG.IPP.PT",
            "PasswordSegura123!");

        var result = await controller.Registar(input, CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);

        var utilizador = await db.Utilizadores.SingleAsync();

        Assert.Equal("aluno@estg.ipp.pt", utilizador.Email);
        Assert.Equal("Aluno Teste", utilizador.Nome);
        Assert.False(utilizador.EmailConfirmado);
        Assert.NotEqual("PasswordSegura123!", utilizador.PasswordHash);
        Assert.Single(db.ConfirmacoesConta);
        Assert.Equal(1, emailFake.QuantidadeEnvios);
    }

    [Fact]
    public async Task UT006_RegistoGeraTokenValidoEEnviaEmail()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var emailFake = new EmailFake();
        var config = new ConfigurationBuilder().Build();

        var controller = new AutenticacaoController(db, config, emailFake);

        var inicio = DateTime.UtcNow;

        var input = new AutenticacaoController.Registo(
            "Aluno Teste",
            "aluno@estg.ipp.pt",
            "PasswordSegura123!");

        var resultado = await controller.Registar(input, CancellationToken.None);

        var fim = DateTime.UtcNow;

        Assert.IsType<AcceptedResult>(resultado);

        var utilizador = await db.Utilizadores.SingleAsync();
        var confirmacao = await db.ConfirmacoesConta.SingleAsync();

        // O token original é enviado por email, mas não fica guardado na BD.
        Assert.False(string.IsNullOrWhiteSpace(emailFake.TokenRecebido));
        Assert.Equal(64, emailFake.TokenRecebido!.Length);

        Assert.Equal("aluno@estg.ipp.pt", emailFake.DestinoRecebido);
        Assert.Equal(1, emailFake.QuantidadeEnvios);

        // O token guardado corresponde ao hash SHA-256 do token enviado.
        var hashEsperado = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(emailFake.TokenRecebido)));

        Assert.Equal(hashEsperado, confirmacao.TokenHash);

        // A confirmação pertence ao utilizador criado e ainda pendente.
        Assert.Equal(utilizador.Id, confirmacao.UtilizadorId);
        Assert.False(utilizador.EmailConfirmado);
        Assert.Null(confirmacao.ConsumidoEm);

        // A validade configurada é de 24 horas.
        Assert.InRange(
            confirmacao.ExpiraEm,
            inicio.AddHours(24).AddSeconds(-2),
            fim.AddHours(24).AddSeconds(2));
    }

    
    [Fact]
    public async Task UT007_TokenValidoConfirmaContaEImpedeReutilizacao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var emailFake = new EmailFake();
        var config = new ConfigurationBuilder().Build();

        var controller = new AutenticacaoController(db, config, emailFake);

        // Criar uma conta pendente e obter o token enviado por email.
        var registo = new AutenticacaoController.Registo(
            "Aluno Teste",
            "aluno@estg.ipp.pt",
            "PasswordSegura123!");

        var resultadoRegisto = await controller.Registar(registo, CancellationToken.None);

        Assert.IsType<AcceptedResult>(resultadoRegisto);

        var token = emailFake.TokenRecebido;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var utilizador = await db.Utilizadores.SingleAsync();
        var confirmacao = await db.ConfirmacoesConta.SingleAsync();

        Assert.False(utilizador.EmailConfirmado);
        Assert.Null(confirmacao.ConsumidoEm);

        // Primeira utilização: o token deve confirmar a conta.
        var resultadoConfirmacao = await controller.ConfirmarConta(
            new AutenticacaoController.Confirmar(token!),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resultadoConfirmacao);

        await db.Entry(utilizador).ReloadAsync();
        await db.Entry(confirmacao).ReloadAsync();

        Assert.True(utilizador.EmailConfirmado);
        Assert.NotNull(confirmacao.ConsumidoEm);

        // Segunda utilização: o token já foi consumido e deve ser rejeitado.
        var segundaConfirmacao = await controller.ConfirmarConta(
            new AutenticacaoController.Confirmar(token!),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(segundaConfirmacao);

        // A conta continua confirmada e o token continua consumido.
        await db.Entry(utilizador).ReloadAsync();
        await db.Entry(confirmacao).ReloadAsync();

        Assert.True(utilizador.EmailConfirmado);
        Assert.NotNull(confirmacao.ConsumidoEm);
    }

    [Fact]
    public async Task UT008_TokenVazioERejeitado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var emailFake = new EmailFake();
        var config = new ConfigurationBuilder().Build();
        var controller = new AutenticacaoController(db, config, emailFake);

        var registo = await controller.Registar(
            new AutenticacaoController.Registo(
                "Aluno Teste",
                "aluno@estg.ipp.pt",
                "PasswordSegura123!"),
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(registo);

        var utilizador = await db.Utilizadores.SingleAsync();
        var confirmacao = await db.ConfirmacoesConta.SingleAsync();

        var resultado = await controller.ConfirmarConta(new AutenticacaoController.Confirmar(" "), CancellationToken.None);

        Assert.IsType<BadRequestResult>(resultado);
        Assert.False(utilizador.EmailConfirmado);
        Assert.Null(confirmacao.ConsumidoEm);
    }

    [Fact]
    public async Task UT008_TokenDesconhecidoERejeitado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var emailFake = new EmailFake();
        var config = new ConfigurationBuilder().Build();
        var controller = new AutenticacaoController(db, config, emailFake);

        var registo = await controller.Registar(
            new AutenticacaoController.Registo(
                "Aluno Teste",
                "aluno@estg.ipp.pt",
                "PasswordSegura123!"),
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(registo);

        var utilizador = await db.Utilizadores.SingleAsync();
        var confirmacao = await db.ConfirmacoesConta.SingleAsync();

        var resultado = await controller.ConfirmarConta(
            new AutenticacaoController.Confirmar("TOKEN_DESCONHECIDO"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.False(utilizador.EmailConfirmado);
        Assert.Null(confirmacao.ConsumidoEm);
    }

    [Fact]
    public async Task UT008_TokenExpiradoERejeitado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var emailFake = new EmailFake();
        var config = new ConfigurationBuilder().Build();
        var controller = new AutenticacaoController(db, config, emailFake);

        var registo = await controller.Registar(
            new AutenticacaoController.Registo(
                "Aluno Teste",
                "aluno@estg.ipp.pt",
                "PasswordSegura123!"),
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(registo);

        var utilizador = await db.Utilizadores.SingleAsync();
        var confirmacao = await db.ConfirmacoesConta.SingleAsync();

        confirmacao.ExpiraEm = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();

        var resultado = await controller.ConfirmarConta(
            new AutenticacaoController.Confirmar(emailFake.TokenRecebido!),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.False(utilizador.EmailConfirmado);
        Assert.Null(confirmacao.ConsumidoEm);
    }

    
    [Fact]
    public async Task F002_IndisponibilizarViagemValida_AlteraEstado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true
        };

        var viagem = new Viagem
        {
            CondutorId = utilizador.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = DateTime.UtcNow.AddDays(2),
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3,
            Estado = "Ativa"
        };

        db.Utilizadores.Add(utilizador);
        db.Viagens.Add(viagem);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier,
                        utilizador.Id.ToString())
                },
                "TestAuthentication"));

        var controller = new ViagensController(db, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var resultado = await controller.Indisponibilizar(viagem.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);

        var viagemAtualizada = await db.Viagens
            .AsNoTracking()
            .SingleAsync(v => v.Id == viagem.Id);

        Assert.Equal("Indisponibilizada", viagemAtualizada.Estado);
    }

    
    [Fact]
    public async Task F002_EditarViagemValida_AtualizaDataELugares()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true
        };

        var partidaOriginal = DateTime.UtcNow.AddDays(2);

        var viagem = new Viagem
        {
            CondutorId = utilizador.Id,
            Origem = "Porto",
            Destino = "Braga",
            PartidaEm = partidaOriginal,
            LugaresOferecidos = 3,
            LugaresDisponiveis = 3,
            Estado = "Ativa"
        };

        db.Utilizadores.Add(utilizador);
        db.Viagens.Add(viagem);
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier,
                        utilizador.Id.ToString())
                },
                "TestAuthentication"));

        var controller = new ViagensController(db, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var novaPartida = DateTime.UtcNow.AddDays(5);

        var resultado = await controller.Editar(
            viagem.Id,
            new ViagensController.EditarViagem(novaPartida, 4),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);

        var viagemAtualizada = await db.Viagens
            .AsNoTracking()
            .SingleAsync(v => v.Id == viagem.Id);

        Assert.Equal(novaPartida, viagemAtualizada.PartidaEm);
        Assert.Equal(4, viagemAtualizada.LugaresOferecidos);
        Assert.Equal(4, viagemAtualizada.LugaresDisponiveis);
        Assert.Equal("Ativa", viagemAtualizada.Estado);
    }

    [Fact]
    public async Task F002_CriarViagemValida_CriaViagemComDadosDaRota()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maps:ApiKey"] = "chave-ficticia-de-teste"
            })
            .Build();

        var handler = new FakeMapasHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                    "features": [
                        {
                        "geometry": {
                            "coordinates": [-8.61, 41.15]
                        },
                        "properties": {
                            "summary": {
                            "distance": 12000,
                            "duration": 1800
                            }
                        }
                        }
                    ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            }
        );

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        var mapas = new MapasService(httpClient, configuracao);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier,
                        utilizador.Id.ToString())
                },
                "TestAuthentication"));

        var controller = new ViagensController(db, mapas)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var partida = DateTime.UtcNow.AddDays(3);

        var resultado = await controller.Criar(
            new ViagensController.CriarViagem(
                "Porto",
                "Braga",
                partida,
                3),
            CancellationToken.None);

        var resposta = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(nameof(ViagensController.Consultar), resposta.ActionName);

        var viagem = await db.Viagens.SingleAsync();

        Assert.Equal(utilizador.Id, viagem.CondutorId);
        Assert.Equal("Porto", viagem.Origem);
        Assert.Equal("Braga", viagem.Destino);
        Assert.Equal(partida, viagem.PartidaEm);
        Assert.Equal(3, viagem.LugaresOferecidos);
        Assert.Equal(3, viagem.LugaresDisponiveis);
        Assert.Equal(12m, viagem.DistanciaKm);
        Assert.Equal(30, viagem.DuracaoMinutos);
        Assert.Equal("Ativa", viagem.Estado);
    }
 
    [Fact]
    public async Task F003_PesquisaValida_FiltraOrdenaEPaginaResultados()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var partidaMaisTardia = DateTime.UtcNow.AddDays(4);
        var partidaMaisProxima = DateTime.UtcNow.AddDays(2);

        db.Viagens.AddRange(
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = partidaMaisTardia,
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            },
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = partidaMaisProxima,
                LugaresOferecidos = 2,
                LugaresDisponiveis = 2,
                Estado = "Ativa"
            },
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Lisboa",
                Destino = "Braga",
                PartidaEm = DateTime.UtcNow.AddDays(1),
                LugaresOferecidos = 4,
                LugaresDisponiveis = 4,
                Estado = "Ativa"
            }
        );

        await db.SaveChangesAsync();

        var controller = new PesquisaController(db);

        var resultado = await controller.Pesquisar(
            "Porto",
            "Braga",
            null,
            null,
            1,
            1,
            CancellationToken.None);

        var resposta = Assert.IsType<OkObjectResult>(resultado);
        Assert.NotNull(resposta.Value);

        using var documento = JsonDocument.Parse(
            JsonSerializer.Serialize(resposta.Value));

        var dados = documento.RootElement;

        Assert.Equal(2, dados.GetProperty("total").GetInt32());
        Assert.Equal(1, dados.GetProperty("pagina").GetInt32());
        Assert.Equal(1, dados.GetProperty("tamanho").GetInt32());

        var viagens = dados.GetProperty("viagens");

        Assert.Single(viagens.EnumerateArray());

        var primeiraViagem = viagens[0];

        Assert.Equal(
            "Porto",
            primeiraViagem.GetProperty("Origem").GetString());

        Assert.Equal(
            "Braga",
            primeiraViagem.GetProperty("Destino").GetString());

        var partidaDevolvida = primeiraViagem
            .GetProperty("PartidaEm")
            .GetDateTime();

        Assert.Equal(partidaMaisProxima, partidaDevolvida);
    }

    
    [Fact]
    public async Task F003_PesquisaSemFiltros_DevolveApenasViagensElegiveis()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var agora = DateTime.UtcNow;

        db.Viagens.AddRange(
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = agora.AddDays(2),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            },
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Lisboa",
                Destino = "Coimbra",
                PartidaEm = agora.AddDays(3),
                LugaresOferecidos = 2,
                LugaresDisponiveis = 0,
                Estado = "Ativa"
            },
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Aveiro",
                Destino = "Porto",
                PartidaEm = agora.AddDays(4),
                LugaresOferecidos = 2,
                LugaresDisponiveis = 2,
                Estado = "Indisponibilizada"
            },
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Braga",
                Destino = "Porto",
                PartidaEm = agora.AddDays(-1),
                LugaresOferecidos = 2,
                LugaresDisponiveis = 2,
                Estado = "Ativa"
            }
        );

        await db.SaveChangesAsync();

        var controller = new PesquisaController(db);

        var resultado = await controller.Pesquisar(null, null, null, null, 1, 20, CancellationToken.None);

        var resposta = Assert.IsType<OkObjectResult>(resultado);

        using var documento = JsonDocument.Parse(JsonSerializer.Serialize(resposta.Value));

        var dados = documento.RootElement;
        var viagens = dados.GetProperty("viagens");

        Assert.Equal(1, dados.GetProperty("total").GetInt32());
        Assert.Single(viagens.EnumerateArray());
        Assert.Equal(
            "Porto",
            viagens[0].GetProperty("Origem").GetString());
        Assert.Equal(
            "Braga",
            viagens[0].GetProperty("Destino").GetString());
    }

    
    [Fact]
    public async Task F003_FiltroDataEHora_DevolveApenasPartidasCorrespondentes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var dataPartida = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        db.Viagens.AddRange(
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = dataPartida.ToDateTime(new TimeOnly(10, 0)),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            },
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = dataPartida.ToDateTime(new TimeOnly(15, 0)),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            },
            new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = "Porto",
                Destino = "Braga",
                PartidaEm = dataPartida.AddDays(1)
                    .ToDateTime(new TimeOnly(16, 0)),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            }
        );

        await db.SaveChangesAsync();

        var controller = new PesquisaController(db);

        var resultado = await controller.Pesquisar(
            null, null, dataPartida, new TimeOnly(12, 0),
            1, 20, CancellationToken.None);

        var resposta = Assert.IsType<OkObjectResult>(resultado);

        using var documento = JsonDocument.Parse(JsonSerializer.Serialize(resposta.Value));

        var dados = documento.RootElement;
        var viagens = dados.GetProperty("viagens");

        Assert.Equal(1, dados.GetProperty("total").GetInt32());
        Assert.Single(viagens.EnumerateArray());
        Assert.Equal(
            new TimeOnly(15, 0),
            TimeOnly.FromDateTime(
                viagens[0].GetProperty("PartidaEm").GetDateTime()));
    }

    [Fact]
    public async Task F003_PaginacaoValida_DevolveSegundaPaginaSemDuplicacoes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var utilizador = new Utilizador
        {
            Nome = "Condutor Teste",
            Email = "condutor@estg.ipp.pt",
            PasswordHash = "hash-de-teste",
            EmailConfirmado = true
        };

        db.Utilizadores.Add(utilizador);
        await db.SaveChangesAsync();

        var agora = DateTime.UtcNow;

        for (var i = 1; i <= 5; i++)
        {
            db.Viagens.Add(new Viagem
            {
                CondutorId = utilizador.Id,
                Origem = $"Origem {i}",
                Destino = "Braga",
                PartidaEm = agora.AddDays(i),
                LugaresOferecidos = 3,
                LugaresDisponiveis = 3,
                Estado = "Ativa"
            });
        }

        await db.SaveChangesAsync();

        var controller = new PesquisaController(db);

        var resultadoPagina1 = await controller.Pesquisar(null, null, null, null, 1, 2, CancellationToken.None);

        var resultadoPagina2 = await controller.Pesquisar(null, null, null, null, 2, 2, CancellationToken.None);

        var resposta1 = Assert.IsType<OkObjectResult>(resultadoPagina1);
        var resposta2 = Assert.IsType<OkObjectResult>(resultadoPagina2);

        using var documento1 = JsonDocument.Parse(JsonSerializer.Serialize(resposta1.Value));
        using var documento2 = JsonDocument.Parse(JsonSerializer.Serialize(resposta2.Value));

        var viagens1 = documento1.RootElement.GetProperty("viagens");
        var viagens2 = documento2.RootElement.GetProperty("viagens");

        Assert.Equal(5, documento2.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(1, documento1.RootElement.GetProperty("pagina").GetInt32());
        Assert.Equal(2, documento2.RootElement.GetProperty("pagina").GetInt32());

        Assert.Equal(2, viagens1.GetArrayLength());
        Assert.Equal(2, viagens2.GetArrayLength());

        Assert.Equal("Origem 1", viagens1[0].GetProperty("Origem").GetString());
        Assert.Equal("Origem 2", viagens1[1].GetProperty("Origem").GetString());
        Assert.Equal("Origem 3", viagens2[0].GetProperty("Origem").GetString());
        Assert.Equal("Origem 4", viagens2[1].GetProperty("Origem").GetString());
        
        var idsPagina1 = viagens1.EnumerateArray()
            .Select(v => v.GetProperty("Id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var idsPagina2 = viagens2.EnumerateArray()
            .Select(v => v.GetProperty("Id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Empty(idsPagina1.Intersect(idsPagina2));
    }

    // Classes auxiliares.
    private sealed class EmailFake : IServicoEmail
    {
        public int QuantidadeEnvios { get; private set; }
        public string? DestinoRecebido { get; private set; }
        public string? TokenRecebido { get; private set; }

        public Task EnviarConfirmacaoAsync(string destino, string token, CancellationToken ct)
        {
            QuantidadeEnvios++;
            DestinoRecebido = destino;
            TokenRecebido = token;

            return Task.CompletedTask;
        }
    }

    
    private sealed class FakeMapasHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }

}
