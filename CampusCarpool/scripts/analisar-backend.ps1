$ErrorActionPreference = 'Continue'
Set-Location (Join-Path $PSScriptRoot '..')
New-Item -ItemType Directory -Force 'artifacts/analise-estatica' | Out-Null
$dir = 'artifacts/analise-estatica'
dotnet --version | Out-File "$dir/sdk.txt"
dotnet restore CampusCarpool.slnx *> "$dir/restore.log"
$restore = $LASTEXITCODE
if ($restore -eq 0) {
  dotnet build CampusCarpool.slnx --no-restore -c Release -p:RunAnalyzersDuringBuild=true -p:EnableNETAnalyzers=true *> "$dir/dotnet-build.log"
  $build = $LASTEXITCODE
} else { $build = 99; 'Restore falhou.' | Out-File "$dir/dotnet-build.log" }
dotnet format CampusCarpool.slnx --verify-no-changes --no-restore --verbosity diagnostic *> "$dir/dotnet-format.log"
$format = $LASTEXITCODE
@("Data UTC: $((Get-Date).ToUniversalTime().ToString('o'))", "restore_exit_code=$restore", "build_exit_code=$build", "format_exit_code=$format") | Out-File "$dir/resumo.txt"
Write-Host "Resultados: $dir"
if ($restore -ne 0 -or $build -ne 0 -or $format -ne 0) { exit 1 }
