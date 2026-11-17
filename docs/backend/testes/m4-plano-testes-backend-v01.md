# Plano de Testes do Backend

Campus Carpool | Grupo 15 | m4, Sprint 08 | Issue I112

**Versão m4 v01** • 17/11/2026 • Estado: suite automatizada verde em SQLite e em SQL Server real, local e na CI; lacunas registadas na secção 7.

Este documento é a versão de m4 do plano de testes de m3 (`m3-s03-i031-plano-testes-backend.docx`, versão 2.1 de 13/10/2026, Issue I031), que fica como está. Mantém a estrutura e a numeração de m3 e acrescenta só o que mudou com a I112 (correção e melhoria do backend, branch `fix/i112-corrigir-melhorar-backend`). O que não é referido aqui continua válido tal como no plano de m3.

## 1. Objetivo e âmbito

Igual a m3: definir estratégia, casos de teste, dados, critérios de aceitação e evidências para validar o backend ASP.NET Core/.NET 10, EF Core/SQL Server e integrações externas do Campus Carpool.

**Acrescentado em m4:** o âmbito passa a incluir as alterações da I112: formato único de erro `{ erro }`, push de ponta a ponta, email de confirmação em segundo plano, conversa paginada por cursor, rota guardada e reutilizada, linha da rota pelo HERE Routing com retas como reserva, simulação da alteração de uma viagem (`POST /viagens/{id}/simular-alteracao`) e regras de arquitetura verificadas por testes. Continua fora do âmbito a validação visual dos clientes Angular e Kotlin (ver lacunas R2 e R3, secção 7).

## 2. Referências e pressupostos

- Plano de m3: `m3-s03-i031-plano-testes-backend.docx` (I031).
- Especificações e relatórios de m3 nesta pasta: I050 (testes unitários), I053 (testes de integração), I054 (evidências), I059 e I060 (relatórios), I061 (cobertura). Coleção Postman em vigor: `postman/CampusCarpool.postman_collection.json` (I112, todos os endpoints com a resposta esperada; ver `LEIA-ME.md`); as de `I055/` e as outras de `postman/` são de m3.
- Guia do código: `docs/backend/arquitetura/GUIA-DO-CODIGO.md`. Estado da implementação: `docs/backend/IMPLEMENTACAO-STATUS.md`.
- Pontos do docente na reunião de feedback de m3 sobre testes: R-Q04 (testes de erro que só verificam o código HTTP) e R-Q06 (Postman sem resposta esperada).
- Integrações externas: Brevo (email; em m3 o plano falava de SMTP), HERE (fornecedor principal: geocodificação, rota, matriz e linha da rota), OpenRouteService (reserva se a HERE falha ou não está configurada), Firebase Cloud Messaging (push). Nos testes automatizados todas são substituídas por serviços falsos (`Integration/ServicosFalsos.cs`) ou por respostas HTTP controladas.

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

- **8 testes `[SqlServerFact]`** (`ConcorrenciaSqlServerIntegrationTests` ×1, `RegrasSqlServerTests` ×3, `MigracoesSqlServerTests` ×4) precisam de SQL Server: sem a variável `CAMPUSCARPOOL_TESTES_SQLSERVER` ficam saltados na execução em SQLite.
- **1 teste `[SoSqliteFact]`** (`ConclusaoViagensTests.I110_15_FalhaNumaViagem_NaoImpedeAsRestantes`) só faz sentido em SQLite e fica saltado em SQL Server.

A CI corre os dois modos: primeiro a suite com cobertura (com o SQL Server disponível, por isso os 8 testes de SQL Server correm também aí) e depois a mesma suite inteira em SQL Server real. Os dois modos juntos executam todos os testes.

**Fluxo de validação (decisão de 17/11):** alterar, teste rápido em SQLite, suite em SQL Server local antes de commitar, depois commit e push; a CI é a confirmação final.

### 4.2 Regras para escrever testes (novo)

