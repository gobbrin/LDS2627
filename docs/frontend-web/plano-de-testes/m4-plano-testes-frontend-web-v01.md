# Plano de testes do frontend web

| Campo | Informação |
| --- | --- |
| **Projeto** | G15 — Campus Carpool |
| **Artefacto** | `m4-plano-testes-frontend-web-v01.md` |
| **Milestone / Sprint** | m4 / Sprint 07 |
| **Issue de origem** | I085 |
| **Pasta de destino** | `/projeto/03-artefactos-de-qualidade-de-produto/03.02-frontend-web/03.02.01-plano-de-testes` |
| **Versão** | v01, 09/11/2026; revista a 10/11/2026 (secção 13) |

## 1. Objetivo

Definir o âmbito, a estratégia, as ferramentas, o ambiente, os dados, as responsabilidades e os critérios de entrada e de saída dos testes do frontend web. Este plano orienta a especificação (I086, I088), a implementação (I087) e o registo dos resultados (I097, I099). Não contém cenários de teste: esses pertencem às especificações.

O frontend mobile tem plano próprio (`m4-plano-testes-frontend-mobile-v01.md`), porque cada aplicação é tratada autonomamente para efeitos de qualidade (Regulamento da UC, secção 17).

## 2. Âmbito

**Em âmbito:** a aplicação Angular em `/projeto/04-artefactos-tecnicos/04.14-frontend-web/m4-v01`, integrada com a API do backend, nas funcionalidades com componente web da Especificação de Requisitos v02.

| Funcionalidade | Conteúdo | Issue |
| --- | --- | --- |
| F001 | Registo, confirmação, sessão, perfil próprio e de outros | I070 |
| F002 | Criação e gestão de viagens | I071 |
| F003, F008 | Pesquisa, mapa, rota e custo | I072 |
| F004 | Pedidos de participação e simulação | I073 |
| F005 | Mensagens e avisos | I074 |
| F006 | Notificações e tempo real | I075 |
| F007, F009 | Avaliações e histórico | I076 |

**Requisitos não funcionais com componente web** (coluna «Âmb.» com W na Especificação de Requisitos v02):

| Requisito | Como se verifica | Onde |
| --- | --- | --- |
| RNF-COM-01 — Chrome, Firefox, Edge e Safari | Teste: os fluxos principais nos quatro browsers | Especificação I086, resultados I097 |
| RNF-USA-03 — larguras de 360 a 1920 px | Teste | I086, I097 |
| RNF-FIA-04 — datas na hora de Portugal | Teste, com o browser noutro fuso | I086, I097 |
| RNF-SEG-12 — conteúdo dos utilizadores como texto (XSS) | Teste, com os payloads da secção 6 | I086, I097 |
| RNF-USA-01 — português europeu | Inspeção da interface, apoiada por um cenário de sistema | I086, I097 |
| RNF-USA-02 — erros com a causa e a correção | Inspeção e demonstração, apoiadas por cenários de sistema | I086, I097 |
| RNF-COM-03 — mesma API que o mobile | Inspeção do código | Proposta: com a análise estática, na I089 |
| RNF-SEG-08 — sem credenciais no código nem no build | Inspeção do repositório e do build | Proposta: com a análise estática, na I089 |
| RNF-SEG-01 — HTTPS | Inspeção | Ambiente público (E3), depois da I102 |
| RNF-MAN-02 e RNF-MAN-03 — quality gate e bloqueio pela pipeline | Teste e demonstração | I089 e I095 |

**Fora de âmbito:** o backend, verificado em m3 pelo seu plano; o frontend mobile; testes de carga e de desempenho; a receção real de email, substituída por uma leitura do token no ambiente de teste, exceto na simulação de aceitação em ambiente público.

## 3. Níveis de teste

