# Manual de instalação e configuração do backend

**Projeto:** G15 — CampusCarpool
**Issue:** I064
**Executor:** Vítor Samuel Vieira Lopes · **Revisor:** Cristiana Gabriela Oliveira Macedo · **Auditor:** Joana Francisca Pacheco Rodrigues
**Última verificação dos procedimentos:** 06/11/2026 (ver secção 11)
**Versão do backend descrita:** a entregue em m3, `04-artefactos-tecnicos/04.12-backend/m3-v01` (commit `0205ac1` da branch `develop`).

## 1. Objetivo e âmbito

Este manual permite que qualquer elemento da equipa prepare o ambiente, configure e execute o backend do CampusCarpool (API .NET 10) a partir de um clone limpo do repositório, ou da cópia entregue em `04.12-backend/m3-v01` (equivalente à pasta `backend/` do repositório), ligado a uma base de dados SQL Server (local ou Azure SQL).

**Não inclui** credenciais reais. Todos os valores sensíveis surgem como `<PLACEHOLDER>` e devem ser pedidos à equipa por um canal privado e guardados apenas em User Secrets ou variáveis de ambiente (ver secção 6).

## 1.1 Início rápido (cerca de 10 minutos)

Sequência mais curta para ter a API e os testes a correr numa máquina nova, sem pedir nada à equipa. Só precisa do .NET SDK 10 e do Docker Desktop. A palavra-passe abaixo serve apenas para o contentor local: pode ser copiada tal como está.

```powershell
# Na pasta do backend (04.12-backend/m3-v01, ou backend/ no repositório)

# 1. SQL Server local em contentor (esperar cerca de 30 segundos depois deste comando)
docker run -d --name campuscarpool-sql -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=CampusCarpool-Local1!" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# 2. Configuração local: connection string e chave JWT
$ligacao = "Server=localhost,1433;Database=CampusCarpoolDb;User Id=sa;Password=CampusCarpool-Local1!;TrustServerCertificate=True;"
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $ligacao --project src/CampusCarpool.Api
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes)) --project src/CampusCarpool.Api

# 3. Criar a base de dados
dotnet tool install --global dotnet-ef --version 10.0.*
dotnet ef database update --project src/CampusCarpool.Infrastructure --startup-project src/CampusCarpool.Api

# 4. Arrancar a API (fica em http://localhost:5100)
dotnet run --project src/CampusCarpool.Api
```

Noutra janela, `curl http://localhost:5100/health` deve responder `Healthy`.

**Criar uma conta de teste sem serviço de email.** Sem chave Brevo, `POST /api/v1/autenticacao/registo` responde `503`, mas a conta fica criada por confirmar. Basta confirmá-la diretamente na base de dados local:

```powershell
docker exec campuscarpool-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "CampusCarpool-Local1!" -d CampusCarpoolDb -Q "UPDATE Utilizadores SET EmailConfirmado = 1 WHERE Email = 'teste1@estg.ipp.pt'"
```

**Correr todos os testes, sem nenhum saltado:**

```powershell
$env:CAMPUSCARPOOL_TESTES_SQLSERVER = "Server=localhost,1433;User Id=sa;Password=CampusCarpool-Local1!;TrustServerCertificate=True;"
dotnet test CampusCarpool.slnx
```

Mapas, email e push ficam desligados nesta configuração. Os testes não precisam deles, porque usam versões simuladas; as rotas da API que dependem de mapas respondem `503` sem `Maps:ApiKey`.

## 1.2 Porque é que as chaves não vêm na entrega

| Valor | O que é | Porque não é entregue | Como se obtém |
| --- | --- | --- | --- |
| `Jwt:Key` | Segredo com que a API assina os tokens de sessão. Quem o conhecer pode fabricar um token válido de qualquer utilizador | O repositório é público. Uma chave conhecida anula a autenticação | Gera-se em segundos (passo 2) e cada ambiente tem a sua |
| `ConnectionStrings:DefaultConnection` | Endereço do SQL Server e credenciais de acesso | A do Azure daria acesso à base de dados real. O regulamento da UC (§18.4) só admite contas temporárias e limitadas | A local é a do passo 2, sem qualquer segredo real |
| `Brevo:ApiKey`, `Maps:ApiKey`, `Firebase:*` | Chaves de contas em serviços externos | São contas pessoais, com quota, e não podem ser reutilizadas | Conta gratuita em cada serviço, só se for preciso testar a integração real |

