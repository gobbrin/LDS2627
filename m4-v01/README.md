# Campus Carpool — frontend web

Grupo G15 · LDS2627 · M4 / Sprint 07 · I069 · versão 0.1.0.

Base Angular/TypeScript com configuração em execução, cliente HTTP, sessão JWT,
interceptores, guardas, páginas carregadas de forma diferida e interface pt-PT.

## Instalar e executar

Pré-requisitos: Node.js 24.15 ou superior da série 24, npm 11 ou 12 e Chrome/Chromium
para os testes. As versões concretas das dependências estão no `package-lock.json`.

A partir da raiz do repositório:

```sh
cd projeto/04-artefactos-tecnicos/04.14-frontend-web/m4-v01
npm ci
npm start
```

Abrir `http://localhost:4200`. Para usar a API local, iniciar separadamente o backend
seguindo [o manual de instalação](../../../../CampusCarpool/docs/operacao/I064-MANUAL-INSTALACAO-BACKEND.md).
Não são incluídas credenciais nem contas de demonstração. É necessária uma conta
académica já confirmada para validar o percurso autenticado.

## Escolher o backend sem alterar código

Editar apenas `public/config.json`, antes ou depois da compilação:

```json
{ "apiBaseUrl": "http://localhost:5100/api/v1" }
```

Para o ambiente público documentado no backend:

```json
{
  "apiBaseUrl": "https://campuscarpool-api-g15-ase2b6g6cbabf7d7.germanywestcentral-01.azurewebsites.net/api/v1"
}
```

O ficheiro é carregado com `cache: no-store` antes do arranque. Depois de `npm run
build`, editar `dist/campus-carpool-web/browser/config.json` permite mudar o destino
sem recompilar. Recarregar a página após a alteração. A configuração é pública:
**nunca colocar chaves, passwords ou tokens neste ficheiro**.

O endereço tem de terminar em `/api/v1`. HTTPS é obrigatório, exceto para localhost,
127.0.0.1 e ::1. A API deve autorizar a origem exata do frontend em `Cors:Origens`
(por exemplo `http://localhost:4200`). A origem inclui esquema, host e porta.
O frontend não desativa CORS nem ignora certificados inválidos. Uma página HTTPS
não deve chamar uma API HTTP.

## Sessão e contratos utilizados

| Operação                   | Contrato atual do backend                                                               |
| -------------------------- | --------------------------------------------------------------------------------------- |
| Entrar                     | `POST /api/v1/autenticacao/login`, `{ email, password }` → `{ accessToken, expiresIn }` |
| Validar sessão             | `GET /api/v1/autenticacao/me` → `{ id }`                                                |
| Consultar o próprio perfil | `GET /api/v1/utilizadores/me`                                                           |
| Terminar sessão            | `POST /api/v1/autenticacao/logout`, sem corpo → `204`                                   |

O JWT fica em `sessionStorage`, separado por endereço de backend. A palavra-passe
não é persistida. A validade é limitada por `expiresIn` e pela claim `exp`; ler
claims no cliente não valida a assinatura nem substitui a autorização da API.
A sessão recupera após recarregar a página no mesmo separador. Ao arrancar com uma
sessão guardada, a aplicação consulta `/autenticacao/me` para validar a identidade.
Não existe refresh token no backend: após expirar é necessário entrar novamente.

O token só é enviado para a API configurada e nunca para URLs de terceiros ou
operações públicas de autenticação. Um `401` de um pedido autenticado da sessão
atual remove a sessão e reencaminha para `/entrar`. Erros de rede não são
interpretados como expiração. O logout pede primeiro a revogação ao servidor e
limpa sempre a sessão local; se o servidor não confirmar, a interface informa
explicitamente essa limitação. Fechar o separador elimina o armazenamento da
sessão, mas não constitui uma chamada de revogação ao servidor.

São normalizados `CorpoErro` (`{ erro: "mensagem" }`), `ProblemDetails` e o formato
histórico `{ erro: { mensagem, campos } }`. Os detalhes internos de erros `5xx` não
são apresentados. Os erros de campo são apresentados junto aos inputs.

