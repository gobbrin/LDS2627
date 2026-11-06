# I064 — Manual de instalação e configuração do backend

**Projeto:** G15 — CampusCarpool
**Issue:** I064 · **Branch:** `docs/i064-manual-instalacao-backend`
**Executor:** Vítor Samuel Vieira Lopes · **Revisor:** Cristiana Gabriela Oliveira Macedo · **Auditor:** Joana Francisca Pacheco Rodrigues
**Última verificação dos procedimentos:** 02/11/2026 (ver [secção 11](#11-verificação-dos-procedimentos))

## 1. Objetivo e âmbito

Este manual permite que qualquer elemento da equipa prepare o ambiente, configure e execute o backend do CampusCarpool (API .NET 10) a partir de um clone limpo do repositório, ligado a uma base de dados SQL Server (local ou Azure SQL).

**Não inclui** credenciais reais. Todos os valores sensíveis surgem como `<PLACEHOLDER>` e devem ser pedidos à equipa por um canal privado e guardados apenas em User Secrets ou variáveis de ambiente (ver [secção 6](#6-configuração)).

## 2. Pré-requisitos

| Ferramenta | Versão | Para quê | Obrigatória |
| --- | --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0.x (verificado com 10.0.403) | Compilar, testar e executar | Sim |
| Git | qualquer recente | Obter o código | Sim |
| SQL Server acessível | LocalDB, SQL Server ou Azure SQL | Base de dados da API | Sim |
| `dotnet-ef` (ferramenta global) | 10.0.x | Criar/aplicar o esquema da base de dados | Sim, na primeira instalação |
| Postman | recente | Testes manuais (`docs/testes/I055/`) | Opcional |
| Python 3 / PowerShell 7+ | — | Apenas scripts de análise estática (`docs/qualidade/analise-estatica/I056-LEIAME.md`) | Opcional |

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
git checkout develop        # ou a branch pretendida
```

A raiz do repositório contém apenas CI, collections Postman e documentos de entrega. **Todo o código está em `CampusCarpool/`** e todos os comandos `dotnet` deste manual devem ser executados nessa pasta:

```powershell
cd CampusCarpool
```

### Estrutura da solução

Solução `CampusCarpool.slnx` (formato XML novo; indicá-la sempre explicitamente nos comandos):

| Projeto | Responsabilidade |
| --- | --- |
| `src/CampusCarpool.Domain` | Entidades, enums, exceções e regras de domínio |
| `src/CampusCarpool.Application` | Interfaces, serviços, validadores, job de conclusão de viagens |
| `src/CampusCarpool.Infrastructure` | EF Core (SQL Server), repositórios, migrations, integrações (email, push, mapas) |
| `src/CampusCarpool.Api` | Controllers, SignalR (`/hubs/pedidos`), `Program.cs` — **projeto executável** |
| `tests/CampusCarpool.Tests` | Testes xUnit (unitários e de integração) |

## 4. Restaurar dependências e compilar

Todas as dependências são pacotes NuGet declarados nos `.csproj`; não há mais nada a instalar manualmente.

```powershell
dotnet restore CampusCarpool.slnx
dotnet build CampusCarpool.slnx
```

Resultado esperado: `0 Error(s)`. Aparecem centenas de *warnings* (analisadores `CA…` e avisos `NU1902/NU1903` de pacotes com vulnerabilidades conhecidas); não impedem a compilação (`TreatWarningsAsErrors` está desligado).

Pacotes principais: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.12, `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.0, `Microsoft.AspNetCore.OpenApi` 10.0.0. SignalR já vem com o ASP.NET Core.

## 5. Base de dados

A API usa **EF Core com SQL Server**. A ligação é lida de `ConnectionStrings:DefaultConnection` (e **não** de `ConnectionStrings__CampusCarpool`, como indica o `CampusCarpool/README.md` — está desatualizado). Sem este valor a aplicação lança `InvalidOperationException` ao arrancar.

> **Importante:** a aplicação **não aplica migrations ao arrancar**. A base de dados tem de ser criada/atualizada manualmente (passo 5.3). Se não o fizer, a API arranca mas os endpoints que usam a base de dados falham.

### 5.1 Opção A — SQL Server LocalDB (desenvolvimento local, só Windows)

É o valor por defeito em `appsettings.json`:

```text
Server=(localdb)\MSSQLLocalDB;Database=CampusCarpoolDb;Trusted_Connection=True;TrustServerCertificate=True;
```

LocalDB vem com o Visual Studio (carga de trabalho "Armazenamento e processamento de dados") ou com o SQL Server Express. Confirmar com `sqllocaldb info`. Não é preciso configurar mais nada.

### 5.2 Opção B — Azure SQL Database

1. No portal Azure, anotar: nome do servidor (`<SERVIDOR>.database.windows.net`), nome da base de dados, utilizador administrador e palavra-passe (pedir à equipa; **não os escrever neste repositório**).
2. **Firewall:** no recurso do servidor SQL → *Networking* → adicionar o IP público da máquina de cada programador (ou, para o ambiente publicado, ativar *Allow Azure services and resources to access this server*). Sem esta regra a ligação falha com erro de firewall.
3. Connection string (formato ADO.NET do Azure):

```text
Server=tcp:<SERVIDOR>.database.windows.net,1433;Initial Catalog=<BASE_DE_DADOS>;User ID=<UTILIZADOR>;Password=<PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Guardá-la em User Secrets ou em variável de ambiente (secção 6) — nunca em `appsettings.json`.

### 5.3 Criar/atualizar o esquema (migrations)

As migrations estão versionadas em `src/CampusCarpool.Infrastructure/Migrations` (`InitialCreate` … `AdicionarVersaoViagem`).

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

> **Atenção:** o `appsettings.json` versionado contém valores de desenvolvimento por defeito (uma `Jwt:Key` de exemplo, a connection string LocalDB e uma `Maps:ApiKey`). Por isso a API arranca "sem configurar nada" — mas esses valores **não servem para ambientes partilhados nem públicos** e devem ser sobrepostos por segredos reais. Ver [secção 10](#10-pontos-de-atenção-conhecidos).

### 6.2 Chaves de configuração

| Chave | Obrigatória | Descrição | Exemplo / formato |
| --- | --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | **Sim** | Ligação SQL Server | ver secção 5 |
| `Jwt:Key` | **Sim** | Chave de assinatura dos tokens; mínimo 32 bytes, senão a API não arranca | `<CHAVE_ALEATORIA_32+_BYTES>` |
| `Jwt:Issuer` | Sim | Emissor do token | `CampusCarpool` |
| `Jwt:Audience` | Sim | Audiência do token | `CampusCarpool.Clients` |
| `Frontend:ConfirmUrl` | Para registo | URL do frontend a que o email de confirmação aponta; o token é acrescentado como `?token=` | `http://localhost:4200/confirmar-conta` |
| `Brevo:ApiKey` | Para registo | Chave da API Brevo (envio do email de confirmação) | `<BREVO_API_KEY>` |
| `Brevo:FromEmail` | Para registo | Remetente (validado na Brevo) | `<EMAIL_REMETENTE>` |
| `Brevo:FromName` | Não | Nome do remetente | `Campus Carpool` |
| `Maps:BaseUrl` | Não | Base do OpenRouteService | `https://api.heigit.org/openrouteservice` |
| `Maps:ApiKey` | Para rotas/estimativas | Chave OpenRouteService; sem ela os endpoints de mapas falham | `<ORS_API_KEY>` |
| `Maps:TimeoutSeconds` | Não | Timeout dos pedidos de mapas (defeito 10) | `10` |
| `Custos:PrecoPorKm` | Para estimativa de custo | Preço por km | `0.10` |
| `Firebase:ProjectId`, `Firebase:ClientEmail`, `Firebase:PrivateKey` | Não | Notificações push (FCM). Se faltar algum, o push é ignorado com um aviso no log. A chave privada é uma PEM com `\n` literais | `<FIREBASE_…>` |

Notas:
- O registo (`POST /api/v1/autenticacao/registar`) grava o utilizador e **depois** envia o email. Sem `Brevo:*` e `Frontend:ConfirmUrl` devolve `503` (a conta fica criada mas por confirmar). Para testar fluxos autenticados é preciso a Brevo configurada, ou uma conta já confirmada na base de dados.
- Só são aceites emails `@estg.ipp.pt` e palavras-passe com pelo menos 12 caracteres.
- A secção `Email:*` do `appsettings.json` (SMTP) **não é usada** pelo código atual; a integração real é `Brevo:*`.

### 6.3 Opção recomendada — User Secrets (local)

O projeto Api já tem `UserSecretsId`. A partir de `CampusCarpool/`:

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
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
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
curl http://localhost:5100/health     # Healthy
curl http://localhost:5100/           # {"service":"Campus Carpool API","status":"running"}
```

Endpoints úteis: API REST em `/api/v1/...`, hub SignalR em `/hubs/pedidos` (o token JWT segue em `?access_token=` apenas neste caminho), OpenAPI apenas em `Development`. Há um *rate limit* global de 60 pedidos/minuto por IP (resposta `429`).

> O `/health` apenas confirma que o processo está vivo; **não testa a ligação à base de dados**. Para validar a BD, chamar um endpoint que a use (por exemplo, registar um utilizador).

### Testar com Postman

Importar a collection e o environment oficiais de `docs/testes/I055/` (ver `docs/testes/I055/I055_LEIA-ME.md`). Ajustar `baseUrl` para o URL em que a API está a escutar — o environment local antigo na raiz do repositório usa `https://localhost:7001`, que **não** corresponde ao perfil atual (`http://localhost:5100`).

## 8. Testes e verificações de qualidade

```powershell
dotnet test CampusCarpool.slnx
dotnet test CampusCarpool.slnx --filter "FullyQualifiedName~ViagemServiceTests"
dotnet format whitespace CampusCarpool.slnx --verify-no-changes
```

- Os testes "de integração" usam SQLite em memória; a maioria instancia os controllers diretamente e alguns usam `WebApplicationFactory`. Nenhum precisa de SQL Server, com **uma exceção**: `ConcorrenciaSqlServerIntegrationTests` (IT023) liga-se a `(localdb)\MSSQLLocalDB` e **requer SQL Server LocalDB instalado**. Sem LocalDB este teste falha ao criar a base de dados; os restantes não são afetados.
- Os testes **não** usam o Azure SQL nem a connection string da aplicação: correr `dotnet test` não toca na base de dados partilhada.
- O CI (`.github/workflows/backend-ci.yml`) executa: restore → build Release → `dotnet format whitespace --verify-no-changes` → testes com cobertura → mínimo de cobertura (10 %) → publish. Correr o `format` localmente antes de fazer push.

## 9. Ambiente público (Azure)

A publicação em si pertence à I063; aqui ficam apenas as configurações que o backend exige nesse ambiente. Instância publicada atualmente (Azure App Service, Germany West Central):

```text
https://campuscarpool-api-g15-ase2b6g6cbabf7d7.germanywestcentral-01.azurewebsites.net
```

`GET /health` → `Healthy` e `GET /` → `{"service":"Campus Carpool API","status":"running"}` (verificado em 02/11/2026). Se a I063 alterar nomes ou URLs, atualizar esta secção.

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

Encontrados ao preparar este manual; ficam registados para a equipa decidir (a correção está fora do âmbito da I064):

1. **Valores por defeito no `appsettings.json`.** O ficheiro versionado traz uma `Jwt:Key` de exemplo, a connection string LocalDB e uma `Maps:ApiKey`. Servem apenas para desenvolvimento local e **nunca** devem ser usados em ambientes partilhados ou públicos. Recomenda-se que a equipa os retire do ficheiro e que as chaves de serviços externos sejam regeneradas e geridas apenas por User Secrets/variáveis de ambiente.
2. **Partilha de credenciais.** Chaves e palavras-passe (Brevo, Azure SQL, contas de teste) não devem circular em canais de chat; usar um canal privado e, se já foram partilhadas, regenerá-las.
3. **README desatualizado.** `CampusCarpool/README.md` e `database/scripts/README.md` referem `ConnectionStrings__CampusCarpool` e uma pasta de migrations (`Persistence/Migrations`) que já não correspondem ao código (`DefaultConnection`, `Infrastructure/Migrations`).
4. **Chave `Frontend:ConfirmUrl` inconsistente** entre o `appsettings.json` (`/confirmar-email`) e o usado pela equipa (`/confirmar-conta`); deve coincidir com a rota do frontend.
5. **`/health` não verifica a base de dados.**
6. **Teste IT023** depende de LocalDB (secção 8).
7. **Avisos de vulnerabilidade NuGet** (`Microsoft.OpenApi` 2.0.0, `System.Security.Cryptography.Pkcs` 9.0.11) no `dotnet restore/build`.

## 11. Verificação dos procedimentos

Verificado em Windows 11, .NET SDK 10.0.403, a partir de um clone na branch `docs/i064-manual-instalacao-backend`:

| Procedimento | Resultado |
| --- | --- |
| `dotnet restore CampusCarpool.slnx` | OK |
| `dotnet build CampusCarpool.slnx` | OK — 0 erros, 283 avisos |
| `dotnet test CampusCarpool.slnx` | 346 passaram, **1 não passou** (total 347). O teste em causa é o IT023, que requer SQL Server LocalDB; a máquina de verificação não tem LocalDB (`sqllocaldb` indisponível) e o erro surgiu em `EnsureCreatedAsync`. Não foi possível confirmar o IT023 num ambiente com LocalDB |
| Arranque da API (`dotnet run --project src/CampusCarpool.Api`, perfil `http`, sem User Secrets) | Escuta em `http://localhost:5100`, ambiente `Development`; `GET /health` → `Healthy` (200) e `GET /` → `{"service":"Campus Carpool API","status":"running"}` (200). Arranca com os valores de `appsettings.json` |
| Instância pública (`GET /health` e `GET /`) | `Healthy` / `running` (200) em 02/11/2026 |
| Chaves de configuração da secção 6.2 | Confirmadas por leitura do código (`Program.cs`, `ServicoEmail`, `ServicoPush`, `MapasService`, `EstimativaCustoService`, `InfrastructureServiceRegistration`) |
| Ligação ao Azure SQL, `dotnet ef database update`, envio de email Brevo, FCM, publicação Azure | **Não executados** neste ambiente (exigem credenciais/recursos da equipa). A sintaxe segue a documentação oficial e deve ser validada pelo revisor com credenciais reais antes de fechar a issue |

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
| `429 Too Many Requests` | Limite global de 60 pedidos/minuto por IP | Aguardar um minuto |
| Teste IT023 falha | Sem SQL Server LocalDB (secção 8) | Instalar LocalDB ou ignorar esse teste localmente |

## 13. Referências

- [Instalar o .NET](https://learn.microsoft.com/dotnet/core/install/)
- [Configuração no ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)
- [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
- [Ferramentas do EF Core (`dotnet ef`)](https://learn.microsoft.com/ef/core/cli/dotnet)
- [Migrations do EF Core](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Azure SQL Database — regras de firewall](https://learn.microsoft.com/azure/azure-sql/database/firewall-configure)
- [Brevo — API de email transacional](https://developers.brevo.com/)
- [OpenRouteService](https://openrouteservice.org/dev/)