Nada disto reduz a testabilidade: a API corre localmente com os dois primeiros valores, e os testes automáticos não precisam de nenhuma chave real.

## 2. Pré-requisitos

| Ferramenta | Versão | Para quê | Obrigatória |
| --- | --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0.x (verificado com 10.0.403) | Compilar, testar e executar | Sim |
| Git | qualquer recente | Obter o código | Sim |
| SQL Server acessível | Contentor Docker (secção 1.1), LocalDB, SQL Server ou Azure SQL | Base de dados da API | Sim |
| `dotnet-ef` (ferramenta global) | 10.0.x | Criar/aplicar o esquema da base de dados | Sim, na primeira instalação |
| Postman | recente | Testes manuais (`03.01.05-postman` e `04.20-documentacao-complementar` da entrega; `docs/testes/I055/` no repositório) | Opcional |
| Python 3 / PowerShell 7+ | — | Apenas o script de análise estática `scripts/analisar-i056.ps1` (ver o relatório de análise estática, `03.01.06`) | Opcional |
| SQL Server para os testes de SQL Server | Contentor `mcr.microsoft.com/mssql/server` ou LocalDB | Executar também os 6 testes específicos (secção 8); sem ele são saltados | Opcional |

Não é necessário Node/Angular para o backend. O frontend é um projeto separado; o backend apenas precisa de conhecer o seu URL (`Frontend:ConfirmUrl`).

Verificar a instalação:

```powershell
dotnet --list-sdks     # tem de listar uma versão 10.0.x
git --version
```

## 3. Obter o código

```powershell
git clone https://github.com/gobbrin/LDS2627.git
cd LDS2627
git checkout develop
```

Na raiz do repositório estão a CI (`.github/`), o backend (`backend/`), os frontends (`frontend-web/`, `frontend-mobile/`) e a documentação (`docs/`). **O código do backend está em `backend/`** e todos os comandos `dotnet` deste manual devem ser executados nessa pasta:

```powershell
cd backend
```

### Estrutura da solução

Solução `CampusCarpool.slnx` (formato XML novo; indicá-la sempre explicitamente nos comandos):

| Projeto | Responsabilidade |
| --- | --- |
| `src/CampusCarpool.Domain` | Entidades, enums, exceções e regras de domínio |
| `src/CampusCarpool.Application` | Módulos da aplicação (`Identidade`, `Viagens`, `Participacoes`, `Comunicacao`, `Notificacoes`, `Avaliacoes`, `Rotas`, `Historico`), cada um com interface e serviço |
| `src/CampusCarpool.Infrastructure` | EF Core (SQL Server), repositórios, migrations, integrações (email, push, mapas), segurança e serviços em segundo plano |
| `src/CampusCarpool.Api` | Controllers, SignalR (`/hubs/pedidos`), `Program.cs` — **projeto executável** |
| `tests/CampusCarpool.Tests` | Testes xUnit (unitários e de integração) |

## 4. Restaurar dependências e compilar

Todas as dependências são pacotes NuGet declarados nos `.csproj`; não há mais nada a instalar manualmente.

```powershell
dotnet restore CampusCarpool.slnx
dotnet build CampusCarpool.slnx
```

Resultado esperado: `0 Error(s)`. Podem aparecer avisos dos analisadores `CA…`, que não impedem a compilação (`TreatWarningsAsErrors` está desligado).

