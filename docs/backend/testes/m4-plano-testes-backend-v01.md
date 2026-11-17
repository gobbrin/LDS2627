# Plano de Testes do Backend

Campus Carpool | Grupo 15 | m4, Sprint 08 | Issue I112

**Versão m4 v01** • 17/11/2026 • Estado: suite automatizada verde em SQLite e em SQL Server real, local e na CI; lacunas registadas na secção 7.

Este documento é a versão de m4 do plano de testes de m3 (`m3-s03-i031-plano-testes-backend.docx`, versão 2.1 de 13/10/2026, Issue I031), que fica como está. Mantém a estrutura e a numeração de m3 e acrescenta só o que mudou com a I112 (correção e melhoria do backend, branch `fix/i112-corrigir-melhorar-backend`). O que não é referido aqui continua válido tal como no plano de m3.

## 1. Objetivo e âmbito

Igual a m3: definir estratégia, casos de teste, dados, critérios de aceitação e evidências para validar o backend ASP.NET Core/.NET 10, EF Core/SQL Server e integrações externas do Campus Carpool.

**Acrescentado em m4:** o âmbito passa a incluir as alterações da I112: formato único de erro `{ erro }`, push de ponta a ponta, email de confirmação em segundo plano, conversa paginada por cursor, rota guardada e reutilizada, linha da rota pelo HERE Routing com retas como reserva, simulação da alteração de uma viagem (`POST /viagens/{id}/simular-alteracao`) e regras de arquitetura verificadas por testes. Continua fora do âmbito a validação visual dos clientes Angular e Kotlin (ver lacunas R2 e R3, secção 7).

## 2. Referências e pressupostos

- Plano de m3: `m3-s03-i031-plano-testes-backend.docx` (I031).
- Especificações e relatórios de m3 nesta pasta: I050 (testes unitários), I053 (testes de integração), I054 (evidências), I059 e I060 (relatórios), I061 (cobertura); coleções Postman em `I055/` e `postman/`.
- Guia do código: `docs/backend/arquitetura/GUIA-DO-CODIGO.md`. Estado da implementação: `docs/backend/IMPLEMENTACAO-STATUS.md`.
- Pontos do docente na reunião de feedback de m3 sobre testes: R-Q04 (testes de erro que só verificam o código HTTP) e R-Q06 (Postman sem resposta esperada).
- Integrações externas: Brevo (email; em m3 o plano falava de SMTP), OpenRouteService (geocodificação, matriz e distância), HERE Routing v8 (só a linha da rota, opcional), Firebase Cloud Messaging (push). Nos testes automatizados todas são substituídas por serviços falsos (`Integration/ServicosFalsos.cs`) ou por respostas HTTP controladas.

## 3. Ambiente, ferramentas e preparação

Atualizado em m4 (a máquina de desenvolvimento e o repositório mudaram desde m3):

- .NET SDK 10; solução `backend/CampusCarpool.slnx`; repositório no GitHub (a pipeline é GitHub Actions, `backend-ci.yml`, e já não GitLab CI).
- xUnit em `backend/tests/CampusCarpool.Tests` (`Unit/` e `Integration/`). Para testar o pipeline HTTP real usa-se `Integration/AplicacaoReal.cs`.
- Base de dados dos testes em dois modos (secção 4.1): SQLite em memória ou SQL Server 2022 real num contentor Docker (`backend/docker-compose.sqlserver.yml`).
- Configuração por User Secrets ou variáveis de ambiente (`Jwt__Key`, `ConnectionStrings__DefaultConnection`, `Maps:ApiKey`, `Here:ApiKey`, `Brevo:*`, `Firebase:*`). Nunca guardar segredos no repositório, que é público. A palavra-passe do SQL Server local é gerada pelo script em `backend/.env`, fora do git.
- Os dados de teste são criados por cada teste (utilizadores, viagens, pedidos) e a base é nova em cada execução; não há dados partilhados entre testes.

## 4. Estratégia de testes

A tabela de m3 mantém-se. Linhas atualizadas em m4:

