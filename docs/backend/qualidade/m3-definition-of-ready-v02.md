# Definition of Ready (DoR)

| Campo | Informação |
| --- | --- |
| **Unidade curricular** | Laboratório de Desenvolvimento de Software — LDS |
| **Grupo** | 15 |
| **Milestone de entrega** | M3 |
| **Momento de avaliação** | M3 |
| **Versão do documento** | v02 |
| **Ficheiro final a submeter** | `m3-definition-of-ready-v02.pdf` |
| **Pasta de destino** | `/projeto/02-artefactos-de-qualidade-de-processo/02.02-definition-of-ready-e-done` |
| **Versão anterior** | `m1-definition-of-ready-v01.pdf` (mantida como histórico) |
| **Documento complementar** | `m1-definition-of-done-v01.pdf`, na mesma pasta. Este documento não substitui nem incorpora a DoD; ambos são artefactos autónomos e obrigatórios. |
| **Documentos relacionados** | Critérios de Qualidade dos Requisitos v02; Checklist de Aprovação dos Requisitos v02; Especificação de Requisitos |
| **Responsável pela elaboração** | v01: Joana Francisca Pacheco Rodrigues. v02: Vítor Samuel Vieira Lopes |
| **Responsável pela manutenção** | Líder do Grupo em funções |
| **Estado** | Por aprovar na reunião de controlo final e fecho de m3 |
| **Aprovação** | v01 aprovada na reunião de assinaturas de 25/09/2026, conforme a respetiva ata. v02: reunião de controlo final e fecho de m3 (ata da reunião) |
| **Histórico de versões** | v01 — aprovada a 25/09/2026. v02 — 03/11/2026: a secção 4 mantém o título e o conteúdo da v01, passa a estar dividida em 4.1 e 4.2 e remete para os Critérios de Qualidade dos Requisitos e para a Checklist de Aprovação dos Requisitos; acrescenta o critério R16 (requisitos aprovados) e a ligação entre as issues e os requisitos (secção 4.2). As referências ao GitLab passam a GitHub: o GitLab da ESTG ficou indisponível e o grupo passou as issues, o repositório e a pipeline para o GitHub (decisões G13 e C3, 04/11/2026). |

---

## 1. Objetivo e âmbito

A Definition of Ready (DoR) estabelece o conjunto de condições que uma tarefa normal deve cumprir para ser considerada suficientemente preparada para execução.

Uma tarefa que não cumpra os critérios obrigatórios de prontidão permanece no Backlog de Projeto e não pode ser distribuída nem selecionada para execução no Sprint.

A DoR aplica-se às tarefas normais do projeto durante o processo de Backlog Refinement e constitui a referência utilizada pelo grupo para verificar se uma tarefa está suficientemente especificada antes do Sprint Planning.

A DoR não substitui a Definition of Done (DoD). Enquanto a DoR determina se uma tarefa está preparada para ser executada, a DoD determina se a tarefa foi efetivamente concluída.

### 1.1 Exceção — tarefa agregada do Secretário do Sprint

A tarefa agregada do Secretário do Sprint, destinada às atividades administrativas e logísticas normais do Sprint, constitui uma exceção ao processo normal de estimativa e divisão de papéis. Esta tarefa:

- possui o limite de pontos definido pelo Regulamento aplicável;
- não é dividida entre Executor, Revisor e Auditor como uma tarefa normal;
- não é sujeita à estimativa nem à divisão de pontos aplicáveis às tarefas normais e rege-se pela secção 9.3 do Regulamento de Funcionamento da Unidade Curricular;
- deve, contudo, possuir uma descrição suficientemente clara das atividades a executar;
- deve ter as evidências e responsabilidades necessárias definidas;
- deve permitir verificar posteriormente o cumprimento das atividades previstas.

Uma atividade administrativa que ultrapasse o secretariado normal e constitua uma tarefa materialmente relevante deve ser criada como tarefa autónoma e seguir o processo normal de preparação.

## 2. Critérios obrigatórios de Ready

Para que uma tarefa possa ser considerada Ready durante o Backlog Refinement, devem estar definidos e validados os elementos necessários à sua execução.

