# I065 — Notas de release do backend

**Projeto:** G15 — CampusCarpool
**Issue:** I065 (atualizado a 04/11/2026 na preparação da entrega m3, I068)
**Executor:** Vítor Samuel Vieira Lopes · **Revisor:** Joana Francisca Pacheco Rodrigues · **Auditor:** Gil Gabriel da Cunha Leão

## 1. Identificação da versão

| Campo | Valor |
| --- | --- |
| Componente | Backend CampusCarpool (API .NET 10) |
| Versão | **v0.2.0** — release do backend entregue em m3 |
| Versão anterior | v0.1.0 (`develop`, commit `2df18e6`, 02/11/2026) |
| Branch de origem | `feat/29-i110-corrigir-os-defeitos-do-backend-identificados-na-revisão-m3` |
| Commit de referência | `ee689ee` — *ci(I110): GitHub Actions como pipeline oficial; retirar o GitLab CI (G13 / DC-28)* |
| Data de referência | 04/11/2026 |
| Código entregue | `04-artefactos-tecnicos/04.12-backend/m3-v01` |
| Scripts da base de dados | `04-artefactos-tecnicos/04.13-scripts-de-base-de-dados/m3-v01` |
| Plataforma | .NET 10 (SDK fixado em `global.json`: 10.0.100, `latestFeature`), ASP.NET Core, EF Core 10.0.12, SQL Server |
| Instalação e configuração | Manual de instalação, `04.17-manual-de-instalacao-e-configuracao/m3-manual-instalacao-configuracao-v01.pdf` |

> O repositório não tem *tags*, ficheiro de versão nem `<Version>` nos `.csproj`. A designação **v0.2.0** e o commit acima são a referência adotada para a entrega m3.

## 2. Resumo

Versão do backend com os fluxos principais de carpooling implementados e testados: autenticação com confirmação por email e fim de sessão, perfil próprio e perfil público, viagens com conclusão automática, pesquisa, pedidos de participação com atualização em tempo real, gestão de participações, avisos e notificações (incluindo push), mensagens, avaliações e histórico pessoal. Inclui a pipeline de CI no GitHub Actions com análise estática e quality gate de cobertura, testes unitários e de integração, a collection Postman e a análise estática.

Em relação à v0.1.0, corrige os defeitos de severidade alta encontrados na revisão do m3 (secção 5). Os defeitos que continuam abertos estão no registo de defeitos do backend (`03.01.07-defeitos/m3-registo-defeitos-backend-v01.xlsx`) e resumidos na secção 6.

## 3. Funcionalidades incluídas

| Área | Descrição | Endpoints principais (`/api/v1/…`) | Issues |
| --- | --- | --- | --- |
| Autenticação | Registo com validação de email académico, confirmação de conta por email, reenvio de confirmação, login JWT (60 min), **fim de sessão com revogação do token** | `autenticacao/registo`, `reenviar-confirmacao`, `confirmar`, `login`, `logout`, `me` | I110 |
| Utilizadores | Consulta e atualização do próprio perfil; **perfil público de outro utilizador com a classificação média** (P-10) | `utilizadores/me`, `utilizadores/{id}` | I110 |
| Viagens | Criação, edição, consulta e indisponibilização; **conclusão automática em segundo plano** (P-04) e conclusão manual auxiliar; controlo de concorrência otimista (coluna de versão) | `viagens`, `viagens/{id}`, `/indisponibilizar`, `/concluir` | I110 |
| Pesquisa | Pesquisa de viagens | `pesquisa/viagens` | — |
| Rotas e mapas | Geocodificação, cálculo de rota e ordenação de pontos (OpenRouteService); estimativa de custo e simulação | `rotas/geocodificar`, `rotas/calcular`, `rotas/viagens/{id}`, `viagens/{id}/simulacao` | I055 |
| Pedidos e participações | Pedido de participação, aceitar, rejeitar e cancelar, com consistência de lugares disponíveis em transação | `pedidos`, `participacoes/{id}/aceitar\|rejeitar\|cancelar` | I038, I041 |
| Tempo real | Atualização imediata da interface para pedidos via SignalR | hub `/hubs/pedidos` | I046 |
| Notificações push | Registo de dispositivos Android e envio de push para pedidos, com fila e trabalhador em segundo plano | `dispositivos` | I047 |
| Avisos e notificações | Aviso do condutor aos passageiros aceites (máx. 100 caracteres); notificações pessoais paginadas, marcar como lida | `viagens/{id}/avisos`, `notificacoes`, `notificacoes/{id}/ler`, `ler-todas` | I044 |
| Mensagens | Conversa entre dois utilizadores | `mensagens`, `mensagens/conversa/{id}` | — |
| Avaliações | Avaliação entre utilizadores por par | `avaliacoes`, `avaliacoes/utilizadores/{id}` | I048 |
| Histórico pessoal | Consulta do histórico de viagens do utilizador; as viagens indisponibilizadas continuam no histórico do passageiro aceite | `historico` | I049, I110 |
| Transversal | Rate limiting global (60 pedidos/min por IP, 429), tratamento global de exceções (ProblemDetails), `/health` | — | — |

