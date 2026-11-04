# I058 — Descrição da pipeline CI/CD do backend

## Objetivo
A pipeline oficial do backend corre no **GitHub Actions** (decisão do grupo G13, 04/11/2026).
Valida e empacota o backend .NET 10: restore, build, análise estática, verificação de pacotes
vulneráveis, testes com cobertura, quality gate de cobertura e publish. Cada passo só corre se o
anterior terminar com sucesso; qualquer falha deixa a execução a vermelho.

A antiga pipeline GitLab CI (`CampusCarpool/.gitlab-ci.yml`) foi retirada: só fazia restore,
build, testes e publish, sem análise estática nem cobertura, e não era a pipeline em que havia
execuções registadas.

## Ficheiro de configuração
O workflow está em `.github/workflows/backend-ci.yml`, na raiz do repositório, e usa:

- runner `ubuntu-latest`;
- SDK .NET indicado em `CampusCarpool/global.json` (`actions/setup-dotnet` com `global-json-file`);
- diretório de trabalho `CampusCarpool/` em todos os passos `run`.

## Gatilhos
Corre em cada `push` (qualquer branch), em cada `pull_request` e manualmente
(`workflow_dispatch`).

## Passos
1. **Restore** — `dotnet restore CampusCarpool.slnx`.
2. **Build** — `dotnet build CampusCarpool.slnx --configuration Release --no-restore`.
3. **Static Analysis** — `dotnet format whitespace` e `dotnet format style`, ambos com
   `--verify-no-changes`: falha se algum ficheiro não cumprir o `.editorconfig`. Os analisadores
   .NET correm também no build (`AnalysisMode=Recommended`, `EnforceCodeStyleInBuild`).
4. **Vulnerable Packages** — `dotnet list package --vulnerable --include-transitive`; falha se
   houver pacotes NuGet com vulnerabilidades conhecidas.
5. **Test with Coverage** — `dotnet test` em Release com `--settings coverage.runsettings`
   (exclui as Migrations da cobertura), `XPlat Code Coverage` e relatório `trx`.
6. **Quality Gate** — soma as linhas cobertas e válidas dos relatórios Cobertura e falha se a
   cobertura de linhas for inferior a `MIN_COVERAGE` (**80 %**, QG-04 e RNF-MAN-01; decisão G14).
7. **Publish** — `dotnet publish src/CampusCarpool.Api/CampusCarpool.Api.csproj` em Release.
8. **Upload artifacts** — guarda `CampusCarpool/publish/` (artefacto `campuscarpool-backend`)
   e, mesmo com falhas, `CampusCarpool/TestResults/` (artefacto `campuscarpool-test-evidence`,
   com o `trx` e a cobertura), ambos durante 7 dias.

```text
restore -> build -> análise estática -> pacotes vulneráveis -> testes + cobertura -> quality gate -> publish -> artefactos
```

## Teste que precisa de SQL Server
O IT023 (`ConcorrenciaSqlServerIntegrationTests`) usa um SQL Server real. Lê a ligação ao servidor
da variável de ambiente `CAMPUSCARPOOL_TESTES_SQLSERVER` e fica marcado como **saltado** quando ela
não existe (`SqlServerFactAttribute`). A pipeline não fornece SQL Server; os restantes testes usam
SQLite em memória.

## Variáveis e segredos
Credenciais SQL Server, JWT, Brevo, Firebase e OpenRouteService não são guardadas no repositório.
A pipeline não precisa de nenhuma: os testes definem a sua própria configuração. Os ambientes que
as precisem (App Service, máquinas de desenvolvimento) usam variáveis de ambiente ou User Secrets
(ver o manual de instalação I064).

## Validação
Local, a partir da raiz do repositório (réplica dos passos da pipeline):

```bash
cd CampusCarpool
dotnet restore CampusCarpool.slnx
dotnet build CampusCarpool.slnx --configuration Release --no-restore
dotnet format whitespace CampusCarpool.slnx --verify-no-changes
dotnet format style CampusCarpool.slnx --verify-no-changes --no-restore
dotnet list CampusCarpool.slnx package --vulnerable --include-transitive
dotnet test CampusCarpool.slnx --configuration Release --no-restore --settings coverage.runsettings --collect:"XPlat Code Coverage"
```

No GitHub, abrir **Actions > CampusCarpool Backend CI**, selecionar a execução e abrir o job
`Restore, Build, Test, Analyse and Publish`. Em caso de falha, abrir o primeiro passo a vermelho:

- `Restore`: dependências ou acesso ao NuGet;
- `Build`: erros de compilação;
- `Static Analysis`: ficheiros por formatar (correr `dotnet format` localmente);
- `Vulnerable Packages`: pacote a atualizar (lista no log);
- `Test with Coverage`: testes falhados (detalhe no artefacto `campuscarpool-test-evidence`);
- `Quality Gate`: cobertura abaixo do limiar (o log mostra a cobertura medida e a exigida);
- `Publish`: problemas de empacotamento.

## Limites
A pipeline não executa:

- deployment para produção;
- aplicação de migrations;
- testes com SQL Server (o IT023 fica saltado);
- testes end-to-end;
- criação/publicação de imagem da aplicação.

A proteção da branch `main` (exigir a pipeline verde antes do merge) é configurada no GitHub em
**Settings > Branches** pelo dono do repositório.
