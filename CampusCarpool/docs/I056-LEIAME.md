# I056 — Análise estática do backend

## Conteúdo
- `.editorconfig`: regras e severidades dos analisadores.
- `Directory.Build.props`: ativa analisadores Roslyn em todos os projetos .NET sob a raiz.
- `scripts/analisar-i056.ps1`: script principal de análise (PowerShell 7+, cross-platform).
- `scripts/quality-gate-i056.py`: avaliador do quality gate (Python 3).
- `docs/AnaliseEstatica/m3-relatorio-analise-estatica-backend-v01.pdf`: relatório preliminar.
- `I056-inventario-preliminar.md`: inventário observado do ZIP de 28/10.

## Instalação
Copiar os ficheiros para a **raiz da solução** (`CampusCarpool/`). `Directory.Build.props` é aplicado aos projetos descendentes. Verificar eventuais definições preexistentes antes de substituir.

## Execução

Requisitos: .NET SDK 10, Python 3, PowerShell 7+ (`pwsh`).

```powershell
./scripts/analisar-i056.ps1