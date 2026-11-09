# Plano de testes do frontend web

| Campo | Informação |
| --- | --- |
| **Projeto** | G15 — Campus Carpool |
| **Artefacto** | `m4-plano-testes-frontend-web-v01.md` |
| **Milestone / Sprint** | m4 / Sprint 07 |
| **Issue de origem** | I085 |
| **Pasta de destino** | `/projeto/03-artefactos-de-qualidade-de-produto/03.02-frontend-web/03.02.01-plano-de-testes` |
| **Versão** | v01, 09/11/2026 |

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

**Requisitos não funcionais verificados por este plano:** RNF-COM-01 (browsers), RNF-COM-03 (mesma API), RNF-USA-02 (mensagens de erro), RNF-USA-03 (larguras de 360 a 1920 px) e RNF-MAN-02 e 03 (quality gate e bloqueio pela pipeline).

**Fora de âmbito:** o backend, verificado em m3 pelo seu plano; o frontend mobile; testes de carga e de desempenho; a receção real de email, substituída por uma leitura do token no ambiente de teste, exceto na simulação de aceitação em ambiente público.

## 3. Níveis de teste

| Nível | Obrigatório | Objetivo | Ferramenta | Especificação | Resultados |
| --- | --- | --- | --- | --- | --- |
| Unitário e de componente | Não; assumido pelo grupo | Serviços, guardas, validadores, interceptores e estados de cada ecrã, com a API simulada | Karma e Jasmine, em Chrome | Ficheiros `*.spec.ts` | `03.02.04-testes-unitarios` e `03.02.05-cobertura-de-testes` |
| Sistema automático | Sim | Fluxos da aplicação num browser real, integrada com o backend | Selenium WebDriver | I086, em `03.02.02-testes-de-sistema` | I097, na mesma pasta |
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

Não são obrigatórios em m4 (Regulamento da UC, secção 17), mas o grupo assume-os: já existem desde a I069 e correm na pipeline. O limiar é de **80 % das linhas**, definido em `karma.conf.cjs`. Ficam a cargo de quem implementa cada funcionalidade, e os resultados e a cobertura são arquivados em `03.02.04` e `03.02.05`.

## 4. Ferramentas

| Finalidade | Ferramenta | Nota |
| --- | --- | --- |
| Aplicação | Angular 22, TypeScript 6, Node 24 LTS | Versões em `package.json` |
| Unitários e cobertura | Karma 6, Jasmine 5, Chrome | `npm test` |
| Testes de sistema | Selenium WebDriver 4 (`selenium-webdriver`) | Escolhido porque o Regulamento o indica e porque cobre os quatro browsers de RNF-COM-01. A I087 pode propor uma tecnologia equivalente, se a justificar |
| Análise estática | Prettier, ESLint | `npm run format:check`, `npm run lint`. A I089 fecha a configuração |
| Pipeline | GitHub Actions, `frontend-web-ci.yml` | A I095 integra os testes de sistema |
| Defeitos | Registo em `03.02.07-defeitos` | `m4-registo-defeitos-frontend-web-v01.xlsx` |

## 5. Ambiente

| Ambiente | Frontend | Backend e dados | Utilização |
| --- | --- | --- | --- |
| **E1 Local** | Build servido em `http://localhost:4200`, a origem aceite por omissão em `Cors:Origens` | API real em `http://localhost:5100`, com SQL Server de teste ou SQLite em memória | Desenvolvimento dos testes |
| **E2 Pipeline** | Build no `ubuntu-latest`, Chrome em modo headless | API e SQL Server em contentor | Execução em cada integração, depois da I095 |
| **E3 Público** | Publicação da I102 | API pública, depois de corrigidos os pendentes U2, U3, U4 e U22 | Simulação de aceitação e verificação final |

**Browsers (RNF-COM-01):** as duas últimas versões estáveis de Chrome, Firefox, Edge e Safari à data de início dos testes de aceitação. O relatório regista o número de cada versão. Larguras de ecrã (RNF-USA-03): 360, 768, 1280 e 1920 px.

**Configuração:** o endereço da API vem da configuração do frontend. Nenhuma chave, palavra-passe ou token entra no repositório, que é público. `Jwt:Key` e `ConnectionStrings:DefaultConnection` são fornecidos por variáveis de ambiente.

## 6. Dados de teste