| Nível | Obrigatório | Objetivo | Ferramenta | Especificação | Resultados |
| --- | --- | --- | --- | --- | --- |
| Unitário e de componente | Não (Regulamento da UC, secção 17); assumido no Plano de Qualidade v02 (QG-08 e QG-09) | Serviços, guardas, validadores, interceptores e estados de cada ecrã, com a API simulada | Karma e Jasmine, em Chrome | Ficheiros `*.spec.ts` | `03.02.04-testes-unitarios` e `03.02.05-cobertura-de-testes` |
| Sistema automático | Sim | Fluxos da aplicação num browser real, integrada com o backend | Selenium WebDriver (secção 4) | I086, em `03.02.02-testes-de-sistema` | I097, na mesma pasta |
| Aceitação (simulação) | Sim | Confirmar com participantes que o produto serve o cliente | Execução manual e guiada | I088, em `03.02.03-testes-de-aceitacao` | I099, na mesma pasta |
| Análise estática | Sim | Formatação, regras de código e quality gate | Prettier, ESLint e o que a I089 definir | I089 | `03.02.06-analise-estatica` |

### 3.1. Testes de sistema automáticos e simulação de aceitação

| | Sistema automático | Simulação de aceitação |
| --- | --- | --- |
| **Pergunta** | A aplicação comporta-se como especificado? | O produto serve quem o vai usar? |
| **Quem executa** | A pipeline e o Executor, sem intervenção humana durante a execução | Participantes, seguindo cenários, com um observador do grupo |
| **Origem dos cenários** | Requisitos e casos de uso | Âmbito acordado e necessidades do cliente |
| **Resultado** | Passou ou falhou, repetível, com registo da execução | Critérios cumpridos ou não, com as observações dos participantes |
| **Ambiente** | Local e pipeline | Ambiente público, depois da I102, ou o mais próximo disponível, declarado no relatório |
| **Evidência** | Relatório de execução e versão testada | Relatório da simulação, com participantes, datas e resultados |

Um teste automático a passar não substitui a simulação de aceitação, nem o contrário.

### 3.2. Testes unitários e cobertura

Não são obrigatórios em m4 (Regulamento da UC, secção 17), mas o Plano de Qualidade v02 assume-os como quality gates: QG-08 (todos os testes passam) e QG-09 (cobertura de pelo menos **80 % das linhas**). Existem desde a I069 e correm na pipeline; o limiar está em `karma.conf.cjs`. Ficam a cargo de quem implementa cada funcionalidade, e os resultados e a cobertura são arquivados em `03.02.04` e `03.02.05`. A situação no fim do Sprint 07 está na secção 11.

## 4. Ferramentas

| Finalidade | Ferramenta | Nota |
| --- | --- | --- |
| Aplicação | Angular 22, TypeScript 6, Node 24 LTS | Versões em `package.json` |
| Unitários e cobertura | Karma 6, Jasmine 5, Chrome | `npm test` |
| Testes de sistema | Selenium WebDriver 4 (`selenium-webdriver`) | Proposto porque o Regulamento o indica e porque conduz o Safari real (`safaridriver`, em macOS), que o RNF-COM-01 exige com número de versão. O Plano de Qualidade v02 indica Playwright no QG-06, mas o Playwright usa o seu próprio WebKit, que não é o Safari. A I087 escolhe e regista a decisão; se ficar o Selenium, o QG-06 é atualizado na versão seguinte do Plano de Qualidade. O critério do QG-06 (100 % dos testes obrigatórios passam) aplica-se seja qual for a ferramenta |
| Análise estática | Prettier, ESLint | `npm run format:check`, `npm run lint`. A I089 fecha a configuração |
| Pipeline | GitHub Actions, `frontend-web-ci.yml` | Criado na I069: formatação, ESLint, Karma com cobertura e build. A I095 integra os testes de sistema |
| Defeitos | Registo em `03.02.07-defeitos` | `m4-registo-defeitos-frontend-web-v01.xlsx` |

## 5. Ambiente

| Ambiente | Frontend | Backend e dados | Utilização |
| --- | --- | --- | --- |
| **E1 Local** | Build servido em `http://localhost:4200`, a origem aceite por omissão em `Cors:Origens` | API de teste em `http://localhost:5100`, com SQL Server de teste ou SQLite em memória | Desenvolvimento dos testes |
| **E2 Pipeline** | Build no `ubuntu-latest`, Chrome em modo headless | API de teste e SQL Server em contentor | Execução em cada integração, depois da I095 |
| **E3 Público** | Publicação da I102 | API pública, depois de corrigidos os pendentes U2, U3, U4 e U22 | Simulação de aceitação e verificação final |