| Nível | Objetivo | Ferramenta |
| --- | --- | --- |
| Unitário | Validadores, regras de estados, cálculo de custos e filtros; **fábricas e regras do Domain (I112)** | xUnit |
| Integração | Persistência EF Core, constraints, transações e serviços | xUnit + SQLite em memória **e SQL Server real (Docker)** |
| API/contrato | Rotas, DTOs, códigos HTTP, JWT e erros; **nomes dos campos das respostas e corpo `{ erro }`** | `AplicacaoReal` / WebApplicationFactory + Postman |
| Arquitetura (novo) | Dependências entre camadas e separação entre entidades e DTOs | xUnit com reflexão (`ArquiteturaTests`) |
| Segurança | Acesso por utilizador, token inválido, dados sensíveis e limites | Testes automatizados + Postman |
| Externo | Email, geocodificação, rotas, linha da rota e push; tempo esgotado e falhas | Serviços falsos e respostas HTTP controladas |

### 4.1 Modo duplo: SQLite e SQL Server (novo)

A mesma suite corre em dois modos:

| Modo | Como se corre | Para quê |
| --- | --- | --- |
| SQLite em memória | `dotnet test CampusCarpool.slnx` em `backend/` | Rápido (cerca de meio minuto); é o teste do dia a dia |
| SQL Server real | `bash scripts/testes-sqlserver.sh` (sobe o contentor, define `CAMPUSCARPOOL_TESTES_SQLSERVER` e `CAMPUSCARPOOL_TESTES_BD=SqlServer` e corre a suite); `--filter "…"` para um subconjunto; `bash scripts/parar-sqlserver.sh` para parar | O esquema vem das migrations e o comportamento é o da base de produção (tipos, traduções de consultas, concorrência); cerca de 22–25 min |

Há testes saltados de propósito, porque só fazem sentido num dos modos:

- **6 testes `[SqlServerFact]`** (`ConcorrenciaSqlServerIntegrationTests` ×1, `RegrasSqlServerTests` ×3, `MigracoesSqlServerTests` ×2) precisam de SQL Server: sem a variável `CAMPUSCARPOOL_TESTES_SQLSERVER` ficam saltados na execução em SQLite.
- **1 teste `[SoSqliteFact]`** (`ConclusaoViagensTests.I110_15_FalhaNumaViagem_NaoImpedeAsRestantes`) só faz sentido em SQLite e fica saltado em SQL Server.

A CI corre os dois modos: primeiro a suite com cobertura (com o SQL Server disponível, por isso os 6 testes de SQL Server correm também aí) e depois a mesma suite inteira em SQL Server real. Os dois modos juntos executam todos os testes.

**Fluxo de validação (decisão de 17/11):** alterar, teste rápido em SQLite, suite em SQL Server local antes de commitar, depois commit e push; a CI é a confirmação final.

### 4.2 Regras para escrever testes (novo)

- **Testes de erro verificam o corpo, não só o código HTTP (R-Q04).** Usar os ajudantes `RespostasDeErro.AssertErro` (resultado de controller) e `AssertErroAsync` (resposta HTTP), que verificam o código, que o corpo tem só o campo `erro` e a mensagem exata.
- **Não escrever testes dependentes da janela do limitador de pedidos** (100/min por utilizador ou IP; 10/min por IP na autenticação). Quando é preciso provocar um 429, os pedidos vão em simultâneo e conta-se pelo menos um 429, em vez de se contar em série dentro de um minuto.
- **Sem tempos fixos.** Não medir durações («responde em menos de 2 s») nem esperar um número fixo de segundos. Esperar por um evento (tarefa concluída, mensagem recebida) com o limite `AplicacaoReal.EsperaMaxima` (60 s), que só serve para o teste não ficar preso se algo falhar.
- Serviços externos sempre falsos ou controlados; nenhum teste chama Brevo, OpenRouteService, HERE ou FCM reais.

## 5. Matriz de casos de teste

A matriz de m3 (CT-001 a CT-038, I029 a I037) mantém-se. Na I112 mudou o resultado esperado de alguns casos:

| ID | Alteração em m4 |
| --- | --- |
| CT-007 | O registo responde 202 depois de gravar a conta; o email de confirmação (Brevo) sai em segundo plano e não prende a resposta |
| CT-011 | Email indisponível: o registo responde **202** (já não 503); a conta fica por confirmar e pode pedir-se o reenvio |
| CT-022, CT-025 e restantes 400 | O corpo do erro é `{ erro }` com uma mensagem por regra (ex.: «A hora de partida tem de vir em UTC (terminada em Z).»); um campo JSON desconhecido ou de tipo errado dá 400 «Pedido inválido: campo em falta, a mais ou com tipo errado.» |
| CT-017, CT-018 | 401 **sem corpo** (única exceção ao formato `{ erro }`) |
| CT-024 | As contas por confirmar não iniciam sessão (403 no login, CT-015), por isso não chegam a criar viagens; a verificação repetida nos serviços foi retirada |
| CT-030, CT-031 | Texto com mais de 200 caracteres dá 400; `POST /rotas/calcular` traz `geometria` e `geometriaAproximada` |
| CT-033 | Continua 502 (falha) ou 504 (tempo esgotado), com o limite de 10 s por operação e não por chamada |

