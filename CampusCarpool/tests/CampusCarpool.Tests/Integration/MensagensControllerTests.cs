using System.Security.Claims;
using CampusCarpool.Api.Controllers;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Comunicacao;
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
        public Erro? ErroEnvio { get; set; }
        public Erro? ErroConsulta { get; set; }
        public List<MensagemIndividual> Historico { get; set; } = [];

        public Task<Resultado<MensagemIndividual>> EnviarAsync(Guid remetenteId, Guid destinatarioId, string conteudo, CancellationToken ct)
        {
            UltimoRemetente = remetenteId;
            UltimoDestinatario = destinatarioId;
            UltimoConteudo = conteudo;
            if (ErroEnvio is not null) return Task.FromResult<Resultado<MensagemIndividual>>(ErroEnvio);
            return Task.FromResult<Resultado<MensagemIndividual>>(
                new MensagemIndividual(Guid.NewGuid(), null, remetenteId, destinatarioId, conteudo, DateTime.UtcNow));
        }

        public Task<Resultado<List<MensagemIndividual>>> ObterConversaAsync(Guid utilizadorId, Guid outroUtilizadorId, CancellationToken ct)
        {
            UltimoConsultante = utilizadorId;
            UltimoInterlocutor = outroUtilizadorId;
            if (ErroConsulta is not null) return Task.FromResult<Resultado<List<MensagemIndividual>>>(ErroConsulta);
            return Task.FromResult<Resultado<List<MensagemIndividual>>>(Historico);
        }

        public Task<IReadOnlyList<ResumoConversa>> ListarConversasAsync(Guid utilizadorId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ResumoConversa>>([]);
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
        var resultado = await controller.Enviar(new MensagensController.EnviarMensagem(b, "Olá B"), default);
        var criado = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
        Assert.Equal(a, servico.UltimoRemetente);
        Assert.Equal(b, servico.UltimoDestinatario);
        Assert.Equal("Olá B", servico.UltimoConteudo);
        var mensagem = Assert.IsType<MensagemIndividual>(criado.Value);
        Assert.Equal(a, mensagem.RemetenteId);
        Assert.Equal(b, mensagem.DestinatarioId);
    }

    [Fact]
    public async Task I043_02_EnviarSemIdentificadorAutenticado_Retorna401()
    {
        var servico = new ServicoMensagensFalso();
        var resultado = await CriarController(servico, null).Enviar(
            new MensagensController.EnviarMensagem(Guid.NewGuid(), "Olá"), default);
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
        var servico = new ServicoMensagensFalso { ErroEnvio = new Erro(TipoErro.DadosInvalidos, "Conteúdo inválido") };
        var resultado = await CriarController(servico, Guid.NewGuid()).Enviar(
            new MensagensController.EnviarMensagem(Guid.NewGuid(), conteudo), default);
        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task I043_05_DestinatarioInexistente_RejeitadoPeloServico_Retorna400()
    {
        var servico = new ServicoMensagensFalso { ErroEnvio = new Erro(TipoErro.DadosInvalidos, "Destinatário inexistente") };
        var resultado = await CriarController(servico, Guid.NewGuid()).Enviar(
            new MensagensController.EnviarMensagem(Guid.NewGuid(), "Olá"), default);
        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task I043_06_Consulta_UsaIdentidadeAutenticadaERetornaHistorico()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var historico = new List<MensagemIndividual>
        {
            new(Guid.NewGuid(), null, a, b, "Primeira", DateTime.UtcNow),
            new(Guid.NewGuid(), null, b, a, "Resposta", DateTime.UtcNow)
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
        var servico = new ServicoMensagensFalso { ErroConsulta = new Erro(TipoErro.DadosInvalidos, "Conversa inválida") };
        var resultado = await CriarController(servico, Guid.NewGuid()).ObterConversa(Guid.NewGuid(), default);
        Assert.IsType<BadRequestObjectResult>(resultado);
    }
}