### Base de dados

Oito migrations EF Core em `Infrastructure/Migrations` (as duas últimas são novas nesta versão):

1. `InitialCreate`
2. `I030_V2_Relacoes`
3. `I038_IndicePedidosAtivos`
4. `I044_AvisoConteudoMaxLength100`
5. `I048_AvaliacoesPorPar`
6. `AdicionarVersaoViagem`
7. `I110_EstadoViagemConcluidaSemAcento` — converte `Concluída` em `Concluida` e cria `CK_Viagens_Estado` (só `Ativa`, `Indisponibilizada`, `Concluida`)
8. `I110_ParticipacaoAceiteEm` — acrescenta `Participacoes.AceiteEm` e preenche-a nas participações já aceites

## 4. Qualidade e verificação

| Verificação | Resultado |
| --- | --- |
| `dotnet test` Release com `coverage.runsettings` (04/11/2026, commit `ee689ee`) | 376 testes: **375 aprovados, 0 falhados, 1 saltado** (IT023, sem SQL Server; ver secção 6) |
| Cobertura de linhas, sem migrations (04/11/2026) | **89,46 %** (2164/2419); ramos 82,64 % (695/841). Relatório em `03.01.04-cobertura-de-testes` |
| Pipeline CI (GitHub Actions) | Execuções verdes 41811030439 (`e8c73d5`) e 41820043932 (`977ffcd`, 375 + 1 saltado, 89,46 %). Evidências em `04.16-ci-cd/m3-relatorio-evidencias-pipeline-ci-cd-v01.md` |
| Passos da pipeline | restore → build Release → `dotnet format whitespace` e `style` (`--verify-no-changes`) → pacotes vulneráveis → testes com cobertura → **gate de 80 %** → publish |
| Testes de API (I055) | Collection e environment Postman em `03.01.05-postman` (sem credenciais) |
| Análise estática (I056) | Relatório em `03.01.06-analise-estatica` |
| Pacotes vulneráveis | Nenhum (`dotnet list package --vulnerable --include-transitive`) |

## 5. Alterações e correções incluídas

### 5.1 Desde a v0.1.0 (I110, revisão m3)

| Correção | Defeito | Detalhe | Commit | Teste de confirmação |
| --- | --- | --- | --- | --- |
| Segredos fora do `appsettings.json` | DEF-BE-026 (alta) | `Jwt:Key` e `Maps:ApiKey` retiradas; arranque recusado sem `Jwt:Key` ≥ 32 bytes; `Frontend:ConfirmUrl` passa a `/confirmar-conta`; secção `Email:*` morta retirada | `24ecf5b` | `ConfiguracaoSegurancaTests.I110_01`…`I110_04` |
| Credenciais retiradas do environment Postman | — (segurança) | Emails, palavras-passe e tokens esvaziados | `b69f968` | — |
| Formatação e LF | — | `.gitattributes`, `dotnet format whitespace` e `style` limpos | `36aa0fc`, `7ed6cf9` | Pipeline |
| Pipeline verde | DEF-BE-031 (parcial) | Pacotes vulneráveis atualizados, SDK fixado, CI com `format style`, auditoria de pacotes e cobertura sem migrations; IT023 com `[SqlServerFact]` | `7cf4954`, `e4b004f`, `e2b263a`, `e8c73d5` | Execução 41811030439 |
| Estado `Concluida` uniforme | DC-32 | Todo o código usa `nameof(EstadoViagem.X)`; migration de dados + `CHECK` | `f5599fb` | `EstadoViagemTests.I110_05`…`I110_07` |
| Indisponibilizar cancela pendentes e aceites | DEF-BE-008, DEF-BE-020 (alta) | Liberta os lugares, notifica aceites e pendentes; a viagem continua no histórico do passageiro aceite (`AceiteEm`) | `9a8619a` | `IndisponibilizacaoHistoricoTests.I110_08`…`I110_10` |
| Conclusão automática das viagens | DEF-BE-009 (alta) | `TrabalhadorConclusaoViagens` (`BackgroundService`, `ConclusaoViagens:IntervaloSegundos`, 60 s); cancela os pedidos pendentes da viagem concluída | `193c9c5` | `ConclusaoViagensTests.I110_11`…`I110_16` |
| Fim de sessão | DEF-BE-004 (alta) | `POST /api/v1/autenticacao/logout` guarda o `jti` em `TokensRevogados`; tokens revogados ou sem `jti` são recusados (também no hub) | `b594802` | `TerminarSessaoHttpTests.I110_17`…`I110_19` |
| Perfil público com média | DEF-BE-006 (alta) | `GET /api/v1/utilizadores/{id}`: só os campos de P-10 + classificação média e total de avaliações | `073aee2` | `PerfilPublicoTests.I110_20`…`I110_22` |
| Comentários no código | — | Comentários XML em português em todo o `src/`, com referência aos requisitos e aos defeitos conhecidos | `4f798b2`…`977ffcd` | — (só comentários) |
| Gate de cobertura | DEF-BE-029 | `MIN_COVERAGE` 10 → **80 %** de linhas sem migrations (decisão G14; RNF-MAN-01 v05) | `9a584f7` | Pipeline |
| Pipeline oficial | — | GitHub Actions; `.gitlab-ci.yml` retirado (decisão G13: GitLab da ESTG indisponível) | `ee689ee` | — |

