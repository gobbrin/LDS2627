# I056 — Análise estática do backend

## Conteúdo
- `.editorconfig`: regras e severidades dos analisadores.
- `Directory.Build.props`: ativa analisadores Roslyn em todos os projetos .NET sob a raiz.
- `scripts/analisar-backend.ps1`: análise em Windows/PowerShell.
- `scripts/analisar-backend.sh`: análise em Linux/macOS.
- `docs/m3-relatorio-analise-estatica-backend-v01.pdf`: relatório preliminar, sem resultados inventados.
- `I056-inventario-preliminar.md`: inventário observado do ZIP de 28/10.

## Instalação
Copiar os ficheiros para a **raiz da solução** (`CampusCarpool/`). `Directory.Build.props` é aplicado aos projetos descendentes. Verificar eventuais definições preexistentes antes de substituir.

## Execução
No PowerShell: `./scripts/analisar-backend.ps1`.
No Bash: `bash scripts/analisar-backend.sh`.
Os logs ficam em `artifacts/analise-estatica/`. O script não corrige nem modifica código; `dotnet format --verify-no-changes` apenas verifica.

## Para concluir a I056
Executar o script com .NET SDK 10 e dependências disponíveis, examinar os diagnósticos, registar correções/falsos positivos com evidências, atualizar o PDF com resultados reais, submeter Merge Request e solicitar revisão/auditoria. A configuração global da pipeline pertence à I057.

## Quality gate proposto (a validar pela equipa)
Falha de `restore`, `build` ou `dotnet format --verify-no-changes` resulta em saída não-zero. Avisos Roslyn não são automaticamente tratados como erros nesta fase; devem ser triados e documentados. Não é um quality gate global de CI.

Ola é o gil aqui, tens de fazer `dotnet format` antes do `dotnet format --verify-no-changes`
dps esta tudo bem :)