### I112 (novo)

| ID / teste | Condição e execução | Resultado esperado | Testes automatizados |
| --- | --- | --- | --- |
| CT-039 Contrato de erro | Erros 400, 403, 404, 405, 409 (regra e concorrência), 415, 429 e 500 | Corpo só com `{ erro }` e a mensagem da regra; 401 sem corpo | `ContratoRespostasHttpTests` (`I112_06`, `I110_66`), `PipelineHttpTests` |
| CT-040 Contrato das respostas | Pedidos válidos a cada módulo | Nomes dos campos JSON fixos (camelCase) | `ContratoRespostasHttpTests` (`I110_63` a `I110_65`, `I112_01` a `I112_07`) |
| CT-041 Arquitetura | Reflexão sobre os assemblies | Domain sem dependências; Application só do Domain; Infrastructure não depende da Api; Api não usa o acesso a dados nem entidades; respostas sem tipos do Domain a qualquer profundidade; repositórios não devolvem DTOs | `Unit/ArquiteturaTests` |
| CT-042 Push de ponta a ponta | Pedido de participação com dispositivo do condutor registado; fila e trabalhador reais, FCM controlado | O FCM recebe o push com `tipo`, `notificacaoId` e `viagemId` no campo `data`; uma falha num envio não pára os seguintes; o trabalhador para com a aplicação | `PushPontaAPontaTests` (`I112_12`, `I112_13`, …) |
| CT-043 Email em segundo plano | Registo com o email retido; reenvio com texto que não é email | Registo responde 202 antes de o email sair e o email sai depois; reenvio dá 400 «Email inválido.» | `EmailEmSegundoPlanoTests` (`I112_26` a `I112_28`) |
| CT-044 Conversa paginada | `GET /mensagens/conversa/{id}?antesDe=&tamanho=` | Últimas `tamanho` (50 por omissão) por ordem cronológica; cursor sem repetir nem perder mensagens no mesmo instante; 400 para `tamanho` fora de 1–100 ou cursor de outra conversa; `ultimaMensagemId` na lista de conversas | `ConversaPaginadaTests` (`I112_29` a `I112_34`) |
| CT-045 Rota guardada | Aceitar, cancelar, simular e indisponibilizar sem paragens novas; editar só hora ou lugares | A rota guardada é reutilizada sem chamar os mapas; funciona com os mapas em baixo | `RotaGuardadaTests` (`I112_17`, `I112_19` a `I112_22`) |
| CT-046 Linha da rota (HERE com reserva) | HERE configurada, em erro ou ausente | Linha guardada em `RotaPontos` e reutilizada; sem HERE ou com erro, a viagem cria-se e a linha são retas (`geometriaAproximada`) | `GeometriaRotaTests` (`I112_02` a `I112_11`) |
| CT-047 Simular alteração | `POST /viagens/{id}/simular-alteracao` | 200 com a rota e o custo, sem gravar, notificar nem chamar a HERE; 403 não condutor; 404; 409 viagem concluída; 400 campo a mais; 401; 503 sem mapas | `SimularAlteracaoViagemTests`, `SimularAlteracaoViagemHttpTests` |
| CT-048 Fim de sessão no hub | Logout com ligação aberta a `/hubs/pedidos` | A ligação é fechada; religar com o token revogado dá 401 | `AutenticacaoRealHttpTests` (`I112_FimDeSessao_FechaLigacaoAoHub`) |
| CT-049 Proxy reverso | Pedidos com `X-Forwarded-For` de proxy configurado e desconhecido | Limite por IP real só com proxy configurado | `ProxyReversoTests` |
| CT-050 Temporizador da conclusão das viagens | Trabalhador real com intervalo de 1 s e serviço de conclusão falso; a primeira verificação falha | A falha fica no log e a verificação seguinte corre (com o «agora» em UTC); a paragem termina o trabalhador, antes ou depois da primeira verificação | `TrabalhadoresSegundoPlanoTests` (`I112_35`, `I112_36`) |
| CT-051 Trabalhador dos emails | Fila e trabalhador reais, serviço de email falso que falha um destino ou nunca acaba | Uma falha não impede o envio seguinte; parar a meio de um envio termina o trabalhador sem erro | `TrabalhadoresSegundoPlanoTests` (`I112_37`, `I112_38`) |
| CT-052 Linha guardada e comparação de pontos | `GeometriaRota` com pares e rotas dadas pelo teste | Pares voltam às mesmas coordenadas; par sem dois números dá erro; pontos em número, ordem ou sítio diferentes pedem outra linha | `Unit/GeometriaRotaUnitTests` (`I112_39` a `I112_42`) |
| CT-053 Conflito de concorrência na gravação | Dois contextos na mesma base: um apaga o registo que o outro altera | `ConfirmarAsync` e `TentarConfirmarAsync` lançam `RegraDeNegocioException` com a mensagem de conflito (409 na API) | `UnidadeDeTrabalhoEfCoreTests` (`I112_43`, `I112_44`) |