- **Testes de erro verificam o corpo, não só o código HTTP (R-Q04, BE-09).** Usar os ajudantes `RespostasDeErro.AssertErro` (resultado de controller) e `AssertErroAsync` (resposta HTTP), que verificam o código e a mensagem exata do campo `erro`; que o corpo tem só esse campo fixa-o `PipelineHttpTests` (`AssertSoCampoErroAsync`) e `ContratoRespostasHttpTests`. **Estado a 17/11 (BE-09 fechado):** todos os testes de erro com corpo leem a mensagem exata; o antigo `RespostaEsperada.Proibido` (só verificava o tipo do corpo) foi retirado. Ficam sem mensagem, de propósito: os 401 sem corpo (JwtBearer e `Unauthorized()` dos controllers sem identidade, 29 testes), os testes do limitador que contam 429 (4; a mensagem do 429 está fixada em `I110_140`), o `/health` 503 (corpo de texto do health check, não `{ erro }`) e os testes que põem um código de erro num serviço externo falso (HERE, OpenRouteService, FCM, Brevo; 17), onde o código não é resposta da API.
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
| CT-040 Contrato das respostas | Pedidos válidos a cada módulo | Nomes dos campos JSON fixos (camelCase) | `ContratoRespostasHttpTests` (`I110_63` a `I110_65`, e os `I112_01` a `I112_07` desta classe; inclui `rotaSimulada` e as listas paginadas) |
| CT-041 Arquitetura | Reflexão sobre os assemblies | Domain sem dependências; Application só do Domain; Infrastructure não depende da Api; Api não usa o acesso a dados nem entidades; respostas sem tipos do Domain a qualquer profundidade; repositórios não devolvem DTOs | `Unit/ArquiteturaTests` |
| CT-042 Push de ponta a ponta | Pedido de participação com dispositivo do condutor registado; fila e trabalhador reais, FCM controlado | O FCM recebe o push com `tipo`, `notificacaoId` e `viagemId` no campo `data`; uma falha num envio não pára os seguintes; o trabalhador para com a aplicação | `PushPontaAPontaTests` (`I112_12`, `I112_51` a `I112_53`; renumerados de `I112_13` a `I112_15`, BE-40) |
| CT-043 Email em segundo plano | Registo com o email retido; reenvio com texto que não é email | Registo responde 202 antes de o email sair e o email sai depois; reenvio dá 400 «Email inválido.» | `EmailEmSegundoPlanoTests` (`I112_26` a `I112_28`) |
| CT-044 Conversa paginada | `GET /mensagens/conversa/{id}?antesDe=&tamanho=` | Últimas `tamanho` (50 por omissão) por ordem cronológica; cursor sem repetir nem perder mensagens no mesmo instante; 400 para `tamanho` fora de 1–100 ou cursor de outra conversa; `ultimaMensagemId` na lista de conversas | `ConversaPaginadaTests` (`I112_29` a `I112_34`) |
| CT-045 Rota guardada | Aceitar, cancelar, simular e indisponibilizar sem paragens novas; editar só hora ou lugares | A rota guardada é reutilizada sem chamar os mapas; funciona com os mapas em baixo | `RotaGuardadaTests` (`I112_17`, `I112_19` a `I112_22`) |
| CT-046 Linha da rota (HERE com reserva) | HERE configurada, em erro ou ausente | Linha guardada em `RotaPontos` e reutilizada; sem HERE ou com erro, a viagem cria-se e a linha são retas (`geometriaAproximada`) | `GeometriaRotaTests` (`I112_45` a `I112_50`, renumerados de `I112_02` a `I112_07` por BE-40; `I112_08`, `I112_09`, `I112_10`, `I112_11`) |
| CT-047 Simular alteração | `POST /viagens/{id}/simular-alteracao` | 200 com a rota, o custo e a linha da HERE (D-2), sem gravar nem notificar; sem HERE, retas; o `PUT` seguinte reutiliza a rota simulada; 403 não condutor; 404; 409 viagem concluída; 400 campo a mais; 401; 503 sem mapas | `SimularAlteracaoViagemTests` (incl. `I112_56`, `I112_57`), `SimularAlteracaoViagemHttpTests` |
| CT-048 Fim de sessão no hub | Logout com ligação aberta a `/hubs/pedidos` | A ligação é fechada; religar com o token revogado dá 401 | `AutenticacaoRealHttpTests` (`I112_54_FimDeSessao_FechaLigacaoAoHub`, BE-40) |
| CT-049 Proxy reverso | Pedidos com `X-Forwarded-For` de proxy configurado e desconhecido | Limite por IP real só com proxy configurado | `ProxyReversoTests` |
| CT-050 Temporizador da conclusão das viagens | Trabalhador real com intervalo de 1 s e serviço de conclusão falso; a primeira verificação falha | A falha fica no log e a verificação seguinte corre (com o «agora» em UTC); a paragem termina o trabalhador, antes ou depois da primeira verificação | `TrabalhadoresSegundoPlanoTests` (`I112_35`, `I112_36`) |
| CT-051 Trabalhador dos emails | Fila e trabalhador reais, serviço de email falso que falha um destino ou nunca acaba | Uma falha não impede o envio seguinte; parar a meio de um envio termina o trabalhador sem erro | `TrabalhadoresSegundoPlanoTests` (`I112_37`, `I112_38`) |
| CT-052 Linha guardada e comparação de pontos | `GeometriaRota` com pares e rotas dadas pelo teste | Pares voltam às mesmas coordenadas; par sem dois números dá erro; pontos em número, ordem ou sítio diferentes pedem outra linha | `Unit/GeometriaRotaUnitTests` (`I112_39` a `I112_42`) |
| CT-053 Conflito de concorrência na gravação | Dois contextos na mesma base: um apaga o registo que o outro altera | `ConfirmarAsync` e `TentarConfirmarAsync` lançam `RegraDeNegocioException` com a mensagem de conflito (409 na API) | `UnidadeDeTrabalhoEfCoreTests` (`I112_43`, `I112_44`) |
| CT-054 Fila de emails cheia (BE-39) | Fila real com capacidade 1 e trabalhador parado | Enfileirar o segundo email não espera; o primeiro continua na fila | `TrabalhadoresSegundoPlanoTests` (`I112_55`) |
| CT-055 Simulação do pedido com mapa (BE-35, D-2) | Simular um pedido com paragem nova e aceitar a seguir, com a mesma memória | `rotaSimulada` com os pontos (Porto, Maia, Braga), a linha da HERE com a Maia como `via` e `geometriaAproximada` falso; o aceitar não pede outra matriz nem outra linha e guarda a rota simulada | `GeometriaRotaTests` (`I112_09`, comportamento mudado pela D-2), `ContratoRespostasHttpTests` (`I112_03`) |
| CT-056 Linha fora do limite e chamadas desnecessárias (BE-36, BE-38 b, c, d) | Rota por recalcular; simular sem paragens com a rota a `null`; editar só a capitalização com os mapas em falha; consultar uma viagem indisponibilizada com paragens | A linha recebe o cancelamento do pedido (não o limite); uma só matriz; 204 sem chamadas e coordenadas iguais; 422 na rota e 200 com o motivo nos detalhes, sem chamadas | `RotaGuardadaTests` (`I112_58` a `I112_61`) |
| CT-057 HERE como Serviço de Mapas (D-5) | `HereMapasFalsa` (HttpMessageHandler) | Geocodificação lê `items[0].position` e envia `in=countryCode:PRT`; lista vazia dá `null`; rota direta com km, minutos e linha; matriz lida na posição n × i + j com `errorCodes` ≠ 0 a `null`; erro HTTP sai como `HttpRequestException` | `Unit/ServicoMapasHereTests` (`I112_62` a `I112_66`) |
| CT-058 HERE principal e OpenRouteService reserva (D-5, D-3, BE-38 a) | `ServicoMapasComReserva` real com as duas falsas | HERE a responder: a reserva não é chamada; HERE com 429, 5xx ou 401, ou sem resposta: o valor vem da reserva; as duas em falha: 502; sem chave HERE: só a reserva; o mesmo texto geocodifica-se uma vez; pré-visualizar e criar geocodifica cada local uma só vez e guarda a linha da rota direta | `MapasHereComReservaTests` (`I112_67` a `I112_73`), `Unit/TempoLimiteClientesHttpTests` (`I112_79`) |
| CT-059 Listas paginadas e passageiros (D-4, BE-41, BE-37) | `/viagens/minhas`, `/historico`, `/mensagens/conversas` com `pagina` e `tamanho`; paginação inválida; `/passageiros` visto por um passageiro e pelo condutor | Página certa com `total` (e `totais` no histórico); 400 com a mensagem da regra; o passageiro não recebe os ids das participações, o condutor recebe | `ListasPaginadasHttpTests` (`I112_74` a `I112_78`) |
| CT-060 Identificadores únicos (BE-40, R-Q04) | Reflexão sobre os métodos de teste | Nenhum `I<issue>_<n>` aparece em dois testes | `Unit/IdentificadoresDosTestesTests` (`I112_80`) |
| CT-061 Simulação do pedido pelo passageiro (ecrã 14, P-13) | Viagem Porto → Braga com um aceite na Maia; outro utilizador simula `POST /viagens/{id}/simular-pedido` com recolha em Famalicão | 200; `rotaSimulada` com Porto, Famalicão e Braga, dois troços e as retas só entre esses pontos; o corpo não tem o texto, as coordenadas nem o id do aceite; os totais são os da rota completa; campos da resposta fixos | `SimularPedidoPassageiroHttpTests` (`I112_81`) |
| CT-062 Regras e quota da simulação do passageiro | Condutor simula; recolha igual à origem (sem pontos novos); viagem cheia; pedido pendente repetido (RF-F004-06); HERE com 503 e OpenRouteService como reserva | 403, 200 sem chamadas aos mapas nem à HERE, 409 e 409 com `{ erro }` e sem chamadas aos mapas; com a HERE em baixo, 200 com uma geocodificação e uma matriz na reserva | `SimularPedidoPassageiroHttpTests` (`I112_82` a `I112_85`), `SimularPedidoPassageiroReservaHttpTests` (`I112_88`) |
| CT-063 A simulação do passageiro não grava (RF-F008-06) | Contar participações e notificações e ler a viagem antes e depois de simular | Tudo igual: nenhuma participação, notificação, rota, lugar, distância ou duração mudou | `SimularPedidoPassageiroHttpTests` (`I112_86`) |
| CT-064 Pontos na rota da viagem (D-1, P-13, RF-F008-09) | Dois aceites (Maia, Trofa) e um pendente leem `GET /rotas/viagens/{id}` | O aceite vê as duas paragens (texto e coordenadas); o pendente vê só a origem e o destino, sem o texto nem as coordenadas das paragens | `SimularPedidoPassageiroHttpTests` (`I112_87`) |
| CT-065 Chamadas aos mapas por operação (R-B01) | Rota guardada Porto → Braga: simular um pedido com paragens; rejeitar com os mapas em falha; novo pedido só com a recolha nova | Uma matriz e nenhuma geocodificação; nenhum pedido aos mapas e a rota igual; uma geocodificação e nenhuma matriz | `RotaGuardadaTests` (`I112_90`, `I112_92`, `I112_93`) |
| CT-066 Simulação com rota sem distância | A matriz devolve 0 m entre os pontos | 422 `{ erro: "Não foi possível calcular a rota." }` | `RotaGuardadaTests` (`I112_91`) |
| CT-067 Notificações sem os pontos do passageiro (RF-F006-14, P-13) | Pedido com recolha e destino novos, aceite pelo condutor | Nenhuma notificação tem o texto da recolha nem do destino | `RotaGuardadaTests` (`I112_94`) |
| CT-068 Token FCM inválido no repositório real | Dois dispositivos do mesmo utilizador; desativar um por token e um token desconhecido | Só o outro fica ativo; o registo fica na BD, inativo; o token desconhecido não dá erro | `DispositivosAtivosIntegrationTests` (`I112_89`) |
| CT-069 Pendente vê a linha real sem paragens (SP-01, D-SP-2) | Viagem com um aceite na Maia; outro utilizador com pedido pendente lê `GET /rotas/viagens/{id}` | Linha da HERE igual à guardada (`geometriaAproximada: false`); pontos só Porto e Braga | `GeometriaRotaTests` (`I112_95`) |
| CT-070 Classificação só com média e contador (BE-43, RF-F007-08 a 10) | Avaliação dada a um condutor; `GET /avaliacoes/utilizadores/{id}`; conta confirmada sem avaliações; conta por confirmar | `{ classificacaoMedia, totalAvaliacoes }` e nada mais; 0 e `null` sem avaliações; 404 por confirmar | `ContratoRespostasHttpTests` (`I112_05`), `AvaliacoesHttpIntegrationTests`, `AvaliacaoServiceTests` |
| CT-071 Restrições do modelo v07 nos dois motores (BE-33) | Pedido pendente só com recolha; SQL que estraga uma regra de cada vez (nome em branco, pedido sem pontos, ponto sem coordenadas, aceite sem data, URL `ftp`, aviso em branco); depois uma alteração válida | Cada SQL recusado pela restrição com esse nome; a alteração válida grava | `RestricoesBaseDeDadosTests` (`I112_96` a `I112_98`) |
| CT-072 Migration v07 em SQL Server (BE-33) | BD nova com as migrations: coordenadas fora de ±90/±180, distância 0, duração negativa, nome em branco, rota sem JSON; índices pelo catálogo. BD com dados do m3 (aceite sem data, pedido sem pontos) migrada | Recusas pelo nome; os 5 índices com as colunas, a ordem descendente e as incluídas; a migration passa, o aceite fica com a data do pedido e o pedido antigo pode ser cancelado | `MigracoesSqlServerTests` (`I112_99`, `I112_100`) |
| CT-073 Correções sem resposta própria (BE-19, BE-21 a BE-24) | Ler `.editorconfig`, `CodeMetricsConfig.txt`, `Directory.Build.props`, `appsettings.json`; reflexão nos repositórios e entidades; login de conta por confirmar | Regras CA1502 (≤ 15), CA1505, CA1506, IDE0051/52/60; sem `ListarPorPassageiroAsync`, `ListarRecebidasAsync`, `Mensagem.ViagemId`, `Aviso.CondutorId`, `Maps:Provider`/`BaseUrl`; login 403 e só uma conta confirmada cria viagens | `CorrecoesDeConfiguracaoECodigoTests` (`I112_101` a `I112_104`) |
| CT-074 Casos de erro com cobertura em falta (17/11) | HERE: `routes` vazio, sem `Here:ApiKey`, matriz com um ponto; simulação sem `pedidoId`; indisponibilizar/concluir viagem inexistente, já indisponível ou de outro condutor | Rota `null`; `InvalidOperationException` sem pedido à rede; `ArgumentException` sem pedido; 400 «Indica um pedidoId válido.»; 404, 409 e 403 sem mudar a viagem | `ServicoMapasHereTests` (`I112_105` a `I112_107`), `SimulacoesIntegrationTests` (`I112_108`), `CicloDeVidaDaViagemTests` (`I112_109` a `I112_111`) |

