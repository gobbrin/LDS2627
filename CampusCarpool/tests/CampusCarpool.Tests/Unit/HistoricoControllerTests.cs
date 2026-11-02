
using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusCarpool.Tests.Unit;

public sealed class HistoricoControllerTests
{
    private sealed class HistoricoServiceFalso : IHistoricoService
    {
        public Guid? UltimoUtilizadorConsultado { get; private set; }

        public Task<HistoricoResultado> ConsultarAsync(
            Guid utilizadorId,
            CancellationToken ct)
        {
            UltimoUtilizadorConsultado = utilizadorId;

            var resultado = new HistoricoResultado(
                Array.Empty<ViagemHistorico>(),
                Array.Empty<ViagemHistorico>(),
                Array.Empty<PedidoHistorico>(),
                Array.Empty<PedidoHistorico>(),
                Array.Empty<NotificacaoHistorico>(),
                Array.Empty<AvisoHistorico>());

            return Task.FromResult(resultado);
        }
    }

    [Fact]
    public async Task Consultar_UtilizaIdDoUtilizadorAutenticado()
    {
        var utilizadorId = Guid.NewGuid();
        var service = new HistoricoServiceFalso();

        var controller = new HistoricoController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[]
                            {
                                new Claim(
                                    ClaimTypes.NameIdentifier,
                                    utilizadorId.ToString())
                            },
                            "Teste"))
                }
            }
        };

        var resposta = await controller.Consultar(
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(resposta);
        Assert.Equal(
            utilizadorId,
            service.UltimoUtilizadorConsultado);
    }

    [Fact]
    public async Task Consultar_SemIdentificadorValido_Devolve401()
    {
        var service = new HistoricoServiceFalso();

        var controller = new HistoricoController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var resposta = await controller.Consultar(
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(resposta);
        Assert.Null(service.UltimoUtilizadorConsultado);
    }
}
