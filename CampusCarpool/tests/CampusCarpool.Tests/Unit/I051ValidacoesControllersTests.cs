using CampusCarpool.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public async Task F008_ConsultaPerfilSemIdentidade_Devolve401()
    {
        var controller = new UtilizadoresController(null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        Assert.IsType<UnauthorizedResult>(await controller.ConsultarPerfil(default));
    }

    [Fact]
    public async Task F008_AtualizacaoPerfilSemIdentidade_Devolve401()
    {
        var controller = new UtilizadoresController(null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = await controller.Atualizar(new UtilizadoresController.AtualizarPerfil("Aluno", null, null), default);
        Assert.IsType<UnauthorizedResult>(result);
    }
}