A atribuição dos nomes concretos de Executor, Revisor e Auditor ocorre posteriormente, de acordo com a Grelha de Rotação de Papéis e o Sprint Planning.

| # | Critério | Descrição e evidência exigida | Responsável no Refinement |
| --- | --- | --- | --- |
| R1 | Identificação da tarefa | A tarefa possui uma Issue no GitHub com identificação inequívoca e título claro. A Issue deve permitir relacionar a tarefa com o respetivo registo no Backlog de Projeto. | Grupo |
| R2 | Descrição e objetivo | A tarefa possui uma descrição clara do trabalho a realizar e um objetivo definido. | Grupo |
| R3 | Resultado esperado | O resultado ou artefacto esperado encontra-se claramente identificado. | Grupo |
| R4 | Critérios de aceitação | Estão definidos critérios de aceitação objetivos, verificáveis e adequados ao resultado esperado (secção 4.1). | Grupo |
| R5 | Evidências do Executor | Estão identificadas as evidências que deverão ser produzidas pelo Executor para demonstrar a execução da tarefa. | Grupo |
| R6 | Verificações do Revisor | Estão identificados os aspetos que deverão ser verificados pelo Revisor. | Grupo |
| R7 | Checklist do Auditor | Estão definidos os pontos de controlo que deverão ser verificados pelo Auditor, incluindo a conformidade do processo e das evidências. | Grupo |
| R8 | Dependências | As dependências conhecidas da tarefa encontram-se identificadas e relacionadas com o planeamento do projeto. | Grupo |
| R9 | Normas e referências | Estão identificados os documentos, normas, modelos, decisões ou restantes referências necessárias à execução da tarefa, incluindo a DoD aplicável. | Grupo |
| R10 | Pasta de destino | Está definida a pasta onde o artefacto, ou as evidências produzidas pela tarefa, deverão ser guardados. | Grupo |
| R11 | Pontuação / orçamento | A tarefa possui o orçamento de pontos definido de acordo com as regras de estimativa aplicáveis e com a escala oficial adotada pelo grupo/UC. | Grupo |
| R12 | Divisão da pontuação | O orçamento é dividido de acordo com a repartição fixa estabelecida no Regulamento de Funcionamento da UC e refletida no Regulamento Interno: 60 % Executor, 30 % Revisor e 10 % Auditor. | Grupo |
| R13 | Tipo da tarefa | O tipo da tarefa (por exemplo, desenvolvimento, testes, documentação, qualidade) está identificado na Issue, através das etiquetas de área/conteúdo. | Grupo |
| R14 | Ausência de impedimento | Não existe impedimento incompatível com a execução da tarefa no Sprint (dependência por resolver, decisão pendente, recurso indisponível). | Grupo |
| R15 | Aprovação no Refinement | A tarefa foi aprovada no Backlog Refinement, com a aprovação registada na ata e refletida no Backlog de Projeto. | Grupo |
| **R16** | **Requisitos aprovados** *(novo na v02)* | Quando a tarefa implementa, altera ou testa requisitos, a Issue indica os IDs desses requisitos (ex.: `RF-F004-10`) e todos estão **Aprovados** na Checklist de Aprovação dos Requisitos em vigor (secção 4.2). | Grupo |

## 3. Informação obrigatória no Backlog e na Issue

A informação utilizada para considerar uma tarefa Ready deve ser mantida de forma rastreável.

O Backlog de Projeto em XLS/XLSX constitui o registo formal do planeamento. A Issue do GitHub constitui o registo operacional complementar da tarefa.

Quando uma tarefa estiver pronta, devem existir informações suficientes para relacionar:

> Requisitos → Backlog → Issue → execução → revisão → auditoria → Sprint Review

A Issue deve utilizar as etiquetas existentes no GitHub de acordo com a natureza e o estado da tarefa.

Podem ser utilizadas, entre outras, as etiquetas de conteúdo: Administration, Analysis, Architecture, Backlog, Database, Development, Documentation, Management, Quality e Testing.

Para o estado da tarefa são utilizadas as etiquetas: Ready, In-Progress, In-Review, In-Audit, Ready-for-Sprint-Review, Done e Blocked.

A etiqueta Ready identifica uma tarefa que cumpre os critérios de prontidão e está preparada para ser executada. Corresponde ao estado oficial Pendente (tarefa ainda não iniciada).