Pacotes principais: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.12, `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.0, `Microsoft.AspNetCore.OpenApi` 10.0.0. SignalR já vem com o ASP.NET Core.

## 5. Base de dados

A API usa **EF Core com SQL Server**. A ligação é lida de `ConnectionStrings:DefaultConnection`. Sem este valor a aplicação lança `InvalidOperationException` ao arrancar.

> **Importante:** a aplicação **não aplica migrations ao arrancar**. A base de dados tem de ser criada/atualizada manualmente (passo 5.3). Se não o fizer, a API arranca mas os endpoints que usam a base de dados falham.

### 5.1 Opção A — SQL Server LocalDB (desenvolvimento local, só Windows)

É o valor por defeito em `appsettings.json`:

```text
Server=(localdb)\MSSQLLocalDB;Database=CampusCarpoolDb;Trusted_Connection=True;TrustServerCertificate=True;
```

LocalDB vem com o Visual Studio (carga de trabalho "Armazenamento e processamento de dados") ou com o SQL Server Express. Confirmar com `sqllocaldb info`. Não é preciso configurar mais nada. Em máquinas sem LocalDB, usar o contentor da secção 1.1.

### 5.2 Opção B — Azure SQL Database

1. No portal Azure, anotar: nome do servidor (`<SERVIDOR>.database.windows.net`), nome da base de dados, utilizador administrador e palavra-passe (pedir à equipa; **não os escrever neste repositório**).
2. **Firewall:** no recurso do servidor SQL → *Networking* → adicionar o IP público da máquina de cada programador (ou, para o ambiente publicado, ativar *Allow Azure services and resources to access this server*). Sem esta regra a ligação falha com erro de firewall.
3. Connection string (formato ADO.NET do Azure):

```text
Server=tcp:<SERVIDOR>.database.windows.net,1433;Initial Catalog=<BASE_DE_DADOS>;User ID=<UTILIZADOR>;Password=<PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Guardá-la em User Secrets ou em variável de ambiente (secção 6) — nunca em `appsettings.json`.

### 5.3 Criar/atualizar o esquema (migrations)

As migrations estão versionadas em `src/CampusCarpool.Infrastructure/Migrations`: 15 migrations, de `InitialCreate` a `I110_TamanhosERestricoes`. `I110_CoordenadasObrigatorias` falha de propósito se houver viagens antigas sem coordenadas: uma base de dados com esses dados deve ser recriada.

Os scripts SQL já gerados (criação, migração idempotente e migração das bases anteriores à I110) estão na entrega em `04-artefactos-tecnicos/04.13-scripts-de-base-de-dados/m3-v01`, com instruções no `LEIA-ME.md` dessa pasta. São a alternativa ao `dotnet ef` quando não se quer instalar a ferramenta.

Instalar a ferramenta (uma vez por máquina):

```powershell
dotnet tool install --global dotnet-ef --version 10.0.*
dotnet ef --version
```

Aplicar as migrations à base de dados configurada:

```powershell
dotnet ef database update `
  --project src/CampusCarpool.Infrastructure `
  --startup-project src/CampusCarpool.Api
```

O `dotnet ef` usa a configuração do projeto Api (incluindo User Secrets, porque o ambiente por defeito das ferramentas EF é `Development`). Para apontar para outra base de dados sem alterar os secrets, adicionar `--connection "<connection string>"`.

Para ver o SQL antes de o aplicar (recomendado em Azure/produção):

```powershell
New-Item -ItemType Directory -Force artifacts | Out-Null   # a pasta está no .gitignore
dotnet ef migrations script --idempotent `
  --project src/CampusCarpool.Infrastructure `
  --startup-project src/CampusCarpool.Api `
  --output artifacts/schema.sql