**API de teste (E1 e E2).** É o `Program.cs` real, com JWT, CORS e limitador reais, mas com os serviços de email, push e mapas substituídos e com acesso de teste à base de dados, como o `AplicacaoReal` dos testes de integração do backend (`backend/tests/CampusCarpool.Tests/Integration/AplicacaoReal.cs`). É necessária por duas razões: sem a Brevo configurada, a API real responde 503 ao registo e deixa a conta por confirmar; e as operações auxiliares da especificação alteram datas na base de dados (secção 6). Nunca se usa em E3. A I087 define como a arranca em E2, e a I095 integra-a na pipeline.

**Browsers (RNF-COM-01):** as duas últimas versões estáveis de Chrome, Firefox, Edge e Safari à data de início dos testes de aceitação. O relatório regista o número de cada versão. Larguras de ecrã (RNF-USA-03): 360, 768, 1280 e 1920 px.

**Configuração:** o endereço da API vem da configuração do frontend. Nenhuma chave, palavra-passe ou token entra no repositório, que é público. `Jwt:Key` e `ConnectionStrings:DefaultConnection` são fornecidos por variáveis de ambiente.

## 6. Dados de teste

- Contas fictícias, com domínio académico aceite pelo backend (por exemplo `@estg.ipp.pt`) e sem dados pessoais reais.
- Cada teste automático cria os seus dados e não depende da ordem de execução.
- Papéis necessários: conta por confirmar, condutor confirmado, passageiro confirmado e um terceiro utilizador sem relação com a viagem.
- Viagens futuras, já partidas, concluídas, indisponibilizadas e sem lugares. As datas são relativas ao instante da execução e enviadas em UTC com `Z`.
- **O backend não tem relógio controlável** e não aceita partidas no passado. Viagens já partidas ou concluídas, ligações de confirmação expiradas e o intervalo do reenvio preparam-se com operações auxiliares que alteram datas na base de dados da API de teste, só em E1 e E2. A especificação I086 define-as (AUX-01 a AUX-13).
- Datas de inverno e de verão, com o browser noutro fuso (por exemplo `America/New_York`), para confirmar que a interface usa a hora de Portugal (RNF-FIA-04).
- O token de confirmação é obtido pelo serviço de email substituído na API de teste, e nunca em ambiente público.
- Dados nos limites: nome, curso e instituição no tamanho máximo, URL longo, 5 ligações externas.

**Payloads de XSS (RNF-SEG-12).** O requisito remete para este plano. Cada payload é guardado pela API nos campos de texto do perfil, na origem e no destino das viagens, nas mensagens e nos avisos, dentro do tamanho máximo de cada campo, e depois apresentado a outro utilizador:

| N.º | Payload | Resultado esperado |
| --- | --- | --- |
| X1 | `<script>window.__xss=1</script>` | Aparece como texto; `window.__xss` continua indefinido |
| X2 | `<img src=x onerror="window.__xss=1">` | Aparece como texto; não é criado nenhum elemento `img` |
| X3 | `"><svg onload="window.__xss=1">` | Aparece como texto; não é criado nenhum elemento `svg` |
| X4 | `{{7*7}}` | Aparece `{{7*7}}`, e não `49` |
| X5 | `javascript:window.__xss=1`, como endereço de uma ligação externa | Recusado pela aplicação e pela API, que só aceitam `http://` e `https://` |

**Limitadores da API:** 10 pedidos por minuto e por IP em login, registo e reenvio, e 100 por minuto nos restantes. Os limites estão fixos no código (`LimitesPedidos`) e **não se desligam por configuração**. O browser e as operações auxiliares saem do mesmo IP, por isso a I087 cria as contas na preparação, de preferência diretamente na base de dados da API de teste, e reutiliza sessões. Nenhum teste pode depender da janela do limitador. O relatório declara a estratégia usada.

## 7. Responsabilidades

