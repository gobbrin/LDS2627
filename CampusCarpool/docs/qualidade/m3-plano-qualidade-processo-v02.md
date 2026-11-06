# Plano de Qualidade de Processo

| Campo | Informação |
| --- | --- |
| **Unidade curricular** | Laboratório de Desenvolvimento de Software — LDS |
| **Grupo** | 15 |
| **Milestone de Entrega** | M3 |
| **Momento de Avaliação** | M3 |
| **Versão do documento** | v02 |
| **Ficheiro final a submeter** | `m3-plano-qualidade-processo-v02.pdf` |
| **Pasta de destino** | `/projeto/02-artefactos-de-qualidade-de-processo/02.01-plano-de-qualidade` |
| **Versão anterior** | `m1-plano-qualidade-processo-v01.pdf` (mantida como histórico) |
| **Responsável pela elaboração** | Aluno identificado através da atribuição da tarefa na respetiva Issue do GitHub |
| **Responsável pela manutenção** | Líder do Grupo em funções |
| **Estado** | Por aprovar na reunião de controlo final e fecho de m3 |
| **Aprovação** | v01 aprovada na reunião de assinaturas de 25/09/2026, conforme a respetiva ata. v02: reunião de controlo final e fecho de m3 (ata da reunião) |

**Histórico de versões**

| Versão | Data | Descrição das alterações | Reunião e ata de aprovação |
| --- | --- | --- | --- |
| v01 | 25/09/2026 | Elaboração inicial do Plano de Qualidade de Processo. | Reunião de 25/09/2026 |
| v02 | 06/11/2026 | Passagem do GitLab para o GitHub e o GitHub Actions (decisão G13). Quality gates alinhados com a pipeline real: cobertura sem migrations (G14), SQL Server real nos testes (G15), análise estática com os analisadores .NET e verificação de pacotes vulneráveis. Referências atualizadas para as versões em vigor dos documentos. | Reunião de controlo final e fecho de m3 |

## 1. Objetivo e Âmbito

O Plano de Qualidade de Processo define as responsabilidades, procedimentos, métricas, verificações e quality gates que o grupo utiliza durante o desenvolvimento do projeto.

O objetivo é garantir:

- o cumprimento do processo definido no Regulamento Interno do Grupo;
- a qualidade dos artefactos produzidos;
- a aplicação dos papéis de Executor, Revisor, Auditor e Secretário;
- a rastreabilidade das tarefas, revisões, auditorias e evidências;
- a qualidade técnica do Backend, Frontend Web e Frontend Mobile;
- a aplicação dos mecanismos de análise estática, testes, cobertura e CI/CD;
- o cumprimento das regras de integração na branch `main`.

Este documento concretiza operacionalmente as regras de qualidade definidas no Regulamento de Funcionamento da UC e no Regulamento Interno do Grupo, não os substituindo.

## 2. Responsabilidades de Qualidade

A garantia da qualidade assenta na separação e rotação dos papéis de avaliação interna definidos pelo grupo.

| Papel | Responsabilidades no contexto da qualidade |
| --- | --- |
| **Executor** | Executa a tarefa atribuída, produz o artefacto ou código previsto e garante o cumprimento dos critérios de aceitação e da Definition of Done. Deve assegurar que o trabalho está corretamente preparado antes de o submeter a revisão. Nunca aprova, aceita nem integra o seu próprio Pull Request. |
| **Revisor** | Analisa tecnicamente o trabalho produzido pelo Executor, verifica os critérios de aceitação e a Definition of Done, identifica defeitos e propostas de melhoria e regista as verificações efetuadas. Depois da autorização do Auditor, aceita o resultado e, no código, aprova e executa o merge. |
| **Auditor** | Verifica o processo de revisão, a independência dos papéis, a existência das evidências necessárias e a conformidade da tarefa com o planeamento e com o Regulamento Interno. Autoriza a aceitação (no código, o merge) quando os critérios aplicáveis estiverem cumpridos e confirma a versão final depois da aceitação. |
| **Secretário do Sprint** | Assegura a organização, preservação e arquivo das evidências produzidas durante o Sprint, incluindo atas, checklists, documentos e restantes artefactos de controlo. |
| **Líder do Grupo** | Coordena o funcionamento global do grupo e assegura a manutenção da configuração geral das ferramentas e processos definidos no Regulamento Interno. |

A atribuição dos papéis é realizada de acordo com a Grelha de Rotação de Papéis, garantindo a independência entre Executor, Revisor e Auditor. Nenhum elemento deve executar a revisão ou auditoria da própria tarefa quando tal comprometa a independência definida no Regulamento Interno.

