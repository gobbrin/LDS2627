# Notas de release do backend

**Projeto:** G15 — CampusCarpool
**Issue:** I065 (atualizado a 06/11/2026 na preparação da entrega m3, I068)
**Executor:** Vítor Samuel Vieira Lopes · **Revisor:** Joana Francisca Pacheco Rodrigues · **Auditor:** Gil Gabriel da Cunha Leão

## 1. Identificação da versão

| Campo | Valor |
| --- | --- |
| Componente | Backend CampusCarpool (API .NET 10) |
| Versão | **v0.2.0** — release do backend entregue em m3 |
| Versão anterior | v0.1.0 (`develop`, commit `2df18e6`, 02/11/2026) |
| Branch de origem | `feat/29-i110-corrigir-os-defeitos-do-backend-identificados-na-revisão-m3` |
| Commit de referência | `0205ac1` da branch `develop`, que junta a I110 com os testes da I060 |
| Data de referência | 06/11/2026 |
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
| Pipeline GitHub Actions (06/11/2026, commit `0205ac1`, run 42202915885) | Todos os passos verdes. 674 testes: **674 aprovados, 0 falhados, 0 saltados**. Repetição em SQL Server real: 673 aprovados, 0 falhados, 1 saltado de propósito (`I110_15`) |
| Cobertura de linhas, sem migrations | **95,73 %** (3052/3188); ramos 87,71 % (857/977). Relatório em `03.01.04-cobertura-de-testes` |
| Máquina sem SQL Server | 668 aprovados, 0 falhados, 6 saltados (os testes específicos de SQL Server) |
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
| Consultas em falta | DEF-BE-005, 010, 011, 012, 016, 018 | `GET /viagens/minhas`, `/viagens/{id}/passageiros`, `/viagens/{id}/pedidos`, `/mensagens/conversas`, ligações externas no perfil; condutor na pesquisa e nos detalhes | `9ff3cd8`, `f9fff10`, `a6ac946`, `1d5f88c` | Registo de defeitos |
| Regras de negócio | DEF-BE-001, 002, 003, 007, 014, 015, 017, 019, 021, 022, 023, 030, 036, 037, 038, 039 | Palavra-passe P-08, domínios em configuração, origem e destino editáveis, mesmo local, rota guardada, histórico completo, pesquisa sem acentos | Ver o registo de defeitos | Registo de defeitos |
| Segurança e robustez | DEF-BE-013, 025, 032, 033, 034, 035 | Limites 100/10 por minuto, visibilidade das viagens (404), conflito de concorrência dá 409, índice das avaliações | `7d1af86`, `f937a29`, `8b61842` | Registo de defeitos |
| Reestruturação | — | Módulos por funcionalidade, `Resultado` em todos os serviços, entidades ricas, respostas com DTOs, teste de arquitetura | Vários | `ArquiteturaTests` |
| Pipeline com SQL Server real | DEF-BE-031 | Contentor na CI, migrations aplicadas e toda a suite repetida em SQL Server | `304462a`, `7e523ce` | Pipeline |

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

A lista completa, com severidade, requisito afetado e estado, está no registo de defeitos do backend (`03.01.07-defeitos`). Resumo do que continua aberto:

**Funcionalidade**

- O autor não consegue ler a sua avaliação (DEF-BE-041).
- A simulação não devolve coordenadas (DEF-BE-042), e um pedido sem novos pontos também exige o Serviço de Mapas (DEF-BE-043).
- Depois de o passageiro cancelar, o aviso continua nas notificações (DEF-BE-049).
- A notificação de nova mensagem não identifica o remetente (DEF-BE-053).
- Nenhuma resposta traz a geometria da rota para o mapa: por decidir antes do frontend.

**Mensagens e erros**

- Algumas mensagens de erro são genéricas (DEF-BE-044), e a resposta 415 sai em inglês e sem `erro` (DEF-BE-047).
- No reenvio da confirmação, um email malformado dá 202 (DEF-BE-045) e o tempo de resposta revela contas pendentes (DEF-BE-046).

**Esquema da base de dados**

- O esquema segue o modelo v06. Faltam 10 `CHECK`, 5 tamanhos e 6 índices da v07 (DEF-BE-052), e mantêm-se `Avisos.CondutorId` (DEF-BE-050) e `Mensagens.ViagemId` (DEF-BE-051).

**Operacionais**

- As migrations não são aplicadas no arranque: aplicar com `dotnet ef database update` ou com os scripts de `04.13`.
- O registo depende do email (Brevo): sem ele a conta fica criada, por confirmar, com resposta 503.
- A fila de push é em memória: um reinício pode perder o alerta, mas a notificação fica sempre na base de dados (decisão G17).
- A instância publicada no Azure é anterior à I110 e não faz parte desta versão.
- `CampusCarpool/README.md` e `database/scripts/README.md` referem `ConnectionStrings__CampusCarpool`. Vale o manual de instalação (`04.17`).

**Testes**

- O SQLite em memória usado no dia a dia não cobre o comportamento próprio do SQL Server; essa cobertura vem da segunda execução da pipeline.

## 7. Como atualizar para esta versão

1. Obter o código do commit `0205ac1` (ou a cópia em `04.12-backend/m3-v01`).
2. Seguir o manual de instalação: restaurar, compilar e configurar `Jwt:Key` (**obrigatória**: deixou de estar no `appsettings.json`), `ConnectionStrings:DefaultConnection` e integrações.
3. Aplicar as migrations à base de dados. Uma base criada com a v0.1.0 tem de receber as duas migrations novas (`I110_EstadoViagemConcluidaSemAcento` e `I110_ParticipacaoAceiteEm`); o script `03-migrar-de-m3-anterior-para-i110.sql` de `04.13` faz só isso.
4. Os tokens emitidos antes da mudança da `Jwt:Key` deixam de ser válidos: os utilizadores voltam a iniciar sessão.
