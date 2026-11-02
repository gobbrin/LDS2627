param([string]$Solution = "CampusCarpool.slnx")
$ErrorActionPreference = 'Continue'
Set-Location (Split-Path -Parent $PSScriptRoot)
$folder = 'artifacts/I056'
New-Item -ItemType Directory -Force $folder | Out-Null
$results = @{}
function Run([string]$name, [string[]]$cmdargs) {
    & dotnet @cmdargs *> "$folder/$name.log"
    $script:results[$name] = $LASTEXITCODE
    Write-Host "$name : $LASTEXITCODE"
}
& dotnet --version *> "$folder/sdk.txt"
Run 'restore' @('restore', $Solution, '--force-evaluate')
Run 'dotnet-build' @('build', $Solution, '-c', 'Release', '--no-restore', '-p:EnableNETAnalyzers=true', '-p:RunAnalyzersDuringBuild=true', '-p:EnforceCodeStyleInBuild=true')
Run 'dotnet-format' @('format', $Solution, '--verify-no-changes', '--no-restore', '--verbosity', 'diagnostic')
Run 'audit' @('list', $Solution, 'package', '--vulnerable', '--include-transitive')
Run 'test' @('test', $Solution, '-c', 'Release', '--no-restore')
@('restore','dotnet-build','dotnet-format','audit','test') | ForEach-Object { "${_}_exit_code=$($results[$_])" } | Set-Content "$folder/resumo.txt" -Encoding utf8
python scripts/quality-gate-i056.py "$folder" *> "$folder/quality-gate.log"
$gateCode = $LASTEXITCODE
Get-Content "$folder/quality-gate.log"
exit $gateCode
