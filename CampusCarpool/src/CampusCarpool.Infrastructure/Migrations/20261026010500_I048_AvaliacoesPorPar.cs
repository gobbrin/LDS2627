using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CampusCarpool.Infrastructure.Persistence;
#nullable disable
namespace CampusCarpool.Infrastructure.Migrations;
[DbContext(typeof(CampusCarpoolDbContext))]
[Migration("20261026010500_I048_AvaliacoesPorPar")]
public partial class I048_AvaliacoesPorPar : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Avaliacoes_ViagemId_AvaliadorId_AvaliadoId", table: "Avaliacoes");
        migrationBuilder.CreateIndex(name: "IX_Avaliacoes_AvaliadorId_AvaliadoId", table: "Avaliacoes", columns: new[] { "AvaliadorId", "AvaliadoId" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Avaliacoes_AvaliadorId_AvaliadoId", table: "Avaliacoes");
        migrationBuilder.CreateIndex(name: "IX_Avaliacoes_ViagemId_AvaliadorId_AvaliadoId", table: "Avaliacoes", columns: new[] { "ViagemId", "AvaliadorId", "AvaliadoId" }, unique: true);
    }
}