## 6. Critérios de entrada, saída e evidências

### Entrada

Igual a m3, com: o contentor de SQL Server sobe com `scripts/testes-sqlserver.sh` e as migrations aplicam-se na base de teste.

### Saída / aceitação

Igual a m3, com: a suite passa nos dois modos (SQLite e SQL Server real), sem testes falhados e só com os 7 saltados previstos na secção 4.1; cobertura de linhas sem migrations ≥ 80 % (barreira da CI); `dotnet format whitespace --verify-no-changes` sem alterações; CI verde.

### Registo de execução

| ID | Data | Executor | Resultado | Evidência / defeito |
| --- | --- | --- | --- | --- |
| Suite completa, SQLite (local) | 17/11/2026 | G15 (I112) | 771 aprovados, 0 falhados, 6 saltados (777) | Saltados: os 6 `[SqlServerFact]` |
| Suite completa, SQL Server (local, Docker) | 17/11/2026 | G15 (I112) | 776 aprovados, 0 falhados, 1 saltado (777) | Saltado: `I110_15` (`[SoSqliteFact]`) |
| Cobertura (local) | 17/11/2026 | G15 (I112) | Linhas 97,21 % (4055/4171); ramos 87,40 % (1173/1342) | `coverage.runsettings`, sem migrations |
| CI, commit `c7686c7` | 17/11/2026 | GitHub Actions, run 44081954165 | Verde (25m41s); suite com cobertura 777/777; SQL Server 776 aprovados, 1 saltado; cobertura 97,43 % | Barreira de 80 % passada |
| Cobertura dos trabalhadores em segundo plano (local, SQLite) | 17/11/2026 | G15 (I112) | 782 aprovados, 0 falhados, 6 saltados (788; 11 testes novos, CT-050 a CT-053). Linhas 98,13 % (4093/4171); ramos 87,93 % (1180/1342). Por classe, antes → depois: `TrabalhadorConclusaoViagens` 45,2 % → 96,8 %; `TrabalhadorEmails` 56,0 % → 100 %; `GeometriaRota` 78,1 % → 100 %; `UnidadeDeTrabalhoEfCore` 78,9 % → 100 %; `TrabalhadorNotificacoesPush` 89,7 % (sem alteração); `SegmentoRota` 66,7 % (sem alteração) | `coverage.runsettings`, sem migrations; testes novos corridos 10 vezes seguidas sem falhas; sem alterações ao código de produção |

## 7. Riscos e limitações

Os riscos de m3 mantêm-se, com estas atualizações:

- **Email:** já não é SMTP mas Brevo; o registo responde 202 e o email sai em segundo plano. As filas de email e de push são em memória: um reinício perde o que estiver por enviar.
- **Mapas:** além do OpenRouteService, a linha da rota usa o HERE Routing v8; sem HERE, a linha são retas e a viagem não falha. Os testes usam respostas controladas; o teste com o serviço HERE real depende da chave, fora do git.
- **Tempo e carga:** testes que dependiam da janela do limitador ou de tempos fixos falhavam na suite completa em SQL Server com a máquina carregada (R1, 8 testes). Corrigido a 17/11 no commit `c7686c7`, só nos testes; regras na secção 4.2.

