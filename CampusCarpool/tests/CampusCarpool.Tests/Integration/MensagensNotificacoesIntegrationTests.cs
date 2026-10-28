using CampusCarpool.Application.Services;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CampusCarpool.Tests.Integration;

//Testes de integração das notificações geradas por mensagens individuais.
public sealed class MensagensNotificacoesIntegrationTests {
    [Fact]
    public async Task EnviarMensagem_CriaNotificacaoNovaMensagemParaDestinatario() {
        //Prepara uma base de dados SQLite isolada para o teste.
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CampusCarpoolDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new CampusCarpoolDbContext(options);
        await db.Database.EnsureCreatedAsync();

        //Cria os utilizadores que vão participar na conversa.
        var remetente = new Utilizador {
            Nome = "Remetente de teste",
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            EmailConfirmado = true
        };

        var destinatario = new Utilizador {
            Nome = "Destinatário de teste",
            Email = $"{Guid.NewGuid():N}@estg.ipp.pt",
            EmailConfirmado = true
        };

        db.Utilizadores.AddRange(remetente, destinatario);
        await db.SaveChangesAsync();

        //Configura os serviços reais e os respetivos repositórios.
        var repositorioMensagens = new RepositorioMensagens(db);
        var repositorioUtilizadores = new RepositorioUtilizadores(db);
        var repositorioNotificacoes = new RepositorioNotificacoes(db);
        var notificacaoService = new NotificacaoService(repositorioNotificacoes);

        var mensagemService = new MensagemService(repositorioMensagens, repositorioUtilizadores, notificacaoService);

        //Envia uma mensagem individual.
        const string conteudo = "Olá! Esta é uma mensagem de teste.";

        var mensagem = await mensagemService.EnviarAsync(remetente.Id, destinatario.Id, conteudo, CancellationToken.None);

        //Confirma que a mensagem foi guardada corretamente.
        Assert.Equal(remetente.Id, mensagem.RemetenteId);
        Assert.Equal(destinatario.Id, mensagem.DestinatarioId);
        Assert.Equal(conteudo, mensagem.Conteudo);

        var mensagemPersistida = await db.Mensagens
            .AsNoTracking()
            .SingleAsync(m => m.Id == mensagem.Id);

        Assert.Equal(conteudo, mensagemPersistida.Conteudo);

        //Confirma que foi criada uma notificação para o destinatário.
        var notificacao = await db.Notificacoes
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(destinatario.Id, notificacao.UtilizadorId);
        Assert.Equal(TipoNotificacao.NovaMensagem, notificacao.Tipo);
        Assert.Contains(remetente.Nome, notificacao.Conteudo);

        //O remetente não deve receber a notificação da própria mensagem.
        Assert.DoesNotContain(await db.Notificacoes.ToListAsync(), n => n.UtilizadorId == remetente.Id);
    }
    
}
