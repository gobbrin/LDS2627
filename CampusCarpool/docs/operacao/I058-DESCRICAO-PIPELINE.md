# I058 — Descrição da pipeline CI/CD do backend

## Objetivo
A pipeline GitLab CI valida e empacota o backend .NET 10 através de restore,
build, testes e publish. Cada etapa só avança se a anterior terminar com
sucesso.

## Ficheiro de configuração
O ficheiro `.gitlab-ci.yml` está na raiz do projeto `CampusCarpool/` e usa:

- imagem Docker `mcr.microsoft.com/dotnet/sdk:10.0`;
- runner com a tag obrigatória `docker-runner-estg`;
- diretório de trabalho `CampusCarpool/`, definido no `before_script`.

## Etapas
1. **restore** — `dotnet restore CampusCarpool.slnx` descarrega dependências NuGet.
2. **build** — `dotnet build CampusCarpool.slnx --configuration Release` compila a solução.
3. **test** — `dotnet test CampusCarpool.slnx --configuration Release` executa testes.
4. **publish** — `dotnet publish src/CampusCarpool.Api/CampusCarpool.Api.csproj --configuration Release --output publish` produz o artefacto da API.

As etapas são executadas pela ordem declarada em `stages`:

```text
restore -> build -> test -> publish
```

## Execução

O ficheiro não define `workflow`, `rules`, `only` ou `except`. A pipeline segue
o comportamento padrão do GitLab para os commits processados pelo projeto.
Não deve ser assumido, apenas a partir deste ficheiro, que corre exclusivamente
em Merge Requests.

## Cache

O cache NuGet usa:

```text
NUGET_PACKAGES=$CI_PROJECT_DIR/.nuget/packages
cache key: nuget-$CI_PROJECT_NAME
```

Também são desativados a telemetria e o logótipo da CLI .NET através de
`DOTNET_CLI_TELEMETRY_OPTOUT` e `DOTNET_NOLOGO`.

## Runner

O runner deve usar Docker, ter a tag `docker-runner-estg` e conseguir aceder à
imagem .NET e à Internet para descarregar pacotes NuGet.
## Artefactos
O job `publish` conserva `CampusCarpool/publish/` como artefacto durante 7 dias.
O publish não faz deployment; apenas produz os ficheiros da API para download.
## Variáveis e segredos
Credenciais SQL Server, JWT, Brevo e OpenRouteService não devem ser guardadas
no repositório. Devem ser configuradas como variáveis protegidas do GitLab nos
ambientes que precisem delas.
## Validação
A partir da raiz do repositório:

```bash
cd CampusCarpool
dotnet restore CampusCarpool.slnx
dotnet build CampusCarpool.slnx --configuration Release --no-restore
dotnet test CampusCarpool.slnx --configuration Release --no-restore
```

No GitLab, abrir **Build > Pipelines**, selecionar a pipeline e verificar os
jobs `restore`, `build`, `test` e `publish`. Em caso de falha, abrir o job e
consultar o log a partir do primeiro erro:

- `restore`: dependências ou acesso ao NuGet;
- `build`: erros de compilação;
- `test`: testes falhados;
- `publish`: problemas de empacotamento.

Se o job `publish` passar, descarregar o artefacto `CampusCarpool/publish/`.

## Limites
A pipeline atual faz restore, build, testes e publish. Não executa:

- deployment para produção;
- aplicação de migrations;
- testes end-to-end;
- análise automática de vulnerabilidades;
- criação/publicação de imagem da aplicação.