```

Criar uma nova migration só é necessário quando o modelo mudar:
`dotnet ef migrations add <Nome> --project src/CampusCarpool.Infrastructure --startup-project src/CampusCarpool.Api`.

## 6. Configuração

### 6.1 Como a configuração é lida

O ASP.NET Core junta, por esta ordem (a última ganha): `appsettings.json` → `appsettings.{Ambiente}.json` → **User Secrets** (apenas em `Development`) → **variáveis de ambiente** → argumentos da linha de comandos.

- Em variáveis de ambiente, o separador `:` escreve-se `__` (ex.: `Jwt:Key` → `Jwt__Key`).
- `dotnet run` com o perfil de lançamento define `ASPNETCORE_ENVIRONMENT=Development`; sem perfil, o ambiente é `Production` e os User Secrets **não** são carregados.

> **Atenção:** o `appsettings.json` versionado só contém valores não secretos (connection string LocalDB de desenvolvimento, `Jwt:Issuer`/`Audience`, preço por km, domínios académicos, origens CORS e `Frontend:ConfirmUrl`). **Não contém `Jwt:Key` nem `Maps:ApiKey`** (retiradas na I110): a API **recusa arrancar** sem uma `Jwt:Key` com pelo menos 32 bytes, por isso é obrigatório defini-la em User Secrets (6.3) ou numa variável de ambiente (6.4) antes do primeiro `dotnet run`.

### 6.2 Chaves de configuração

| Chave | Obrigatória | Descrição | Exemplo / formato |
| --- | --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | **Sim** | Ligação SQL Server | ver secção 5 |
| `Jwt:Key` | **Sim** | Chave de assinatura dos tokens; mínimo 32 bytes, senão a API não arranca | `<CHAVE_ALEATORIA_32+_BYTES>` |
| `Jwt:Issuer` | Sim | Emissor do token | `CampusCarpool` |
| `Jwt:Audience` | Sim | Audiência do token | `CampusCarpool.Clients` |
| `Frontend:ConfirmUrl` | Para registo | URL do frontend a que o email de confirmação aponta; o token é acrescentado como `?token=` | `http://localhost:4200/confirmar-conta` |
| `EmailAcademico:Dominios` | **Sim** | Domínios de email aceites no registo. Lista vazia recusa todos os registos | `["estg.ipp.pt"]` |
| `Cors:Origens` | Para o frontend | Origens autorizadas a chamar a API | `["http://localhost:4200"]` |
| `Brevo:ApiKey` | Para registo | Chave da API Brevo (envio do email de confirmação) | `<BREVO_API_KEY>` |
| `Brevo:FromEmail` | Para registo | Remetente (validado na Brevo) | `<EMAIL_REMETENTE>` |
| `Brevo:FromName` | Não | Nome do remetente | `Campus Carpool` |
| `Maps:BaseUrl` | Não | Base do OpenRouteService | `https://api.heigit.org/openrouteservice` |
| `Maps:ApiKey` | Para rotas/estimativas | Chave OpenRouteService; sem ela os endpoints de mapas falham | `<ORS_API_KEY>` |
| `Maps:TimeoutSeconds` | Não | Timeout dos pedidos de mapas (defeito 10) | `10` |
| `Custos:PrecoPorKm` | Para estimativa de custo | Preço por km | `0.10` |
| `ConclusaoViagens:IntervaloSegundos` | Não | Intervalo, em segundos, com que o serviço em segundo plano conclui as viagens cuja hora de chegada prevista já passou (RF-F002-12); 60 por omissão | `60` |
| `Firebase:ProjectId`, `Firebase:ClientEmail`, `Firebase:PrivateKey` | Não | Notificações push (FCM). Se faltar algum, o push é ignorado com um aviso no log. A chave privada é uma PEM com `\n` literais | `<FIREBASE_…>` |

Notas:
- O registo (`POST /api/v1/autenticacao/registo`) grava o utilizador e **depois** envia o email. Sem `Brevo:*` e `Frontend:ConfirmUrl` devolve `503` (a conta fica criada mas por confirmar). Para testar fluxos autenticados sem Brevo, confirmar a conta na base de dados (secção 1.1).
- Só são aceites emails dos domínios em `EmailAcademico:Dominios` e palavras-passe com pelo menos 8 caracteres, uma letra e um dígito.
- O envio de email usa só `Brevo:*` (a antiga secção `Email:*`, que o código não lia, foi retirada do `appsettings.json` na I110).
- `Maps:BaseUrl` e `Maps:Provider` não têm efeito: o serviço de mapas usa os URL absolutos do OpenRouteService (`api.heigit.org`).

### 6.3 Opção recomendada — User Secrets (local)

O projeto Api já tem `UserSecretsId`. A partir de `backend/`:

```powershell
$api = "src/CampusCarpool.Api"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<CONNECTION_STRING>" --project $api
dotnet user-secrets set "Jwt:Key" "<CHAVE_ALEATORIA_32+_BYTES>" --project $api
dotnet user-secrets set "Brevo:ApiKey" "<BREVO_API_KEY>" --project $api
dotnet user-secrets set "Brevo:FromEmail" "<EMAIL_REMETENTE>" --project $api
dotnet user-secrets set "Maps:ApiKey" "<ORS_API_KEY>" --project $api
dotnet user-secrets set "Frontend:ConfirmUrl" "http://localhost:4200/confirmar-conta" --project $api
dotnet user-secrets list --project $api
```