## 6. Critérios de entrada, saída e evidências

### Entrada

Igual a m3, com: o contentor de SQL Server sobe com `scripts/testes-sqlserver.sh` e as migrations aplicam-se na base de teste.

### Saída / aceitação

Igual a m3, com: a suite passa nos dois modos (SQLite e SQL Server real), sem testes falhados e só com os 9 saltados previstos na secção 4.1; cobertura de linhas sem migrations ≥ 80 % (barreira da CI); `dotnet format whitespace --verify-no-changes` sem alterações; CI verde.

### Registo de execução

| ID | Data | Executor | Resultado | Evidência / defeito |
| --- | --- | --- | --- | --- |
| Suite completa, SQLite (local) | 17/11/2026 | G15 (I112) | 771 aprovados, 0 falhados, 6 saltados (777) | Saltados: os 6 `[SqlServerFact]` |
| Suite completa, SQL Server (local, Docker) | 17/11/2026 | G15 (I112) | 776 aprovados, 0 falhados, 1 saltado (777) | Saltado: `I110_15` (`[SoSqliteFact]`) |
| Cobertura (local) | 17/11/2026 | G15 (I112) | Linhas 97,21 % (4055/4171); ramos 87,40 % (1173/1342) | `coverage.runsettings`, sem migrations |
| CI, commit `c7686c7` | 17/11/2026 | GitHub Actions, run 44081954165 | Verde (25m41s); suite com cobertura 777/777; SQL Server 776 aprovados, 1 saltado; cobertura 97,43 % | Barreira de 80 % passada |
| Cobertura dos trabalhadores em segundo plano (local, SQLite) | 17/11/2026 | G15 (I112) | 782 aprovados, 0 falhados, 6 saltados (788; 11 testes novos, CT-050 a CT-053). Linhas 98,13 % (4093/4171); ramos 87,93 % (1180/1342). Por classe, antes → depois: `TrabalhadorConclusaoViagens` 45,2 % → 96,8 %; `TrabalhadorEmails` 56,0 % → 100 %; `GeometriaRota` 78,1 % → 100 %; `UnidadeDeTrabalhoEfCore` 78,9 % → 100 %; `TrabalhadorNotificacoesPush` 89,7 % (sem alteração); `SegmentoRota` 66,7 % (sem alteração) | `coverage.runsettings`, sem migrations; testes novos corridos 10 vezes seguidas sem falhas; sem alterações ao código de produção |
| Fase R, BE-43, BE-33 e testes de BE-19/21/23/24 (local, SQLite) | 17/11/2026 | G15 (I112) | 848 aprovados, 0 falhados, 8 saltados (856). Linhas 98,35 % (4530/4606); ramos 88,75 % (1255/1414). SQL Server, subconjunto (migrations, restrições e os ficheiros com dados de teste alterados): 175 aprovados, 1 saltado | `coverage.runsettings`, sem migrations; tabela por classe abaixo |
| BE-09: testes de erro leem a mensagem `{ erro }` (local, SQLite) | 17/11/2026 | G15 (I112) | 848 aprovados, 0 falhados, 8 saltados (856, a mesma lista de testes; 30 testes passaram a verificar a mensagem exata) | Só testes alterados; regra na secção 4.2 |
| Análise estática final (E5, R-Q01, R-Q02) | 17/11/2026 | G15 (I112) | Build `--no-incremental` com as regras da CI: 0 erros, 0 avisos; CA1502 ≥ 8: 18 métodos, máximo 13; CA1506 ≥ 30: 18, máximo 55 (`ViagemService`); `dotnet format whitespace` e `style` limpos; `dotnet list package --vulnerable --include-transitive`: nenhum | Relatório local `14-analise-estatica-e-classes-grandes.md` §6 |
| Postman, fluxo completo (E5, R-Q06) | 17/11/2026 | G15 (I112) | 90 pedidos; corridos 88 (2 saltados: confirmação por token do email), 168 testes, 0 falhas | API local em SQL Server (Docker), sem chaves de mapas nem Brevo: contas confirmadas e viagens gravadas na BD; os pedidos de mapas responderam 503 `{ erro }`, a resposta esperada sem chave. Com a chave HERE: por correr |

