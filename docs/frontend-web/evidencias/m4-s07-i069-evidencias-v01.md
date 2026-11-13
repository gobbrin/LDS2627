# I069 — Registo de implementação e validação

Grupo G15 · M4 · Sprint 07 · 2026-11-08.

## Fontes e versão de partida

- Repositório `gobbrin/LDS2627`, base `develop`, commit
  `069764e07b12301bc88f1a79ad6fbfe7a54ff56e`.
- Sprint Backlog S07 v01: I069, estrutura web/API, Executor Gil Gabriel da Cunha
  Leão; Revisora Cristiana Gabriela Oliveira Macedo; Auditor Vítor Samuel Vieira
  Lopes. Orçamento 50 pontos (30/15/5), prazo 2026-11-10 às 14:30.
- Critérios detalhados da linha I069 do Backlog de Projeto v07 incluído na M3.
  O S07 refere v08, cujo conteúdo detalhado não estava disponível. Confirmar essa
  linha antes da aceitação formal; não se presume que a v07 substitui a v08.
- Modelo de classes `m2-modelo-classes-frontend-web-v01.md` da M3 (conteúdo declara
  v04), arquitetura M3 v02, protótipo de baixa fidelidade M2 v01.
- Regulamento Interno v02, §§10–11: convenções Git, Angular/TypeScript,
  Jasmine/Karma, ESLint, Prettier e cobertura de linhas de 80%.
- Controllers, DTOs, CORS, middleware e manual do backend no commit de partida.

## Critérios e implementação

| Critério I069                                   | Evidência implementada                                                                                |
| ----------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| Compilar e arrancar com instruções documentadas | README, `npm ci`, `npm start`, `npm run build`, lockfile                                              |
| Endereço configurável sem alterar código        | `public/config.json`, carregado antes do bootstrap; também editável após build                        |
| Sessão mantida e terminada corretamente         | `SessaoService`, validação `/autenticacao/me`, expiração, logout remoto/local e testes                |
| Erros consistentes                              | Interceptor, normalização, notificação global e erros de campo                                        |
| Estrutura corresponde ao modelo                 | `core`, `shared`, `features`; serviços e guardas identificados no modelo; âmbito funcional delimitado |

## Adaptações explícitas ao contrato atual

1. O modelo antigo usa `/auth/*`; o código entregue utiliza as rotas reais
   `/api/v1/autenticacao/*`.
2. Login devolve apenas `accessToken` e `expiresIn`, sem `tokenType` ou utilizador.
   O ID é lido do JWT para estado local e confirmado por `/autenticacao/me`; o nome
   e os restantes dados vêm de `/utilizadores/me`.
3. O erro normal é `{ erro: string }`, além de `ProblemDetails`. O normalizador
   aceita também o formato antigo com campos, sem exigir que o backend o emita.
4. Acrescenta-se `/inicio` como destino autenticado, em linha com o protótipo.
   As outras rotas só são expostas quando o respetivo conteúdo existe.
5. O menu é uma base para as próximas funcionalidades; não se apresentam botões
   de viagens, chat ou notificações sem implementação. O perfil é apenas de leitura.
6. Não foram reescritos o modelo histórico, documentos assinados ou backend.

## Verificação

| Verificação                                                                                          | Resultado em 2026-11-08                                                                                                 |
| ---------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `npm run format:check`                                                                               | Passou                                                                                                                  |
| `npm run lint`                                                                                       | Passou, sem erros ou warnings do ESLint                                                                                 |
| `npm run build`                                                                                      | Passou; bundle inicial aproximadamente 279 kB, abaixo do limite de 500 kB                                               |
| `npm run test:dom`                                                                                   | 36 testes passaram; Angular AOT + Jasmine + jsdom, HTTP simulado                                                        |
| `npm test` (Karma/Chrome)                                                                            | Bloqueado pelo ambiente antes da execução: `socket() failed: Operation not permitted` ao iniciar Chrome                 |
| Cobertura de linhas no Karma                                                                         | Não medida; limiar de 80% configurado e ainda por validar                                                               |
| Inspeção visual em browser                                                                           | Não concluída: browser remoto sem acesso ao servidor local                                                              |
| API pública: `/health`                                                                               | Respondeu HTTP 200                                                                                                      |
| API pública: preflight `OPTIONS /api/v1/autenticacao/me`, origem `http://localhost:4200`, método GET | Respondeu HTTP 405, sem `Access-Control-Allow-Origin`; ligação direta a partir dessa origem não validada                |
| Sessão com conta real / revogação na API publicada                                                   | Não executada; conta confirmada não disponibilizada e CORS pendente                                                     |
| Workflow no GitHub                                                                                   | Preparado, mas não executado: publicação da branch bloqueada pela revisão automática por falta de autorização explícita |

Os 36 testes cobrem sessão persistida/expirada/corrompida, isolamento por backend,
armazenamento indisponível, envio restrito de token, 401 atual e atrasado, login,
logout normal e sem rede, erros, guardas, páginas, formulário e consulta de perfil.
O backend HTTP é simulado pelo Angular, sem contas, passwords ou tokens reais.

A evidência de `/health` não comprova que os frontends consigam usar a API. A
configuração de CORS no ambiente publicado deve ser corrigida pelo responsável do
backend/deployment e novamente verificada. A I069 não altera esse ambiente.
Não há assinatura nem decisão formal de revisão ou aceitação neste registo.

## Revisão e auditoria pendentes

Confirmar os critérios da v08/Issue, executar login com uma conta académica
confirmada contra o backend, recarregar `/perfil`, terminar sessão e verificar
que o token anterior é recusado. Verificar o build alojado com fallback de rotas,
CORS e `config.json`. Arquivar revisão e auditoria pelos responsáveis designados.
A validação assistida não substitui os papéis do grupo, nem autoriza merge.