Gerar uma `Jwt:Key` aleatória (PowerShell):

```powershell
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

A chave tem 64 caracteres (48 bytes), acima do mínimo de 32 bytes exigido. Usar uma chave **diferente** em cada ambiente.

Os secrets ficam fora do repositório (`%APPDATA%\Microsoft\UserSecrets\9f083261-f775-481a-92c7-295b90385f3c\secrets.json`).

### 6.4 Alternativa — variáveis de ambiente (servidores, CI, ambiente público)

```powershell
$env:ConnectionStrings__DefaultConnection = "<CONNECTION_STRING>"
$env:Jwt__Key = "<CHAVE_ALEATORIA_32+_BYTES>"
$env:Jwt__Issuer = "CampusCarpool"
$env:Jwt__Audience = "CampusCarpool.Clients"
$env:Brevo__ApiKey = "<BREVO_API_KEY>"
$env:Brevo__FromEmail = "<EMAIL_REMETENTE>"
$env:Maps__ApiKey = "<ORS_API_KEY>"
$env:Frontend__ConfirmUrl = "<URL_DO_FRONTEND>/confirmar-conta"
```

No Azure App Service estas chaves configuram-se em *Settings → Environment variables* (ligação à BD também em *Connection strings*, tipo `SQLAzure`). Ficheiros `appsettings.Local.json`, `appsettings.Production.json`, `.env*` e `secrets.json` estão no `.gitignore`.

## 7. Executar

```powershell
dotnet run --project src/CampusCarpool.Api
```

O perfil `http` de `Properties/launchSettings.json` escuta em **`http://localhost:5100`** com `ASPNETCORE_ENVIRONMENT=Development`.

Verificar:

```powershell
curl http://localhost:5100/health     # Healthy (503 se a base de dados não responder)
curl http://localhost:5100/           # {"service":"Campus Carpool API","status":"running"}
```

Endpoints úteis: API REST em `/api/v1/...`, hub SignalR em `/hubs/pedidos` (o token JWT segue em `?access_token=` apenas neste caminho), OpenAPI apenas em `Development`. Limites de pedidos (resposta `429`): 100 por minuto por utilizador ou IP e 10 por minuto por IP no login, no registo e no reenvio da confirmação.

> O `/health` executa uma consulta real à base de dados: responde `503` se ela não estiver acessível.

### Testar com Postman

Importar a collection `03-artefactos-de-qualidade-de-produto/03.01-backend/03.01.05-postman/m3-collection-postman-v01.json` e o environment `04-artefactos-tecnicos/04.20-documentacao-complementar/m3-environment-postman-v01.json`; as instruções estão em `m3-postman-instrucoes-execucao-v01.md`, na mesma pasta. O environment não traz contas nem tokens: preencher `emailCondutor`, `passwordCondutor`, `emailPassageiro` e `passwordPassageiro` com contas de teste confirmadas e ajustar `baseUrl` para `http://localhost:5100`.

## 8. Testes e verificações de qualidade

```powershell
dotnet test CampusCarpool.slnx
dotnet test CampusCarpool.slnx --filter "FullyQualifiedName~ViagemServiceTests"
dotnet test CampusCarpool.slnx --settings coverage.runsettings --collect:"XPlat Code Coverage"   # cobertura, sem migrations
dotnet format whitespace CampusCarpool.slnx --verify-no-changes
dotnet format style CampusCarpool.slnx --verify-no-changes
```

- Os testes de integração usam SQLite em memória (ver 8.1). Os testes que dependem de SQL Server leem a ligação da variável `CAMPUSCARPOOL_TESTES_SQLSERVER`; cada um cria e apaga a sua base de dados. **Sem a variável, esses 6 testes são saltados** e os restantes não são afetados.
- Os testes **não** usam o Azure SQL nem a connection string da aplicação: correr `dotnet test` não toca na base de dados partilhada.
- A pipeline oficial é o GitHub Actions (`.github/workflows/backend-ci.yml`; a antiga `.gitlab-ci.yml` foi retirada): restore → build Release → `dotnet format whitespace` e `style` com `--verify-no-changes` → pacotes vulneráveis → SQL Server em contentor → testes com cobertura (sem migrations) → a mesma suite em SQL Server → cobertura mínima de **80 %** de linhas → publish. Correr os dois `format` localmente antes de fazer push. Descrição completa em `04.16-ci-cd/m3-descricao-pipeline-ci-cd-v01.md`.

