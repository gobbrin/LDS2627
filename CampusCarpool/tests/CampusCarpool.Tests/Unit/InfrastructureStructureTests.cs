using Xunit;
namespace CampusCarpool.Tests.Unit;

/// <summary>Teste de estrutura: o tipo do <c>DbContext</c> existe. Sem valor de verificação (TE-09).</summary>
public sealed class InfrastructureStructureTests
{
    [Fact]
    public void DbContextTypeExists() => Assert.NotNull(typeof(CampusCarpool.Infrastructure.Persistence.CampusCarpoolDbContext));
}