| Atividade | Issue | Executor / Revisor / Auditor |
| --- | --- | --- |
| Plano de testes web | I085 | Vítor Lopes / Gil Leão / Joana Rodrigues |
| Especificação dos testes de sistema | I086 | Cristiana Macedo / Vítor Lopes / Joana Rodrigues |
| Testes unitários e de componente | I069 a I076 | Executor de cada issue |
| Implementação dos testes de sistema | I087 | A atribuir no Sprint Planning |
| Especificação dos testes de aceitação | I088 | A atribuir no Sprint Planning |
| Análise estática e relatório | I089 | A atribuir no Sprint Planning |
| Integração na pipeline | I095 | A atribuir no Sprint Planning |
| Resultados dos testes de sistema | I097 | A atribuir no Sprint Planning |
| Simulação de aceitação | I099 | A atribuir no Sprint Planning |

Cada defeito é registado por quem o encontra e corrigido pelo Executor da funcionalidade. O teste de confirmação é feito por quem o registou.

## 8. Critérios de entrada

**Comuns:** a tarefa cumpre a Definition of Ready, e a funcionalidade a testar está integrada na branch de teste com `npm run check` a passar.

| Nível | Critérios adicionais |
| --- | --- |
| Sistema automático | Especificação I086 revista; I069 a I076 integradas, para os fluxos respetivos; API de teste disponível (secção 5), com a base de dados migrada; dados de teste criados |
| Simulação de aceitação | Especificação I088 revista; testes de sistema executados, sem defeitos de severidade alta abertos; ambiente E3 disponível, ou alternativa declarada; participantes identificados |
| Análise estática | Configuração da I089 no projeto |

## 9. Critérios de saída

1. **0 testes falhados** nas execuções unitária e de sistema. Um teste saltado só é aceite com motivo registado.
2. **Cobertura de linhas de pelo menos 80 %** nos testes unitários e de componente (QG-09).
3. **Cada fluxo principal** do glossário da Especificação de Requisitos tem cenário de sistema a passar: registo e confirmação, início de sessão, criar viagem, pesquisar viagens, pedir participação, aceitar ou rejeitar um pedido, enviar mensagem, consultar notificações, avaliar e consultar o histórico.
4. **RNF-COM-01:** os fluxos principais passam nos quatro browsers, com as versões registadas.
5. **RNF-USA-03:** sem deslocamento horizontal da página nas larguras indicadas.
6. **Quality gates cumpridos.** Os do Plano de Qualidade v02 para o frontend web são QG-01 (build sem erros), QG-05 (ESLint sem erros), QG-06 (testes de sistema), QG-08 e QG-09 (unitários e cobertura) e QG-12 (Prettier). A pipeline atual já verifica QG-01, QG-05, QG-08, QG-09 e QG-12; o QG-06 entra com a I095. A P-29 deixou o quality gate de análise estática dos frontends por definir em m4. Proposta, a fechar na I089: QG-05 e QG-12, mais 0 avisos no ESLint e 0 vulnerabilidades altas ou críticas no `npm audit`. Nenhuma das duas condições novas está ainda na pipeline: hoje o `npm run lint` não falha com avisos.
7. **Pipeline verde** no commit entregue, e bloqueio demonstrado quando um teste ou o quality gate falham (RNF-MAN-03).
8. **Simulação de aceitação concluída,** com cada critério avaliado e as observações registadas.
9. **Defeitos registados** em `03.02.07`, com severidade e estado, e nenhum de severidade alta aberto sem decisão registada.
10. **Rastreabilidade:** cada cenário indica os requisitos que verifica, e a RTM é atualizada na I104.

## 10. Riscos e limitações