- Contas fictícias, com domínio académico aceite pelo backend (por exemplo `@estg.ipp.pt`) e sem dados pessoais reais.
- Cada teste automático cria os seus dados e não depende da ordem de execução.
- Papéis necessários: conta por confirmar, condutor confirmado, passageiro confirmado e um terceiro utilizador sem relação com a viagem.
- Viagens futuras, concluídas, indisponibilizadas e sem lugares. As datas são relativas ao instante da execução e enviadas em UTC com `Z`.
- O token de confirmação é obtido pelo serviço de email substituído no ambiente de teste, e nunca em ambiente público.
- Dados nos limites: nome, curso e instituição no tamanho máximo, URL longo, 5 ligações externas.

**Limitadores da API:** 10 pedidos por minuto e por IP em login, registo e reenvio, e 100 por minuto nos restantes. A I087 deve reutilizar sessões ou desligar o limitador na configuração de teste, e declará-lo no relatório. Não pode escrever testes que dependam da janela do limitador.

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
| Sistema automático | Especificação I086 revista; I069 a I076 integradas, para os fluxos respetivos; API disponível, com a base de dados migrada; dados de teste criados |
| Simulação de aceitação | Especificação I088 revista; testes de sistema executados, sem defeitos de severidade alta abertos; ambiente E3 disponível, ou alternativa declarada; participantes identificados |
| Análise estática | Configuração da I089 no projeto |

## 9. Critérios de saída

1. **0 testes falhados** nas execuções unitária e de sistema. Um teste saltado só é aceite com motivo registado.
2. **Cobertura de linhas de pelo menos 80 %** nos testes unitários e de componente.
3. **Cada fluxo principal** do glossário da Especificação de Requisitos tem cenário de sistema a passar: registo e confirmação, início de sessão, criar viagem, pesquisar viagens, pedir participação, aceitar ou rejeitar um pedido, enviar mensagem, consultar notificações, avaliar e consultar o histórico.
4. **RNF-COM-01:** os fluxos principais passam nos quatro browsers, com as versões registadas.
5. **RNF-USA-03:** sem deslocamento horizontal da página nas larguras indicadas.
6. **Quality gate de análise estática cumprido.** A P-29 deixou os frontends por definir em m4. Proposta, a fechar na I089: formatação sem alterações, 0 erros e 0 avisos no ESLint, build sem erros, 0 vulnerabilidades altas ou críticas no `npm audit` e a cobertura do ponto 2.
7. **Pipeline verde** no commit entregue, e bloqueio demonstrado quando um teste ou o quality gate falham (RNF-MAN-03).
8. **Simulação de aceitação concluída,** com cada critério avaliado e as observações registadas.
9. **Defeitos registados** em `03.02.07`, com severidade e estado, e nenhum de severidade alta aberto sem decisão registada.
10. **Rastreabilidade:** cada cenário indica os requisitos que verifica, e a RTM é atualizada na I104.

## 10. Riscos e limitações

| Risco | Efeito | Resposta |
| --- | --- | --- |
| Safari exige macOS | Sem Mac, RNF-COM-01 fica incompleto | Identificar uma máquina macOS antes da I097. Se não existir, repetir os fluxos manualmente noutro equipamento ou declarar a falta no relatório |
| Ambiente público com pendentes U2, U3, U4 e U22 | A simulação não corre em E3 | Corrigir antes da I102; alternativa em E1, declarada |
| Sem geometria da rota no backend (DEF-BE-054) | O mapa da I072 fica limitado | Decidir na I072 e refletir na especificação |
| Defeitos do hub (DEF-BE-055, 056) e do reenvio (DEF-BE-045, 046) | Comportamentos a contornar nos testes | Cenários indicam o defeito conhecido |
| Testes de sistema instáveis com SignalR e temporizadores | Falsos negativos | Esperas explícitas por condição, nunca tempos fixos |
| SQLite em memória difere do SQL Server | Resultados locais incompletos | Executar em SQL Server na pipeline, E2 |
| Dependência de serviços externos (Brevo, OpenRouteService) | Quotas e falhas | Substituídos nos testes automáticos; reais apenas em E3 |

## 11. Documentos relacionados

Regulamento da UC (secções 8, 14, 15, 17 e 19); Proposta de Sistema v03; Declaração de Âmbito v03; Especificação de Requisitos v02; Regulamento Interno do Grupo; `m3-plano-testes-backend-v01.md`; modelo de classes do frontend web; protótipo de baixa fidelidade; `m4-backlog-projeto-v08`.