### Cobertura por classe (17/11/2026, noite)

Classes importantes: serviços da Application, entidades e regras do Domain, controllers, trabalhadores, Serviço de Mapas (HERE, reserva, OpenRouteService) e push. Medição local em SQLite com `coverage.runsettings` (sem migrations), depois dos testes novos; as linhas de classes geradas pelo compilador (métodos `async`, lambdas) contam na classe de topo. Critério pedido: pelo menos 90 % de linhas e 80 % de ramos.

| Camada | Classe | Linhas | Ramos | Nota |
| --- | --- | --- | --- | --- |
| Api | `AutenticacaoController` | 100,0 % (36/36) | 83,3 % (10/12) |  |
| Api | `AvaliacoesController` | 100,0 % (23/23) | 50,0 % (7/14) | abaixo: ramos |
| Api | `AvisosController` | 100,0 % (13/13) | 50,0 % (5/10) | abaixo: ramos |
| Api | `DispositivosController` | 100,0 % (10/10) | 100,0 % (8/8) |  |
| Api | `GestaoParticipacoesController` | 100,0 % (13/13) | 50,0 % (6/12) | abaixo: ramos |
| Api | `HistoricoController` | 100,0 % (10/10) | 100,0 % (6/6) |  |
| Api | `MensagensController` | 100,0 % (23/23) | 87,5 % (14/16) |  |
| Api | `NotificacoesController` | 100,0 % (16/16) | 85,7 % (12/14) |  |
| Api | `PedidosController` | 100,0 % (12/12) | 100,0 % (4/4) |  |
| Api | `PedidosHub` | 100,0 % (9/9) | sem ramos |  |
| Api | `PesquisaController` | 100,0 % (7/7) | 100,0 % (2/2) |  |
| Api | `RotasController` | 100,0 % (25/25) | 100,0 % (16/16) |  |
| Api | `SimulacoesController` | 100,0 % (17/17) | 75,0 % (12/16) | abaixo: ramos |
| Api | `UtilizadoresController` | 91,7 % (33/36) | 72,2 % (13/18) | abaixo: ramos |
| Api | `ViagensController` | 100,0 % (48/48) | 58,8 % (20/34) | abaixo: ramos |
| Application | `AlteracaoDeViagem` | 94,8 % (55/58) | 85,7 % (24/28) |  |
| Application | `AutenticacaoService` | 97,6 % (123/126) | 93,5 % (58/62) |  |
| Application | `AvaliacaoService` | 100,0 % (48/48) | 100,0 % (34/34) |  |
| Application | `AvisoService` | 100,0 % (38/38) | 93,8 % (15/16) |  |
| Application | `CicloDeVidaDaViagemService` | 100,0 % (38/38) | 100,0 % (18/18) |  |
| Application | `ConclusaoViagensService` | 97,7 % (43/44) | 83,3 % (10/12) |  |
| Application | `ConsultaRotasService` | 95,7 % (88/92) | 82,1 % (23/28) |  |
| Application | `ConsultaViagensService` | 100,0 % (19/19) | sem ramos |  |
| Application | `DispositivoService` | 100,0 % (13/13) | 100,0 % (6/6) |  |
| Application | `EstimativaCustoService` | 100,0 % (34/34) | 100,0 % (10/10) |  |
| Application | `HistoricoService` | 100,0 % (63/63) | sem ramos |  |
| Application | `MensagemService` | 95,4 % (62/65) | 84,4 % (27/32) |  |
| Application | `NotificacaoService` | 96,6 % (86/89) | 83,3 % (10/12) |  |
| Application | `ParticipacaoService` | 95,7 % (155/162) | 85,7 % (48/56) |  |
| Application | `PerfilService` | 97,9 % (92/94) | 84,0 % (42/50) |  |
| Application | `PesquisaService` | 100,0 % (19/19) | sem ramos |  |
| Application | `PreparacaoDePedido` | 93,9 % (108/115) | 91,2 % (73/80) |  |
| Application | `RotaGuardada` | 98,8 % (82/83) | 87,5 % (21/24) |  |
| Application | `RotaService` | 98,7 % (153/155) | 82,7 % (43/52) |  |
| Application | `SimulacaoDeAlteracaoService` | 96,8 % (30/31) | 81,8 % (18/22) |  |
| Application | `SimulacaoDePedidoService` | 96,3 % (158/164) | 87,8 % (65/74) |  |
| Application | `ViagemService` | 99,5 % (183/184) | 93,9 % (77/82) |  |
| Domain | `Avaliacao` | 100,0 % (17/17) | 100,0 % (4/4) |  |
| Domain | `Aviso` | 100,0 % (4/4) | sem ramos |  |
| Domain | `CalculadoraDeClassificacao` | 100,0 % (6/6) | 100,0 % (2/2) |  |
| Domain | `CalculadoraDeCusto` | 100,0 % (6/6) | sem ramos |  |
| Domain | `ConfirmacaoConta` | 100,0 % (12/12) | 100,0 % (4/4) |  |
| Domain | `Dispositivo` | 100,0 % (4/4) | sem ramos |  |
| Domain | `HoraDePortugal` | 100,0 % (9/9) | sem ramos |  |
| Domain | `LigacaoExterna` | 100,0 % (5/5) | sem ramos |  |
| Domain | `Mensagem` | 100,0 % (19/19) | 80,0 % (8/10) |  |
| Domain | `Notificacao` | 100,0 % (10/10) | 100,0 % (4/4) |  |
| Domain | `OrdenadorDePontos` | 100,0 % (102/102) | 95,8 % (46/48) |  |
| Domain | `Participacao` | 98,7 % (76/77) | 97,4 % (37/38) |  |
| Domain | `PoliticaDeVisibilidade` | 100,0 % (8/8) | 75,0 % (3/4) | abaixo: ramos |
| Domain | `RegraMesmoLocal` | 100,0 % (3/3) | 100,0 % (2/2) |  |
| Domain | `TextoPesquisa` | 100,0 % (10/10) | 100,0 % (4/4) |  |
| Domain | `Utilizador` | 100,0 % (15/15) | sem ramos |  |
| Domain | `Viagem` | 100,0 % (98/98) | 100,0 % (28/28) |  |
| Infrastructure | `FilaEmails` | 100,0 % (20/20) | 100,0 % (2/2) |  |
| Infrastructure | `FilaNotificacoesPush` | 100,0 % (20/20) | 100,0 % (2/2) |  |
| Infrastructure | `ServicoMapasComReserva` | 100,0 % (53/53) | 100,0 % (8/8) |  |
| Infrastructure | `ServicoMapasHere` | 100,0 % (97/97) | 90,0 % (18/20) |  |
| Infrastructure | `ServicoMapasRotas` | 100,0 % (109/109) | 97,2 % (35/36) |  |
| Infrastructure | `ServicoPush` | 95,6 % (108/113) | 97,6 % (41/42) |  |
| Infrastructure | `TrabalhadorConclusaoViagens` | 96,8 % (30/31) | 50,0 % (1/2) | abaixo: ramos |
| Infrastructure | `TrabalhadorEmails` | 100,0 % (25/25) | 100,0 % (2/2) |  |
| Infrastructure | `TrabalhadorNotificacoesPush` | 89,7 % (26/29) | 100,0 % (2/2) | abaixo: linhas |