### 5.2 Incluídas na v0.1.0

| Correção | Detalhe | Commit / PR |
| --- | --- | --- |
| Registo do `ServicoPush` | Passou a ser registado com `AddHttpClient` no `Program.cs`; removida a duplicação em `AddInfrastructureServices` | `9f1073c` |
| Integração de mapas (OpenRouteService) | Endpoints de geocodificação e direções corrigidos para `api.heigit.org`; chave sujeita a `Trim()` | `a899848` (I055) |
| Atualização em tempo real de pedidos | Ajustes ao `NotificadorPedidosSignalR` após revisão | `8a7b7b5` (I046) |
| Notificações push | Fila e trabalhador em segundo plano; falhas de push não interrompem a operação principal | `21b7a5a` (I047) |
| Testes unitários dos controllers | Cobertura alargada das validações | `2e60d8f` (I051) |
| Scripts de análise estática | Substituídos pelos validados na revisão | `fdcf62f` (I056) |
| Infraestrutura e testes de integração | Dependências e testes ajustados após a integração das branches | `9f1073c`, `0d2351a`, PR #13, PR #27 (I061) |

## 6. Problemas conhecidos e limitações

A lista completa, com severidade, requisito afetado e estado, está no registo de defeitos do backend (`03.01.07-defeitos`). Resumo dos defeitos abertos:

**Funcionalidades incompletas face à especificação**

- Ligações a perfis externos não implementadas (DEF-BE-005).
- Alterar origem e destino de uma viagem não é permitido (DEF-BE-007).
- Faltam consultas: viagens ativas do condutor (DEF-BE-010), passageiros de uma viagem (DEF-BE-012), pedidos pendentes de uma viagem (DEF-BE-016), lista de conversas (DEF-BE-018); a pesquisa e os detalhes não mostram nome e média do condutor (DEF-BE-011).
- O fim de sessão ainda não desativa o dispositivo push (DEF-BE-021).
- A rota ordena os pontos por heurística de distância, não pela menor duração (DEF-BE-022).
- Regras de validação por completar: palavra-passe (DEF-BE-001), pedido sem pontos (DEF-BE-014), mesmo local (DEF-BE-015, DEF-BE-030), aviso em viagem não ativa (DEF-BE-017).

**Documentação**

- A documentação da API entregue em m2 não corresponde à API implementada (DEF-BE-027).
- `CampusCarpool/README.md` e `database/scripts/README.md` referem `ConnectionStrings__CampusCarpool`; o código lê `ConnectionStrings:DefaultConnection`. `docs/IMPLEMENTACAO-STATUS.md` está desatualizado.

**Operacionais**

- As migrations **não são aplicadas no arranque nem na pipeline**: aplicar com `dotnet ef database update` ou com os scripts de `04.13`.
- O registo depende do email (Brevo): sem ele a conta fica criada mas por confirmar (resposta 503).
- `/health` não verifica a base de dados.
- Um conflito de concorrência ao editar, indisponibilizar ou concluir uma viagem devolve 500 em vez de 409.
- A publicação em ambiente público (I063) **não faz parte** desta versão.

**Testes**

- O IT023 (concorrência em SQL Server) só corre com `CAMPUSCARPOOL_TESTES_SQLSERVER` definida; a pipeline ainda não tem SQL Server, por isso fica saltado (DEF-BE-031, parcial).
- Os testes de integração usam SQLite em memória (`EnsureCreated`): as migrations e o comportamento específico do SQL Server não são exercitados. O índice único das avaliações difere entre uma base migrada e uma base criada pelos testes (DEF-BE-025).
- ~290 avisos de compilação (analisadores `CA…`), sem impacto no *build* (`TreatWarningsAsErrors` desligado).

## 7. Como atualizar para esta versão

1. Obter o código do commit `ee689ee` (ou a cópia em `04.12-backend/m3-v01`).
2. Seguir o manual de instalação: restaurar, compilar e configurar `Jwt:Key` (**obrigatória**: deixou de estar no `appsettings.json`), `ConnectionStrings:DefaultConnection` e integrações.
3. Aplicar as migrations à base de dados. Uma base criada com a v0.1.0 tem de receber as duas migrations novas (`I110_EstadoViagemConcluidaSemAcento` e `I110_ParticipacaoAceiteEm`); o script `03-migrar-de-m3-anterior-para-i110.sql` de `04.13` faz só isso.
4. Os tokens emitidos antes da mudança da `Jwt:Key` deixam de ser válidos: os utilizadores voltam a iniciar sessão.