A correspondência completa entre as etiquetas de estado e os estados oficiais das tarefas é a definida na Definition of Done (secção 4) e no Regulamento Interno (secção 9.2).

## 4. Critérios de qualidade dos requisitos da tarefa

Esta secção define como devem estar escritos os requisitos da tarefa, isto é, o que se pede à tarefa para poder ser executada e aceite. A qualidade dos requisitos do sistema (se um RF ou RNF está bem escrito) é avaliada noutros dois documentos, em `04-artefactos-tecnicos/04.03-requisitos-e-rtm`: os Critérios de Qualidade dos Requisitos v02 definem os critérios e a Checklist de Aprovação dos Requisitos v02 regista o resultado. A DoR liga-se a esses documentos pelo critério R16 (secção 4.2).

### 4.1 Critérios de aceitação da tarefa

Os critérios de aceitação definidos para uma tarefa devem ser:

- claros;
- objetivos;
- verificáveis;
- relacionados com o resultado esperado;
- suficientemente específicos para permitir a revisão;
- compatíveis com as normas e documentos aplicáveis.

Os critérios não devem depender exclusivamente de interpretações subjetivas do Executor ou do Revisor.

Quando uma tarefa produzir um artefacto documental, deve também estar definido o formato e, quando aplicável, o local de arquivo.

Quando uma tarefa produzir código, devem estar identificados os requisitos técnicos relevantes, incluindo a tecnologia, componente ou área afetada quando estes forem conhecidos.

Quando a tarefa implementar requisitos do sistema, os critérios de aceitação da tarefa incluem os critérios de aceitação desses requisitos, tal como estão na Especificação de Requisitos.

### 4.2 Requisitos implementados pela tarefa (critério R16)

1. Uma Issue que implementa, altera ou testa requisitos do sistema indica os respetivos IDs (ex.: `RF-F004-10`, `RNF-SEG-09`).
2. Esses requisitos têm de estar **Aprovados** na Checklist de Aprovação dos Requisitos em vigor, segundo os Critérios de Qualidade dos Requisitos v02 (secção 4).
3. Um requisito **Não aprovado** impede a tarefa de ser Ready, exceto quando a própria tarefa é a correção do defeito que o reprovou (documentação ou código). Nesse caso, a Issue indica o ID do defeito (ex.: `D-012`).
4. Se, durante o refinamento, se encontrar um defeito num requisito já aprovado, o defeito é registado na folha "Defeitos" da checklist e o requisito volta a ser avaliado na versão seguinte da especificação (Critérios v02, secção 4.1). Até lá, a tarefa fica bloqueada por dependência (secção 5).
5. A RTM liga cada requisito às Issues que o implementam.

Tarefas que não implementam requisitos do sistema (ex.: gestão, atas, documentação de processo) não precisam de cumprir R16; nesse caso a Issue indica "R16: não aplicável".

## 5. Dependências

Antes de uma tarefa ser considerada Ready, o grupo deve identificar as dependências conhecidas.

Podem constituir dependências:

- outras Issues;
- decisões ainda pendentes;
- artefactos necessários;
- requisitos, incluindo requisitos ainda não aprovados ou com defeitos por corrigir;
- componentes técnicos;
- acesso a ferramentas ou ambientes;
- resultados de tarefas anteriores.

Uma dependência que impeça efetivamente o início da tarefa deve ser refletida no planeamento.

Caso uma tarefa já Ready fique posteriormente impedida por uma dependência, é utilizada a indicação Bloqueada (etiqueta Blocked) enquanto o impedimento não estiver resolvido. A indicação deve registar o motivo, a data, a dependência, o responsável pela resolução e o impacto, e não cria uma pista nem um estado terminal autónomo.

## 6. Estimativa e orçamento

A tarefa deve possuir um orçamento definido antes da sua entrada no Sprint.

A estimativa deve respeitar as regras estabelecidas para o projeto e para a UC. Quando for utilizado Planning Poker, deve ser utilizada a escala oficial definida para o projeto.

O orçamento atribuído à tarefa constitui a referência para a sua execução e para a posterior avaliação dos papéis.