Testes acrescentados para esta medição (cenários reais): `CicloDeVidaDaViagemService` 77,8 % → 100 % de ramos (`I112_109` a `I112_111`: viagem inexistente, indisponibilizar uma viagem já indisponível, concluir a viagem de outro condutor); `ServicoMapasHere` 96,9 % → 100 % de linhas, 75 % → 90 % de ramos (`I112_105` a `I112_107`: HERE sem caminho entre os pontos, sem chave, matriz com um só ponto); `SimulacoesController` 94,1 % → 100 % de linhas (`I112_108`: simulação sem `pedidoId`).

Abaixo do critério, sem teste acrescentado, com o motivo:

- **Ramos dos controllers** (`Avaliacoes`, `Avisos`, `GestaoParticipacoes`, `Simulacoes`, `Utilizadores`, `Viagens`): os ramos por cobrir são só o `return Unauthorized()` de `if (User.IdUtilizador() is not Guid id)`. Pela API real nunca se chega lá: sem sessão válida, o JwtBearer e a política por omissão respondem 401 antes do controller (testado em `RotasProtegidasHttpTests`). Testá-los era chamar o controller com uma sessão vazia que a API nunca produz.
- **`PoliticaDeVisibilidade`** (3/4): o ramo em falta é o da comparação com um `Guid?` nulo (consulta sem utilizador), que nenhum endpoint faz.
- **`TrabalhadorConclusaoViagens`** (1/2) e **`TrabalhadorNotificacoesPush`** (89,7 % de linhas): o ramo e as linhas em falta são a saída do ciclo quando a aplicação pára a meio de uma espera ou de um envio. Um teste disso depende do momento exato da paragem (frágil, R1).
- **`RepositorioAvaliacoes`** (88 %, fora da tabela): o `catch (DbUpdateConcurrencyException)` só acontece com duas alterações da mesma avaliação ao mesmo tempo em SQL Server; e `RepositorioParticipacoes` (91,3 %): o índice único dos pedidos (erros 2601/2627) só existe em SQL Server, onde `PedidosAtivosDuplicados_BaseDadosRejeitaSegundo` o percorre.

