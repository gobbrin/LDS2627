# Corre os gates da CI com os testes de SQL Server, se houver servidor.
# Uso, a partir de CampusCarpool/: pwsh scripts/testes-completos.ps1
#
# Os testes de SQL Server leem CAMPUSCARPOOL_TESTES_SQLSERVER (connection string do servidor, sem base de dados):
#   LocalDB:   Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;
#   Contentor: Server=localhost,1433;User Id=sa;Password=<a sua>;TrustServerCertificate=True;

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:CAMPUSCARPOOL_TESTES_SQLSERVER)) {
    Write-Warning 'CAMPUSCARPOOL_TESTES_SQLSERVER não está definida: os testes de SQL Server vão ficar saltados.'
}

dotnet build CampusCarpool.slnx --configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet format whitespace CampusCarpool.slnx --verify-no-changes
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet format style CampusCarpool.slnx --verify-no-changes --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet test CampusCarpool.slnx --configuration Release --no-build --settings coverage.runsettings --collect:"XPlat Code Coverage"
exit $LASTEXITCODE