## Estrutura e extensão nas próximas tarefas

| Pasta                           | Responsabilidade                                                                  |
| ------------------------------- | --------------------------------------------------------------------------------- |
| `src/app/core`                  | Configuração, `ApiClient`, `SessaoService`, interceptores, guardas e notificações |
| `src/app/shared`                | Modelos, erros de formulário e página 404                                         |
| `src/app/features/autenticacao` | Serviço de login/logout e `LoginPage`                                             |
| `src/app/features/inicio`       | `InicioPage` pública e autenticada                                                |
| `src/app/features/perfil`       | Serviço e consulta mínima de `PerfilPage`                                         |

`/` e `/entrar` são rotas de visitante. `/inicio` e `/perfil` exigem sessão. Rotas
inexistentes apresentam uma página 404. Os componentes são standalone; a divisão
por `core`, `shared` e `features` é a modularização funcional prevista no modelo.

A I070 pode ampliar os serviços e páginas existentes com registo, confirmação,
reenvio, edição de perfil e consulta de outros utilizadores. As I071–I076 acrescentam
as suas pastas em `features/` e rotas lazy com `autenticadoGuard`. As páginas devem
usar os serviços e `ApiClient`; parâmetros de pesquisa usam `HttpParams`.
Não adicionar os endereços da API diretamente aos componentes.

As funcionalidades de viagens, pesquisa, pedidos, mensagens, notificações,
avaliações, histórico e SignalR pertencem às tarefas funcionais seguintes e ainda
não são disponibilizadas nesta base. O menu apresenta apenas destinos existentes.
Não existem resultados fictícios apresentados como dados reais.

## Qualidade e testes

```sh
npm run format:check
npm run lint
npm test
npm run build
# Ou todos os passos:
npm run check
```

- Jasmine/Karma com ChromeHeadless, conforme o Regulamento Interno v02.
- Cobertura mínima de linhas: **80%**, bloqueante em `karma.conf.cjs`.
- Relatórios em `coverage/`, incluindo HTML e `lcov.info`.
- ESLint recomendado + TypeScript + Angular, e Prettier.
- Apenas bootstrap/configuração declarativa e interfaces sem código executável
  são excluídos da cobertura; `core/configuracao.ts`, que contém lógica, é testado.
- Os testes HTTP usam o backend simulado do Angular: não comprovam o funcionamento
  do ambiente público nem substituem a revisão contra a API real.

Se Chrome não for encontrado, definir `CHROME_BIN` com o caminho do executável.
Em contentores que não suportem sandbox, definir `KARMA_NO_SANDBOX=1` apenas no
ambiente de teste. Não é necessário numa instalação normal.

Quando um ambiente bloquear a abertura de Chrome, existe uma verificação
**suplementar** dos mesmos testes em Node.js/Jasmine com DOM simulado e compilação
Angular AOT:

```sh
npm run test:dom
```

Esse comando não executa Karma/Chrome, não mede cobertura nem comprova o layout.
Não substitui `npm test` nem os quality gates da pipeline.

O workflow [frontend-web-ci.yml](../../../../.github/workflows/frontend-web-ci.yml)
executa instalação pelo lockfile, formatação, lint, testes/cobertura e build, e
arquiva os relatórios e o build. Não publica automaticamente a aplicação.

## Build e alojamento

```sh
npm run build
```

Publicar o conteúdo de `dist/campus-carpool-web/browser/` num servidor de ficheiros
estáticos. Configurar fallback das rotas da aplicação para `index.html`, permitindo
recarregar diretamente `/entrar`, `/inicio` e `/perfil`. Preservar `config.json`
como JSON, servi-lo sem cache e configurar HTTPS e CORS antes da publicação.
A compilação usa a raiz `/`; alojamento sob um subcaminho requer um `base-href`
correspondente e uma configuração equivalente no servidor.

## Rastreabilidade e estado

Consultar [a evidência da I069](docs/m4-s07-i069-evidencias-v01.md) para fontes,
diferenças face ao modelo, verificações executadas e limitações. A implementação
não constitui aprovação do Revisor ou do Auditor nem encerra a Issue.