## 7. Riscos e limitações

Os riscos de m3 mantêm-se, com estas atualizações:

- **Email:** já não é SMTP mas Brevo; o registo responde 202 e o email sai em segundo plano. As filas de email e de push são em memória: um reinício perde o que estiver por enviar.
- **Mapas:** a HERE é o fornecedor principal e o OpenRouteService a reserva; sem nenhuma das chaves, as operações que precisam dos mapas respondem 503. Sem a linha da HERE, a linha são retas (`geometriaAproximada: true`) e a operação não falha. Os testes usam respostas controladas; o teste com o serviço HERE real depende da chave, fora do git.
- **Tempo e carga:** testes que dependiam da janela do limitador ou de tempos fixos falhavam na suite completa em SQL Server com a máquina carregada (R1, 8 testes). Corrigido a 17/11 no commit `c7686c7`, só nos testes; regras na secção 4.2.

**Lacunas registadas** (por fazer; dono e momento por indicar pelo Vítor):

| ID | Lacuna | Nota |
| --- | --- | --- |
| R2 | Frontend web: achados WEB-01 a WEB-17 (inclui testes que simulam respostas antigas do backend) | Depois de o backend estar completo |
| R3 | Frontend mobile: achados MOB-01 a MOB-17 (inclui testes sem o corpo completo dos pedidos) | Idem |
| R4 | Simulação completa em Docker (API + SQL Server + frontend web) | Adiada por decisão do Vítor |
| — | Paginação das avaliações recebidas (BE-14 c) e do histórico (AP-10, BE-27) | **Fechada:** `/historico` paginado (`I112_75`); as avaliações recebidas deram só média e contagem, sem lista (BE-43, CT-070) |
| R-Q01, R-Q02 | Relatório de análise estática final (analisadores corridos de novo) | **Feito a 17/11 (E5):** build sem avisos com as regras da CI; complexidade máxima 13 (limite 15); acoplamento máximo 55 (limiar 60, sem o `Program`); `dotnet format` limpo; nenhum pacote vulnerável |
| R-Q06 | Postman com a resposta esperada (código e corpo) e asserções em cada pedido | **Feito a 17/11 (E5):** `postman/CampusCarpool.postman_collection.json`, 90 pedidos, todos os endpoints; corrida local sem chaves de mapas: 88 pedidos, 168 testes, 0 falhas (ver secção 6 e `LEIA-ME.md`). Por verificar: a corrida com a chave HERE (respostas 200 dos mapas) |

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