### 8.1 SQLite em memória e SQL Server real

Os testes correm de duas formas, e a pipeline executa ambas:

| | SQLite em memória | SQL Server real (contentor) |
| --- | --- | --- |
| O que é | Uma base de dados que vive na memória do processo de teste. Nasce no início do teste e desaparece no fim, sem servidor nem ficheiros | O mesmo motor usado em produção, num contentor Docker |
| Esquema | Criado a partir do modelo (`EnsureCreated`) | Criado pelas migrations reais |
| Tempo | Cerca de 25 segundos para toda a suite | Cerca de 20 minutos |
| Precisa de instalação | Não | Docker e a variável `CAMPUSCARPOOL_TESTES_SQLSERVER` |
| O que não verifica | Concorrência com `rowversion`, `ISJSON`, tipos e `CHECK` próprios do SQL Server, migrations | Verifica tudo isto |

O SQLite é o teste do dia a dia: rápido, isolado e sem dependências. O SQL Server dá a garantia de que o comportamento é igual ao de produção. Por isso, numa máquina sem SQL Server aparecem **6 testes saltados** (só existem para SQL Server), e isso não é uma falha. Com a variável definida (secção 1.1) nenhum é saltado.

Na pipeline: `$env:CAMPUSCARPOOL_TESTES_BD = "SqlServer"` repete a suite inteira em SQL Server; nessa execução 1 teste (`[SoSqliteFact]`) é saltado de propósito, porque só faz sentido em SQLite.

## 9. Ambiente público (Azure)

A publicação em si pertence à I063; aqui ficam apenas as configurações que o backend exige nesse ambiente. Instância publicada atualmente (Azure App Service, Germany West Central):

```text
https://campuscarpool-api-g15-ase2b6g6cbabf7d7.germanywestcentral-01.azurewebsites.net
```

**A instância publicada é anterior à I110**: não corresponde ao backend entregue. Para avaliar a versão de m3, seguir a secção 1.1.

> **Cuidados em produção**
> - **Não reutilizar** a `Jwt:Key` nem quaisquer credenciais de desenvolvimento.
> - **Não confiar** nos valores do `appsettings.json` (são de desenvolvimento): definir todas as chaves por variáveis de ambiente do serviço.
> - **Rever e testar** o script SQL num ambiente de teste antes de o aplicar à base de dados pública.

1. Produzir o pacote: `dotnet publish src/CampusCarpool.Api/CampusCarpool.Api.csproj --configuration Release --output publish`.
2. Definir **todas** as chaves da secção 6.2 por variáveis de ambiente do serviço (nunca confiar nos valores de `appsettings.json`), com uma `Jwt:Key` **diferente** da de desenvolvimento.
3. Ajustar `Frontend:ConfirmUrl` para o URL público do frontend.
4. Azure SQL: permitir o acesso do serviço no firewall e usar `Encrypt=True;TrustServerCertificate=False`.
5. Aplicar o esquema à BD pública com o script idempotente da secção 5.3 (rever antes de executar) — o arranque não aplica migrations.
6. Em `Production` não há OpenAPI; verificar com `GET /health`.
7. O serviço fica atrás de HTTPS (a API usa `UseHttpsRedirection`).

## 10. Pontos de atenção conhecidos

1. **Chaves que estiveram no repositório.** As antigas `Jwt:Key` e `Maps:ApiKey` e as credenciais do Azure SQL ficaram no histórico público do git. Têm de ser regeneradas e nunca reutilizadas.
2. **Partilha de credenciais.** Chaves e palavras-passe não devem circular em chat; se já foram partilhadas, regenerá-las.
3. **READMEs internos.** `backend/README.md` e `database/scripts/README.md` referem `ConnectionStrings__CampusCarpool` e `Persistence/Migrations`. Vale este manual: `DefaultConnection` e `Infrastructure/Migrations`.

