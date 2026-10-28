using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CampusCarpool.Tests.Integration;

/// <summary>
/// Testes da fronteira controller-serviço. Não demonstram persistência nem notificações reais.
/// </summary>
public sealed class MensagensControllerTests
{
    private sealed class ServicoMensagensFalso : IMensagemService
    {
        public Guid? UltimoRemetente { get; private set; }
        public Guid? UltimoDestinatario { get; private set; }
        public string? UltimoConteudo { get; private set; }
        public Guid? UltimoConsultante { get; private set; }
        public Guid? UltimoInterlocutor { get; private set; }
        public Exception? ErroEnvio { get; set; }
        public Exception? ErroConsulta { get; set; }
        public IReadOnlyList<Mensagem> Historico { get; set; } = Array.Empty<Mensagem>();

        public Task<Mensagem> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct)
        {
            UltimoRemetente = remetenteId;
            UltimoDestinatario = destinatarioId;
            UltimoConteudo = conteudo;
            if (ErroEnvio is not null) throw ErroEnvio;
            return Task.FromResult(new Mensagem
            {
                RemetenteId = remetenteId,
                DestinatarioId = destinatarioId,
                Conteudo = conteudo
            });
        }

        public Task<IReadOnlyList<Mensagem>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct)
        {
            UltimoConsultante = utilizadorId;
            UltimoInterlocutor = outroUtilizadorId;
            if (ErroConsulta is not null) throw ErroConsulta;
            return Task.FromResult(Historico);
        }
    }

    private static MensagensController CriarController(ServicoMensagensFalso servico, Guid? utilizadorId)
    {
        var claims = utilizadorId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, utilizadorId.Value.ToString()) }
            : Array.Empty<Claim>();
        return new MensagensController(servico)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, utilizadorId.HasValue ? "Teste" : null))
                }
            }
        };
    }

    [Fact]
    public async Task I043_01_EnviarMensagemValida_UsaRemetenteDoJwtERetorna201()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var servico = new ServicoMensagensFalso();
        var controller = CriarController(servico, a);
        var resultado = await controller.Enviar(new EnviarMensagemRequest(b, "Olá B"), default);
        var criado = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
        Assert.Equal(a, servico.UltimoRemetente);
        Assert.Equal(b, servico.UltimoDestinatario);
        Assert.Equal("Olá B", servico.UltimoConteudo);
        var mensagem = Assert.IsType<Mensagem>(criado.Value);
        Assert.Equal(a, mensagem.RemetenteId);
        Assert.Equal(b, mensagem.DestinatarioId);
    }

    [Fact]
    public async Task I043_02_EnviarSemIdentificadorAutenticado_Retorna401()
    {
        var servico = new ServicoMensagensFalso();
        var resultado = await CriarController(servico, null).Enviar(
            new EnviarMensagemRequest(Guid.NewGuid(), "Olá"), default);
        Assert.IsType<UnauthorizedResult>(resultado);
        Assert.Null(servico.UltimoRemetente);
    }

    [Fact]
    public async Task I043_03_ConsultarSemIdentificadorAutenticado_Retorna401()
    {
        var servico = new ServicoMensagensFalso();
        var resultado = await CriarController(servico, null).ObterConversa(Guid.NewGuid(), default);
        Assert.IsType<UnauthorizedResult>(resultado);
        Assert.Null(servico.UltimoConsultante);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task I043_04_ConteudoInvalido_RejeitadoPeloServico_Retorna400(string conteudo)
    {
        var servico = new ServicoMensagensFalso { ErroEnvio = new ArgumentException("Conteúdo inválido") };
        var resultado = await CriarController(servico, Guid.NewGuid()).Enviar(
            new EnviarMensagemRequest(Guid.NewGuid(), conteudo), default);
        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task I043_05_DestinatarioInexistente_RejeitadoPeloServico_Retorna400()
    {
        var servico = new ServicoMensagensFalso { ErroEnvio = new InvalidOperationException("Destinatário inexistente") };
        var resultado = await CriarController(servico, Guid.NewGuid()).Enviar(
            new EnviarMensagemRequest(Guid.NewGuid(), "Olá"), default);
        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task I043_06_Consulta_UsaIdentidadeAutenticadaERetornaHistorico()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var historico = new[]
        {
            new Mensagem { RemetenteId = a, DestinatarioId = b, Conteudo = "Primeira" },
            new Mensagem { RemetenteId = b, DestinatarioId = a, Conteudo = "Resposta" }
        };
        var servico = new ServicoMensagensFalso { Historico = historico };
        var resultado = await CriarController(servico, a).ObterConversa(b, default);
        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(a, servico.UltimoConsultante);
        Assert.Equal(b, servico.UltimoInterlocutor);
        Assert.Same(historico, ok.Value);
    }

    [Fact]
    public async Task I043_07_ConsultaInvalida_RejeitadaPeloServico_Retorna400()
    {
        var servico = new ServicoMensagensFalso { ErroConsulta = new ArgumentException("Conversa inválida") };
        var resultado = await CriarController(servico, Guid.NewGuid()).ObterConversa(Guid.NewGuid(), default);
        Assert.IsType<BadRequestObjectResult>(resultado);
    }
}