**Estado final da I112 (17/11, noite), antes do envio:** 856 testes; SQLite 848 aprovados, 0 falhados, 8 saltados (os 8 `[SqlServerFact]`); cobertura local sem migrations: linhas 98,35 % (4530/4606), ramos 88,75 % (1255/1414). Suite completa em SQL Server e CI por confirmar no envio (secção 9.4).

### 9.2 Correspondência com os casos

Os 14 testes com código CT no nome (CT-008, 009, 010, 013, 016, 019, 020, 022, 026, 027, 030, 032, 034, 038) continuam na suite. Os casos novos CT-039 a CT-074 estão ligados aos testes na secção 5. Os testes de erro verificam o corpo com `AssertErro`/`AssertErroAsync` (R-Q04): 91 verificações com mensagem no commit do contrato de erro da I112, 100 utilizações dos ajudantes na suite a 17/11 (contagem por pesquisa no código).

### 9.3 Lista integral dos testes executados

Não se reproduz aqui (856 testes no estado final). A lista sai de `dotnet test CampusCarpool.slnx --list-tests` e os resultados de cada execução estão nos ficheiros TRX da CI.

### 9.4 Próximas validações e critérios de fecho

- Rever os testes dos frontends contra o contrato novo do backend (R2, R3).
- Suite completa em SQL Server (local) e CI verde com o estado final (856 testes).
- Correr a coleção Postman com a chave HERE (respostas 200 dos mapas, `geometria` real); sem chave já foi corrida (secção 6).
- Teste com o serviço HERE real, com a chave fora do git.