## 3. Qualidade de Processo

O controlo da qualidade do processo baseia-se nos seguintes mecanismos.

### 3.1. Definition of Ready e Definition of Done

As tarefas devem cumprir os critérios de Definition of Ready (DoR) antes de serem consideradas prontas para execução. Uma tarefa apenas pode ser considerada concluída quando cumprir cumulativamente os critérios definidos na Definition of Done (DoD) e o respetivo processo de revisão e auditoria.

As definições oficiais encontram-se nos documentos `m3-definition-of-ready-v02.pdf` e `m1-definition-of-done-v01.pdf`.

A correspondência entre as etiquetas de estado do GitHub e os estados oficiais das tarefas (Pendente, Em Execução, Em Revisão, Em Auditoria de Qualidade, Pronta para Sprint Review e Done) é a definida na Definition of Done (secção 4) e no Regulamento Interno (secção 9.2).

### 3.2. Checklist de Controlo de Artefactos

A Checklist de Controlo de Artefactos (CCA) é utilizada para acompanhar os artefactos e compromissos de entrega do projeto. A CCA deve:

- identificar os artefactos previstos;
- permitir acompanhar o respetivo estado;
- manter a rastreabilidade das entregas;
- ser atualizada ao longo do projeto;
- ser arquivada de acordo com a estrutura documental definida pelo grupo.

A versão final da CCA é exportada para PDF e assinada, de acordo com as regras de entrega da UC.

### 3.3. Qualidade dos Requisitos

Os requisitos são avaliados de acordo com os Critérios de Qualidade dos Requisitos definidos pelo grupo no ficheiro `m3-criterios-qualidade-requisitos-v02.md`. A avaliação permite verificar, quando aplicável: correção, clareza, completude, consistência, necessidade, viabilidade, verificabilidade, rastreabilidade, singularidade, ausência de ambiguidade e independência da solução.

A Checklist de Aprovação dos Requisitos utiliza obrigatoriamente o template oficial e contém uma linha por requisito, identificada pelo ID e pelo título existentes no Documento de Especificação de Requisitos, e uma coluna independente para cada critério de qualidade definido pelo grupo. Cada célula indica `Cumpre`, `Não cumpre — <justificação objetiva>` ou `Não aplicável — <justificação>`. A coluna Resultado Final utiliza exclusivamente `Aprovado` ou `Não aprovado`, e um requisito só é considerado aprovado quando o resultado final for `Aprovado`.

Uma tarefa que implemente requisitos só fica Ready com esses requisitos aprovados (critério R16 da DoR).

A Requirements Traceability Matrix (RTM) utiliza obrigatoriamente o template oficial, é cumulativa e é entregue em `m2`, `m3` e `m4`. Contém uma linha por requisito e preserva no histórico os requisitos descontinuados.

Todos os diagramas são produzidos em Mermaid (ou tecnologia equivalente que preserve uma representação textual, editável e versionável), com o código-fonte incluído no ficheiro Markdown. Não é suficiente entregar apenas uma imagem ou captura.

Os resultados devem ser registados nos artefactos de qualidade correspondentes.

### 3.4. Gestão de Não Conformidades

As não conformidades e problemas identificados no processo devem ser registados e acompanhados através das ferramentas de gestão do projeto.

No GitHub, as Issues utilizam as etiquetas existentes no projeto: Administration, Analysis, Architecture, Backlog, Blocked, Database, Development, Documentation, Management, Quality, Testing, Ready, In-Progress, In-Review, In-Audit, Ready-for-Sprint-Review e Done.

Não é utilizada uma etiqueta `incumprimento`, uma vez que esta não pertence ao conjunto de etiquetas definido pelo grupo.

As ações corretivas devem ser acompanhadas através das Issues correspondentes e das evidências produzidas durante a sua resolução.

### 3.5. Equilíbrio da Carga

O grupo acompanha a distribuição das tarefas através da Carga das Tarefas Planeadas (CTP). A distribuição deve respeitar os limites definidos no Regulamento de Funcionamento da UC e ser monitorizada nos momentos de avaliação previstos.

## 4. Integração Contínua e Quality Gates

A integração contínua é realizada através do GitHub Actions, utilizando o ficheiro `.github/workflows/backend-ci.yml`. O GitLab da ESTG ficou indisponível e o grupo passou as Issues, o repositório e a pipeline para o GitHub (decisão G13).

A branch `main` não deve receber alterações diretamente. A integração de alterações deve ocorrer através do processo de Pull Request (o equivalente, no GitHub, ao Merge Request do Regulamento), respeitando o seguinte fluxo:

1. o Executor cria o Pull Request na branch apropriada, liga-o à Issue, identifica o Revisor e envia a notificação formal;
2. a pipeline é executada e os testes, a análise estática e os quality gates aplicáveis são cumpridos;
3. o Revisor analisa o trabalho, produz as evidências e entrega a revisão ao Auditor;
4. o Auditor audita e, se estiver conforme, regista `AUTORIZAÇÃO DE ACEITAÇÃO`;
5. o Revisor aceita, aprova e executa o merge na `main`, só depois da autorização do Auditor;
6. o Revisor regista `ACEITAÇÃO FINAL CONCLUÍDA` e devolve a tarefa ao Auditor;
7. o Auditor confirma o merge, a versão final e a ausência de alterações não revistas;
8. o Auditor regista `AUDITORIA FINAL CONCLUÍDA` e move a tarefa para `Ready-for-Sprint-Review`.

O Executor nunca aprova, aceita nem integra o seu próprio Pull Request. Para artefactos que não sejam código aplica-se um fluxo equivalente de revisão, autorização, aceitação e confirmação.

### 4.1. Quality Gates

Os quality gates do grupo são os definidos na secção 11.6 do Regulamento Interno. Em `m3` estão implementados os do backend; os de frontend entram na pipeline em `m4`.

| Código | Componente | Verificação | Ferramenta ou regra | Critério ou limiar |
| --- | --- | --- | --- | --- |
| QG-01 | Backend, frontend web e frontend mobile | Construção ou compilação | .NET CLI, npm e Gradle | Todos os componentes compilam/constroem sem erros |
| QG-02 | Backend | Testes unitários | xUnit | 100% dos testes unitários passam |
| QG-03 | Backend | Testes de integração | xUnit, WebApplicationFactory, SQLite em memória e SQL Server real em contentor | 100% dos testes de integração passam, nas duas bases de dados |
| QG-04 | Backend | Cobertura de testes | Coverlet (XPlat Code Coverage) | Cobertura de linhas ≥ 80%, testes unitários e de integração, sem migrations |
| QG-05 | Backend, frontend web e frontend mobile | Linter ou análise estática | Analisadores .NET (`AnalysisMode=Recommended`) e `dotnet format style` / ESLint / detekt (ktlint) | Ausência de erros bloqueantes |
| QG-06 | Frontend web | Testes de sistema automáticos | Playwright | 100% dos testes de sistema obrigatórios passam |
| QG-07 | Frontend mobile | Testes de sistema automáticos | Espresso / UI Automator | 100% dos testes de sistema obrigatórios passam |
| QG-08 | Frontend web | Testes unitários e de componentes | Jasmine + Angular TestBed | 100% dos testes obrigatórios passam |
| QG-09 | Frontend web | Cobertura de testes | Karma Coverage (Istanbul) | Cobertura de linhas ≥ 80% |
| QG-10 | Frontend mobile | Testes unitários | JUnit | 100% dos testes obrigatórios passam |
| QG-11 | Frontend mobile | Cobertura de testes | JaCoCo | Cobertura de linhas ≥ 80% |
| QG-12 | Backend, frontend web e frontend mobile | Formatação de código | `dotnet format whitespace --verify-no-changes` / `Prettier --check` / `./gradlew ktlintCheck` | 100% do código cumpre a formatação definida |

A pipeline do backend verifica ainda que não existem pacotes NuGet com vulnerabilidades conhecidas (`dotnet list package --vulnerable --include-transitive`).

O incumprimento de qualquer quality gate faz a pipeline falhar e bloqueia a integração. Os quality gates devem ser implementados de forma que uma verificação obrigatória não seja reduzida a um simples aviso quando o seu cumprimento for condição de integração.

### 4.2. Funcionamento correto da pipeline e evidências

Em `m3` e `m4`, a pipeline deve executar automaticamente os testes e a análise estática aplicáveis e verificar os quality gates. Considera-se que a pipeline funciona corretamente quando conclui todas as etapas obrigatórias numa execução válida e bloqueia a integração ou a entrega quando existe um erro de compilação, falha um teste, falha a análise estática ou não é atingido um quality gate.

As evidências das execuções bem-sucedidas e dos bloqueios provocados por erros devem ficar disponíveis no GitHub e preservadas no ZIP submetido, arquivadas em `04-artefactos-tecnicos/04.16-ci-cd`.

## 5. Processo de Revisão e Auditoria

Cada tarefa sujeita a avaliação interna deve respeitar a sequência definida pelo grupo:

> Executor → Revisor → Auditor → Revisor (aceitação) → Auditor (confirmação final) → Sprint Review

**O Executor:**