Para as tarefas normais às quais se aplica a divisão de papéis:

- Executor: 60 %;
- Revisor: 30 %;
- Auditor: 10 %.

A divisão deve respeitar o orçamento total da tarefa e não deve criar pontos adicionais fora do orçamento aprovado.

## 7. Atribuição dos papéis

A DoR não determina antecipadamente os nomes dos alunos que irão desempenhar os papéis.

Depois de a tarefa cumprir os critérios de Ready, a atribuição dos papéis é realizada de acordo com:

- a Grelha de Rotação de Papéis;
- as regras de independência;
- a distribuição de carga;
- o Sprint Planning.

A mesma pessoa não deve desempenhar simultaneamente os papéis de Executor e Revisor ou Executor e Auditor da mesma tarefa quando tal comprometa a independência prevista no Regulamento Interno.

A identificação dos elementos atribuídos deve ficar registada na Issue e nos restantes registos oficiais aplicáveis.

## 8. Relação entre DoR e estados do GitHub

A DoR corresponde ao momento em que uma tarefa passa a estar preparada para execução. O fluxo normal é:

> Backlog de Projeto → Ready → In-Progress → In-Review → In-Audit → Ready-for-Sprint-Review → Done

A indicação Blocked pode ser utilizada, em qualquer momento do fluxo, quando uma tarefa fica impedida por uma dependência ou impedimento identificado.

Uma tarefa estar Ready não significa que esteja concluída. A passagem para Done só ocorre depois do cumprimento da Definition of Done e da aprovação no Sprint Review.

## 9. Consequências do incumprimento da DoR

Se um dos critérios obrigatórios da DoR não estiver cumprido, a tarefa não deve ser considerada Ready. A tarefa deve permanecer no Backlog de Projeto até que a informação em falta seja definida, revista e validada.

Entre as situações que impedem a passagem para Ready encontram-se:

- ausência de descrição clara;
- ausência de objetivo;
- resultado esperado indefinido;
- critérios de aceitação insuficientes;
- requisitos implementados não identificados ou não aprovados na checklist em vigor (R16);
- dependências desconhecidas ou não tratadas;
- ausência de referências necessárias;
- orçamento não definido;
- divisão da pontuação não definida quando aplicável;
- ausência da pasta de destino;
- tipo da tarefa não identificado;
- existência de impedimento incompatível com a execução;
- ausência de aprovação no Backlog Refinement;
- falta de informação necessária para a revisão ou auditoria.

Uma tarefa que não cumpra os critérios obrigatórios de prontidão não pode ser distribuída nem incluída no Sprint, uma vez que tal comprometeria a previsibilidade do Sprint e a qualidade do processo de desenvolvimento.

## 10. Checklist de Definition of Ready

Antes de uma tarefa ser marcada como Ready, deve ser possível confirmar:

- [ ] A Issue foi criada no GitHub.
- [ ] A tarefa possui identificação inequívoca.
- [ ] A descrição está definida.
- [ ] O objetivo está definido.
- [ ] O resultado esperado está definido.
- [ ] Os critérios de aceitação estão definidos.
- [ ] Os requisitos implementados estão indicados na Issue e estão Aprovados na checklist em vigor, ou a Issue indica "R16: não aplicável".
- [ ] As evidências esperadas do Executor estão definidas.
- [ ] As verificações do Revisor estão definidas.
- [ ] Os pontos de controlo do Auditor estão definidos.
- [ ] As dependências conhecidas estão identificadas.
- [ ] As referências necessárias estão indicadas.
- [ ] A pasta de destino está definida.
- [ ] O orçamento da tarefa está definido.
- [ ] A divisão da pontuação (60 % Executor, 30 % Revisor, 10 % Auditor) está definida.
- [ ] O tipo da tarefa está identificado.
- [ ] Não existe impedimento incompatível com a execução.
- [ ] A tarefa foi aprovada no Backlog Refinement.
- [ ] A tarefa está registada no Backlog de Projeto.
- [ ] A tarefa pode ser relacionada com a respetiva Issue.
- [ ] A tarefa cumpre os critérios necessários para ser considerada Ready.

Só depois de os critérios obrigatórios estarem cumpridos é que a tarefa deve ser considerada Ready para execução.