## 11. Verificação dos procedimentos

Verificação inicial a 02/11/2026 (I064). **Última verificação a 06/11/2026**, no commit `0205ac1`, o backend entregue, em Windows 11 com .NET SDK 10.0.403:

| Procedimento | Resultado |
| --- | --- |
| `dotnet test` em SQLite, sem SQL Server | 668 aprovados, 0 falhados, 6 saltados (os de SQL Server) |
| Pipeline 42202915885, com SQL Server real | Todos os passos verdes: 674 aprovados, 0 saltados; repetição em SQL Server: 673 aprovados, 1 saltado de propósito; cobertura de 95,73 % |
| Arranque da API com `Jwt__Key` e ligação a um servidor inexistente, em `Production` | `GET /` responde 200 e `GET /health` responde 503 (comportamento esperado) |
| Contentor SQL Server e migrations | Executados pela pipeline em cada push |
| Passos da secção 1.1 com Docker | **Não executados nesta máquina**, sem motor Docker funcional. O contentor e as migrations são os da pipeline. Falta validar a confirmação de conta por SQL e o registo a responder 503 sem Brevo |
| Azure SQL, envio de email Brevo, FCM, publicação Azure | **Não executados** (exigem credenciais da equipa) |

## 12. Resolução de problemas

| Sintoma | Causa provável | Solução |
| --- | --- | --- |
| `InvalidOperationException: ConnectionStrings:DefaultConnection não configurada` ao arrancar | Chave em falta ou com nome errado (ex.: `ConnectionStrings__CampusCarpool`) | Definir `ConnectionStrings:DefaultConnection` (secção 6) |
| `Definir Jwt:Key com pelo menos 32 bytes…` | `Jwt:Key` ausente ou curta | Gerar uma chave com o comando da secção 6.3 |
| Os User Secrets parecem ignorados | A API correu em `Production` (ex.: `--no-launch-profile`) | Usar o perfil `http` ou `ASPNETCORE_ENVIRONMENT=Development` |
| `Cannot open server … Client with IP address … is not allowed to access the server` | Firewall do Azure SQL | Adicionar o IP público da máquina no portal Azure (secção 5.2) |
| `Login failed for user` | Utilizador/palavra-passe errados ou caracteres especiais mal escapados | Confirmar credenciais; numa connection string, envolver a palavra-passe em aspas duplas se tiver `;` ou `=` (não é solução universal) |
| Erros `Invalid object name '…'` nos endpoints | Migrations não aplicadas | `dotnet ef database update` (secção 5.3) |
| `dotnet ef` não encontrado | Ferramenta global não instalada | `dotnet tool install --global dotnet-ef --version 10.0.*` |
| Registo devolve `503` | Brevo ou `Frontend:ConfirmUrl` não configurados | Definir `Brevo:*` e `Frontend:ConfirmUrl` |
| Endpoints de rotas/estimativa falham | `Maps:ApiKey` em falta ou inválida | Confirmar a chave OpenRouteService |
| `401` com token válido | `Jwt:Key`, `Issuer` ou `Audience` diferentes dos usados ao emitir o token | Usar os mesmos valores em todo o ambiente |
| `429 Too Many Requests` | Limite de pedidos (secção 7) | Aguardar um minuto |
| Aparecem 6 testes saltados | `CAMPUSCARPOOL_TESTES_SQLSERVER` não definida (secção 8.1) | Definir a variável (secção 1.1) ou aceitar: não é uma falha |
| `/health` responde 503 | Base de dados inacessível | Confirmar o contentor (`docker ps`) e a connection string |

## 13. Referências

- [Instalar o .NET](https://learn.microsoft.com/dotnet/core/install/)
- [Configuração no ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
- [Ferramentas do EF Core (`dotnet ef`)](https://learn.microsoft.com/ef/core/cli/dotnet)
- [Migrations do EF Core](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Azure SQL Database — regras de firewall](https://learn.microsoft.com/azure/azure-sql/database/firewall-configure)
- [Brevo — API de email transacional](https://developers.brevo.com/)
- [OpenRouteService](https://openrouteservice.org/dev/)
