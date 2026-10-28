using Xunit;
namespace CampusCarpool.Tests.Unit;

public sealed class InfrastructureStructureTests
{
    [Fact]
    public void DbContextTypeExists() => Assert.NotNull(typeof(CampusCarpool.Infrastructure.Persistence.CampusCarpoolDbContext));
}
