#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
mkdir -p artifacts/analise-estatica
LOG="artifacts/analise-estatica/dotnet-build.log"
FORMAT="artifacts/analise-estatica/dotnet-format.log"
SUMMARY="artifacts/analise-estatica/resumo.txt"
printf 'Data UTC: %s\nSDK: ' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" > "$SUMMARY"
dotnet --version >> "$SUMMARY" 2>&1
printf '\n[BUILD + ANALISADORES]\n' >> "$SUMMARY"
dotnet restore CampusCarpool.slnx > artifacts/analise-estatica/restore.log 2>&1
restore_code=$?
if [ "$restore_code" -eq 0 ]; then
  dotnet build CampusCarpool.slnx --no-restore -c Release -p:RunAnalyzersDuringBuild=true -p:EnableNETAnalyzers=true > "$LOG" 2>&1
  build_code=$?
else
  printf 'Restore falhou; build não executado.\n' > "$LOG"
  build_code=99
fi
printf 'restore_exit_code=%s\nbuild_exit_code=%s\n' "$restore_code" "$build_code" >> "$SUMMARY"
printf '\n[FORMAT VERIFY]\n' >> "$SUMMARY"
dotnet format CampusCarpool.slnx --verify-no-changes --no-restore --verbosity diagnostic > "$FORMAT" 2>&1
format_code=$?
printf 'format_exit_code=%s\n' "$format_code" >> "$SUMMARY"
printf '\nAvisos/erros do build:\n' >> "$SUMMARY"
grep -Ei 'warning |error |aviso |erro ' "$LOG" | sort -u >> "$SUMMARY" || true
printf '\nResumo guardado em %s\n' "$SUMMARY"
if [ "$restore_code" -ne 0 ] || [ "$build_code" -ne 0 ] || [ "$format_code" -ne 0 ]; then exit 1; fi