| Risco | Efeito | Resposta |
| --- | --- | --- |
| Safari exige macOS | Sem Mac, RNF-COM-01 fica incompleto | Identificar uma máquina macOS antes da I097. Se não existir, repetir os fluxos manualmente noutro equipamento ou declarar a falta no relatório |
| Ferramenta dos testes de sistema diferente da do QG-06 | Plano, especificação I086 e Plano de Qualidade desalinhados | A I087 decide e regista; se ficar o Selenium, atualiza-se o QG-06 (secção 4) |
| API de teste ainda por montar em E2 | Testes de sistema fora da pipeline | A I087 parte do `AplicacaoReal`; a I095 integra-a |
| Ambiente público com pendentes U2, U3, U4 e U22 | A simulação não corre em E3 | Corrigir antes da I102; alternativa em E1, declarada |
| Sem geometria da rota no backend (DEF-BE-054) | O mapa da I072 fica limitado | Decidir na I072 e refletir na especificação |
| Defeitos do hub (DEF-BE-055, 056) e do reenvio (DEF-BE-045, 046) | Comportamentos a contornar nos testes | Cenários indicam o defeito conhecido |
| Testes de sistema instáveis com SignalR e temporizadores | Falsos negativos | Esperas explícitas por condição, nunca tempos fixos |
| SQLite em memória difere do SQL Server | Resultados locais incompletos | Executar em SQL Server na pipeline, E2 |
| Dependência de serviços externos (Brevo, OpenRouteService) | Quotas e falhas | Substituídos na API de teste; reais apenas em E3 |

## 11. Situação dos testes no fim do Sprint 07

Situação a 10/11/2026. Os testes de sistema, a simulação de aceitação e o relatório de análise estática pertencem às issues das secções 3 e 7. Esta secção regista o que as issues de funcionalidade do Sprint 07 já executaram e o que ficou por executar, para que nada se perca entre sprints.

| Issue | Executado | Por executar | Quem e quando |
| --- | --- | --- | --- |
| I069 | Formatação, ESLint e build; Karma 40/40, com 99,69 % das linhas; 18/18 verificações no Chrome contra a API local; pipeline verde | Nada específico da issue | — |
| I070 | Karma 196/196, com 99,87 % das linhas; guiões no Chrome contra a API local sem falhas (212/212 no guião final); pipeline verde no PR #40 | Nada específico da issue | — |
| I071 | Karma 228/228, com 99,91 % das linhas; 75/75 no Chrome contra a API local e 9/9 com o browser em `America/New_York`; regressão da I070, 212/212 | Pipeline no GitHub | Executora da I071, no push, antes da aceitação |
| I069 a I071 | — | Firefox, Edge e Safari (RNF-COM-01); larguras do RNF-USA-03; SQL Server; payloads de XSS da secção 6; testes de sistema automáticos versionados no projeto | I087 (implementação) e I097 (resultados) |

As verificações no Chrome foram feitas com guiões do Executor (Playwright), contra a API local com SQLite, fora do repositório. São evidência das issues, mas não substituem os testes de sistema da I087.

**Testes recomendados, por fazer:** nenhum teste unitário confirma que os ecrãs de perfil (I070) e de viagens (I071) mostram os payloads X1 a X4 como texto. O Angular já escapa a interpolação, mas falta a prova. Ficam para quem voltar a estes ecrãs nas issues seguintes, ou para a I087, no cenário do RNF-SEG-12.

## 12. Documentos relacionados

Regulamento da UC (secções 8, 14, 15, 17 e 19); Proposta de Sistema v03; Declaração de Âmbito v03; Especificação de Requisitos v02; Plano de Qualidade v02 (secção 4.1, quality gates); Regulamento Interno do Grupo; `m3-plano-testes-backend-v01.md`; modelo de classes do frontend web; protótipo de baixa fidelidade; `m4-backlog-projeto-v08`; `m4-especificacao-testes-sistema-frontend-web-v01.md` (I086).

## 13. Histórico

| Data | Alteração |
| --- | --- |
| 09/11/2026 | Versão inicial |
| 10/11/2026 | Revista antes da revisão formal. Secção 2: todos os requisitos não funcionais com componente web, com a forma de verificação. Secções 3, 4 e 9: quality gates do Plano de Qualidade v02 e divergência do QG-06 declarada. Secção 5: API de teste. Secção 6: relógio do backend, fuso do browser, payloads de XSS e limitador fixo no código (a versão anterior admitia desligá-lo por configuração, o que o backend não permite). Secção 11: situação dos testes no fim do Sprint 07 |