- executa a tarefa;
- produz as evidências;
- verifica a própria conformidade antes de solicitar revisão;
- nunca aprova, aceita nem integra o seu próprio Pull Request.

**O Revisor:**

- verifica o resultado produzido;
- verifica os critérios de aceitação;
- verifica a Definition of Done e os quality gates aplicáveis;
- regista os problemas encontrados;
- solicita correções quando necessário, registando `REVISÃO REJEITADA`;
- depois da autorização do Auditor, aceita o resultado (e efetua o merge, no código) e regista `ACEITAÇÃO FINAL CONCLUÍDA`.

**O Auditor:**

- verifica o processo de revisão;
- verifica a independência dos papéis;
- confirma a existência das evidências;
- confirma a rastreabilidade;
- regista `AUDITORIA REJEITADA` quando exige correções;
- regista `AUTORIZAÇÃO DE ACEITAÇÃO` quando os critérios estão cumpridos;
- depois da aceitação, confirma a versão final e regista `AUDITORIA FINAL CONCLUÍDA`.

A tarefa apenas deve avançar para `Ready-for-Sprint-Review` depois de concluídas as verificações necessárias, e só passa a `Done` depois da aprovação no Sprint Review.

## 6. Gestão da Qualidade no GitHub

As Issues constituem o registo operacional das tarefas e devem seguir o modelo definido no Regulamento Interno. Uma Issue deve conter, quando aplicável:

- descrição;
- objetivo;
- tipo da tarefa;
- resultado esperado;
- critérios de aceitação;
- requisitos implementados (critério R16 da DoR);
- evidências do Executor;
- verificações do Revisor;
- checklist do Auditor;
- dependências;
- papéis e pontuação;
- Definition of Done;
- referências;
- pasta de destino.

As Issues utilizam as etiquetas definidas no GitHub para identificar a área da tarefa e o respetivo estado. Exemplo: `Development` + `In-Progress`, `Documentation` + `In-Review` ou `Testing` + `In-Audit`.

A numeração das Issues é utilizada como identificador da tarefa, sendo referenciada nas branches, commits e Pull Requests quando aplicável.

## 7. Evidências e Arquivo

As evidências de qualidade devem ser mantidas de forma rastreável e organizada. Podem constituir evidência:

- Issues;
- Pull Requests;
- resultados de pipelines, incluindo os bloqueios provocados por erros (em m3 e m4);
- relatórios de testes;
- relatórios de cobertura;
- relatórios de revisão;
- checklists de auditoria;
- atas;
- documentos oficiais;
- ficheiros XLS/XLSX;
- PDFs finais;
- restantes artefactos previstos no projeto.

Os artefactos devem ser arquivados na estrutura de pastas definida pelo Regulamento de Funcionamento da UC (secção 19) e pelo Regulamento Interno do Grupo.

## 8. Documentos e Instrumentos Relacionados

| Documento | Ficheiro | Relação com este Plano |
| --- | --- | --- |
| Regulamento Interno do Grupo | `m3-regulamento-grupo-v02.pdf` | Define as regras internas, papéis, organização e processos do grupo |
| Definition of Ready | `m3-definition-of-ready-v02.pdf` | Define os critérios necessários para uma tarefa estar pronta |
| Definition of Done | `m1-definition-of-done-v01.pdf` | Define os critérios necessários para considerar uma tarefa concluída |
| Critérios de Qualidade dos Requisitos | `m3-criterios-qualidade-requisitos-v02.md` | Define os critérios aplicáveis à qualidade dos requisitos |
| Checklist de Controlo de Artefactos | `m3-checklist-controlo-artefactos-v05.xlsx` / PDF | Permite acompanhar os compromissos e artefactos de entrega |
| Grelha de Rotação de Papéis | `m3-grelha-rotacao-papeis-v<versao>.xlsx` | Regista a distribuição dos papéis de Executor, Revisor, Auditor e Secretário |
| Issues do GitHub | `i<número>` | Registam operacionalmente as tarefas e respetivas evidências |
| Pipeline CI/CD | `.github/workflows/backend-ci.yml` | Automatiza as verificações e quality gates |

## 9. Revisão do Plano

Este Plano de Qualidade deve ser revisto sempre que exista uma alteração relevante:

- nas tecnologias utilizadas;
- no processo de desenvolvimento;
- nas ferramentas de qualidade;
- nos critérios de testes;
- nos quality gates;
- na estrutura documental;
- no Regulamento Interno do Grupo.

Qualquer alteração deve ser registada no histórico de versões e submetida ao processo de aprovação definido pelo grupo. A versão aprovada constitui a referência aplicável ao grupo até à aprovação de uma nova versão.