**Lacunas registadas** (por fazer; dono e momento por indicar pelo Vítor):

| ID | Lacuna | Nota |
| --- | --- | --- |
| R2 | Frontend web: achados WEB-01 a WEB-17 (inclui testes que simulam respostas antigas do backend) | Depois de o backend estar completo |
| R3 | Frontend mobile: achados MOB-01 a MOB-17 (inclui testes sem o corpo completo dos pedidos) | Idem |
| R4 | Simulação completa em Docker (API + SQL Server + frontend web) | Adiada por decisão do Vítor |
| — | Paginação das avaliações recebidas (BE-14 c) e do histórico (AP-10, BE-27) e os respetivos testes | Registado na I112 |
| R-Q01, R-Q02 | Relatório de análise estática final (analisadores corridos de novo) | Etapa E5 |
| R-Q06 | Postman com a resposta esperada (código e corpo) e asserções em cada pedido | Etapa E5; as coleções atuais são de m3 e não têm o endpoint `simular-alteracao` |

## 8. Execução automatizada e entrega

Comandos atualizados (em `backend/`):

```bash
dotnet build CampusCarpool.slnx < /dev/null
dotnet test CampusCarpool.slnx < /dev/null                     # SQLite
bash scripts/testes-sqlserver.sh                               # SQL Server real em Docker
dotnet test CampusCarpool.slnx --settings coverage.runsettings --collect "XPlat Code Coverage"
dotnet format whitespace CampusCarpool.slnx --verify-no-changes
```

As migrations não são aplicadas no arranque nem há scripts SQL guardados (ver `backend/database/scripts/README.md`); os testes `MigracoesSqlServerTests` aplicam-nas numa base de teste. Os resultados de cada execução da CI (ficheiros TRX dos dois modos e relatório de cobertura) ficam como artefactos da execução no GitHub. Nunca guardar segredos nem dados reais de utilizadores.

## 9. Registo de execução automatizada — 17/11/2026

O registo de m3 (I031.trx, 20 testes, 13/10/2026) fica no plano de m3. Este é o registo da I112.

### 9.1 Síntese dos resultados

| Evidência | CI run 44081954165 (commit `c7686c7`) e execuções locais de 17/11 |
| --- | --- |
| Testes na suite | 777 (290 unitários e 487 de integração) |
| SQLite, local | 771 aprovados, 0 falhados, 6 saltados |
| SQL Server real, local e CI | 776 aprovados, 0 falhados, 1 saltado |
| CI, suite com cobertura (SQL Server disponível) | 777 aprovados, 0 falhados, 0 saltados |
| Cobertura de linhas, sem migrations | 97,21 % local (4055/4171), 97,43 % na CI; ramos 87,40 % local |
| Estado | Execução automatizada aprovada nos dois modos; lacunas na secção 7 |

Comparação com m3 (06/11, commit `0205ac1`): 674 testes, cobertura 95,73 %.

Depois desta síntese, os testes dos trabalhadores em segundo plano (CT-050 a CT-053) levaram a suite a 788 testes (295 unitários e 493 de integração) e a cobertura local a 98,13 %; o registo está na secção 6 e a confirmação da CI fica para o próximo envio.

### 9.2 Correspondência com os casos

Os 14 testes com código CT no nome (CT-008, 009, 010, 013, 016, 019, 020, 022, 026, 027, 030, 032, 034, 038) continuam na suite. Os casos novos CT-039 a CT-053 estão ligados aos testes na secção 5. Os testes de erro verificam o corpo com `AssertErro`/`AssertErroAsync` (R-Q04): 91 verificações com mensagem no commit do contrato de erro da I112, 100 utilizações dos ajudantes na suite a 17/11 (contagem por pesquisa no código).

### 9.3 Lista integral dos testes executados

Não se reproduz aqui (777 testes). A lista sai de `dotnet test CampusCarpool.slnx --list-tests` e os resultados de cada execução estão nos ficheiros TRX da CI.

### 9.4 Próximas validações e critérios de fecho

- Rever os testes dos frontends contra o contrato novo do backend (R2, R3).
- Relatório de análise estática final (R-Q01, R-Q02) e Postman com resposta esperada (R-Q06).
- Testes de paginação das avaliações e do histórico, quando a paginação for feita.
- Teste com o serviço HERE real, com a chave fora do git.
