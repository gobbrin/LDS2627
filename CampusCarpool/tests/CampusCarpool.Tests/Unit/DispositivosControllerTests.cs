using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class DispositivosControllerTests
{
    private sealed class RepositorioDispositivosFalso : IRepositorioDispositivos
    {
        public Guid? UtilizadorRegistado { get; private set; }

        public string? TokenRegistado { get; private set; }

        public Guid? UtilizadorDesativado { get; private set; }

        public string? TokenDesativado { get; private set; }

        public int Registos { get; private set; }

        public int Remocoes { get; private set; }

        public Task<IReadOnlyList<Dispositivo>> ListarAtivosAsync(Guid utilizadorId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Dispositivo>>(Array.Empty<Dispositivo>());

        public Task RegistarAsync(Guid utilizadorId, string token, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            UtilizadorRegistado = utilizadorId;
            TokenRegistado = token;
            Registos++;

            return Task.CompletedTask;
        }

        public Task DesativarAsync(Guid utilizadorId, string token, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            UtilizadorDesativado = utilizadorId;
            TokenDesativado = token;
            Remocoes++;

            return Task.CompletedTask;
        }

        public Task DesativarTokenInvalidoAsync(string token, CancellationToken ct) => Task.CompletedTask;
    }

    private static DispositivosController CriarController(
        RepositorioDispositivosFalso repositorio,
        Guid? utilizadorId = null,
        string? claimType = null)
    {
        var claims = utilizadorId.HasValue
            ? new[]
            {
                new Claim(
                    claimType ?? ClaimTypes.NameIdentifier,
                    utilizadorId.Value.ToString())
            }
            : Array.Empty<Claim>();

        var controller = new DispositivosController(repositorio)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Teste"))
                }
            }
        };

        return controller;
    }

    [Fact]
    public async Task Registar_ComTokenValido_RegistaEDevolve204()
    {
        var utilizadorId = Guid.NewGuid();
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(repositorio, utilizadorId);

        var resposta = await controller.Registar(
            new DispositivosController.TokenDispositivo(" token-teste "),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resposta);
        Assert.Equal(1, repositorio.Registos);
        Assert.Equal(utilizadorId, repositorio.UtilizadorRegistado);
        Assert.Equal("token-teste", repositorio.TokenRegistado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Registar_ComTokenVazio_Devolve400(string token)
    {
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(repositorio, Guid.NewGuid());

        var resposta = await controller.Registar(
            new DispositivosController.TokenDispositivo(token),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resposta);
        Assert.Equal(0, repositorio.Registos);
    }

    [Fact]
    public async Task Registar_ComTokenDemasiadoLongo_Devolve400()
    {
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(repositorio, Guid.NewGuid());

        var resposta = await controller.Registar(
            new DispositivosController.TokenDispositivo(new string('a', 4097)), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resposta);
        Assert.Equal(0, repositorio.Registos);
    }

    [Fact]
    public async Task Registar_SemIdentificadorValido_Devolve401()
    {
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(repositorio);

        var resposta = await controller.Registar(
            new DispositivosController.TokenDispositivo("token-teste"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(resposta);
        Assert.Equal(0, repositorio.Registos);
    }

    [Fact]
    public async Task Registar_ComClaimSub_UtilizaIdDoUtilizador()
    {
        var utilizadorId = Guid.NewGuid();
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(
            repositorio, utilizadorId, "sub");

        var resposta = await controller.Registar(
            new DispositivosController.TokenDispositivo("token-teste"),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resposta);
        Assert.Equal(utilizadorId, repositorio.UtilizadorRegistado);
    }

    [Fact]
    public async Task Remover_ComTokenValido_DesativaEDevolve204()
    {
        var utilizadorId = Guid.NewGuid();
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(repositorio, utilizadorId);

        var resposta = await controller.Remover(
            new DispositivosController.TokenDispositivo(" token-teste "),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resposta);
        Assert.Equal(1, repositorio.Remocoes);
        Assert.Equal(utilizadorId, repositorio.UtilizadorDesativado);
        Assert.Equal("token-teste", repositorio.TokenDesativado);
    }

    [Fact]
    public async Task Remover_ComTokenVazio_Devolve400()
    {
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(repositorio, Guid.NewGuid());

        var resposta = await controller.Remover(
            new DispositivosController.TokenDispositivo("   "),
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(resposta);
        Assert.Equal(0, repositorio.Remocoes);
    }

    [Fact]
    public async Task Remover_SemIdentificadorValido_Devolve401()
    {
        var repositorio = new RepositorioDispositivosFalso();
        var controller = CriarController(repositorio);

        var resposta = await controller.Remover(
            new DispositivosController.TokenDispositivo("token-teste"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(resposta);
        Assert.Equal(0, repositorio.Remocoes);
    }
}
