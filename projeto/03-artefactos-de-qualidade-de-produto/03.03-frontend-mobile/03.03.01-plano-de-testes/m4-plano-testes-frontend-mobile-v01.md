# Plano de testes do frontend mobile

| Campo | Informação |
| --- | --- |
| **Projeto** | G15 — Campus Carpool |
| **Artefacto** | `m4-plano-testes-frontend-mobile-v01.md` |
| **Milestone / Sprint** | m4 / Sprint 07 |
| **Issue de origem** | I090 |
| **Pasta de destino** | `/projeto/03-artefactos-de-qualidade-de-produto/03.03-frontend-mobile/03.03.01-plano-de-testes` |
| **Versão** | v01, 10/11/2026 |

## 1. Objetivo

Definir o âmbito, a estratégia, as ferramentas, o ambiente, os dados, as responsabilidades e os critérios de entrada e de saída dos testes do frontend mobile. Este plano orienta a especificação (I091, I093), a implementação (I092) e o registo dos resultados (I098, I100). Não contém cenários de teste: esses pertencem às especificações.

O frontend web tem plano próprio (`m4-plano-testes-frontend-web-v01.md`), porque cada aplicação é tratada autonomamente para efeitos de qualidade (Regulamento da UC, secção 17).

## 2. Âmbito

**Em âmbito:** a aplicação Android em `/projeto/04-artefactos-tecnicos/04.15-frontend-mobile/m4-v01`, integrada com a API do backend, nas funcionalidades com componente mobile da Especificação de Requisitos v02.

| Funcionalidade | Conteúdo | Issue |
| --- | --- | --- |
| F001 | Registo, confirmação, sessão, perfil próprio e de outros | I078 |
| F002 | Criação e gestão de viagens | I079 |
| F003, F008 | Pesquisa, mapa, rota e custo | I080 |
| F004 | Pedidos de participação e simulação | I081 |
| F005 | Mensagens e avisos | I082 |
| F006 | Notificações, tempo real e push (FCM) | I083 |
| F007, F009 | Avaliações e histórico | I084 |

**Requisitos não funcionais verificados por este plano:** RNF-COM-02 (Android 10 ou superior), RNF-COM-03 (mesma API), RNF-FIA-04 (datas na hora de Portugal), RNF-USA-02 (mensagens de erro) e RNF-MAN-02 e 03 (quality gate e bloqueio pela pipeline).

**Fora de âmbito:** o backend, verificado em m3 pelo seu plano; o frontend web; testes de carga, de desempenho e de consumo de bateria; versões anteriores ao Android 10; a receção real de email, substituída por uma leitura do token no ambiente de teste, exceto na simulação de aceitação em ambiente público.

## 3. Níveis de teste

| Nível | Obrigatório | Objetivo | Ferramenta | Especificação | Resultados |
| --- | --- | --- | --- | --- | --- |
| Unitário | Não; recomendado e assumido no projeto desde a I077 | Núcleo (repositórios, sessão, validações, contrato da API), ViewModels e ecrãs, com a API simulada | JUnit, MockWebServer e Robolectric | Testes em `core/src/test` e `app/src/test` | `03.03.04-testes-unitarios` e `03.03.05-cobertura-de-testes` |
| Sistema automático | Sim | Fluxos da aplicação em emulador ou dispositivo, integrada com o backend | Compose UI Test e Espresso | I091, em `03.03.02-testes-de-sistema` | I098, na mesma pasta |
| Aceitação (simulação) | Sim | Confirmar com participantes que o produto serve o cliente | Execução manual e guiada, em dispositivos | I093, em `03.03.03-testes-de-aceitacao` | I100, na mesma pasta |
| Análise estática | Sim | Regras de código e quality gate | Android Lint e ktlint, configurados na I077; a I094 fecha o quality gate | I094 | `03.03.06-analise-estatica` |

### 3.1. Testes de sistema automáticos e simulação de aceitação

| | Sistema automático | Simulação de aceitação |
| --- | --- | --- |
| **Pergunta** | A aplicação comporta-se como especificado? | O produto serve quem o vai usar? |
| **Quem executa** | A pipeline e o Executor, sem intervenção humana durante a execução | Participantes, seguindo cenários, com um observador do grupo |
| **Origem dos cenários** | Requisitos e casos de uso | Âmbito acordado e necessidades do cliente |
| **Resultado** | Passou ou falhou, repetível, com registo da execução | Critérios cumpridos ou não, com as observações dos participantes |
| **Ambiente** | Emulador local e pipeline | Dispositivos reais com o ambiente público, depois da I102, ou a alternativa declarada no relatório |
| **Evidência** | Relatório de execução e versão testada | Relatório da simulação, com participantes, datas, dispositivos e resultados |

Um teste automático a passar não substitui a simulação de aceitação, nem o contrário.

### 3.2. Testes unitários e cobertura

Não são obrigatórios em m4, mas são recomendados (Regulamento da UC, secção 17), e o projeto assumiu-os desde a I077. O limiar é de **80 % das linhas**, igual ao do frontend web, medido com JaCoCo em cada módulo: `core`, com `:core:check`, e `app`, com `:app:verificarCoberturaApp`. Os resultados são arquivados em `03.03.04` e `03.03.05`.

Até à I079, só o `core` e os ViewModels correram. Os testes de ecrã em JVM (Robolectric), a cobertura de `app` e os testes em dispositivo (`app/src/androidTest`) continuam por executar (secção 10).

## 4. Ferramentas

| Finalidade | Ferramenta | Nota |
| --- | --- | --- |
| Aplicação | Kotlin, Jetpack Compose, Retrofit, Android 10 ou superior (API 29) | Na I077: Kotlin 2.2.21, Compose BOM 2025.10.01, Retrofit 2.11.0, `compileSdk` e `targetSdk` 36 |
| Testes de sistema | Compose UI Test e Espresso, com AndroidJUnitRunner | Tecnologia equivalente ao Selenium para Android, como o Regulamento admite. A I092 pode propor outra, por exemplo Appium, se a justificar |
| Unitários e cobertura | JUnit 4.13.2, MockWebServer 4.12.0, Robolectric 4.16; JaCoCo 0.8.13 | Ponto 3.2 |
| Análise estática | Android Lint; ktlint (motor 1.5.0) | Configurados na I077. O Regulamento Interno, §11.4, admite detekt ou ktlint. A I094 fecha o quality gate |
| Compilação | Gradle 8.13, JDK 21, Android SDK (plataforma 36) e Android Gradle Plugin 8.13.2 | Instruções no `README.md` do projeto |
| Pipeline | GitHub Actions | `frontend-mobile-ci.yml`, criado na I077, corre build, testes, cobertura e análise estática. A I095 acrescenta os testes de sistema |
| Defeitos | Registo em `03.03.07-defeitos` | `m4-registo-defeitos-frontend-mobile-v01.xlsx` |

## 5. Ambiente

| Ambiente | Aplicação | Backend e dados | Utilização |
| --- | --- | --- | --- |
| **E1 Local** | Emulador Android | API real no computador de desenvolvimento, acedida pelo emulador em `http://10.0.2.2:5100`, com SQL Server de teste ou SQLite em memória | Desenvolvimento dos testes |
| **E2 Pipeline** | Emulador em GitHub Actions | API e SQL Server em contentor | Execução em cada integração, depois da I095 |
| **E3 Público** | APK instalado em dispositivos reais | API pública, depois de corrigidos os pendentes U2, U3, U4 e U22 | Simulação de aceitação e verificação final |

**Versões (RNF-COM-02):** no mínimo, Android 10 (API 29) e a versão estável mais recente disponível no emulador. O relatório regista a versão e o dispositivo de cada execução.

**Configuração:** o endereço da API define-se na compilação, sem alterar o código-fonte, pela propriedade Gradle `campusApiUrl` ou pela variável `CAMPUS_API_URL`, e tem de terminar em `/api/v1/`. Em dispositivo físico ligado por USB, usa-se `adb reverse tcp:5100 tcp:5100`. Nenhuma chave, palavra-passe, token ou `google-services.json` real entra no repositório, que é público. O tráfego HTTP sem TLS, necessário em E1, só é aceite na variante debug, para `10.0.2.2`, `127.0.0.1` e `localhost`.

## 6. Dados de teste

- Contas fictícias, com domínio académico aceite pelo backend (por exemplo `@estg.ipp.pt`) e sem dados pessoais reais.
- Cada teste automático cria os seus dados e não depende da ordem de execução.
- Papéis necessários: conta por confirmar, condutor confirmado, passageiro confirmado e um terceiro utilizador sem relação com a viagem.
- Viagens futuras, concluídas, indisponibilizadas e sem lugares. As datas são relativas ao instante da execução e enviadas em UTC com `Z`.
- Datas de inverno e de verão, com o dispositivo noutro fuso, para confirmar que a interface usa a hora de Portugal (RNF-FIA-04).
- O token de confirmação é obtido pelo serviço de email substituído no ambiente de teste, e nunca em ambiente público.
- Dados nos limites: nome, curso e instituição no tamanho máximo, URL longo, 5 ligações externas.

**Limitadores da API:** 10 pedidos por minuto e por IP em login, registo e reenvio, e 100 por minuto nos restantes. A I092 deve reutilizar sessões ou desligar o limitador na configuração de teste, e declará-lo no relatório. Não pode escrever testes que dependam da janela do limitador.

## 7. Responsabilidades

| Atividade | Issue | Executor / Revisor / Auditor |
| --- | --- | --- |
| Plano de testes mobile | I090 | Vítor Lopes / Gil Leão / Cristiana Macedo |
| Especificação dos testes de sistema | I091 | Gil Leão / Cristiana Macedo / Joana Rodrigues |
| Testes unitários | I077 a I084 | Executor de cada issue |
| Implementação dos testes de sistema | I092 | A atribuir no Sprint Planning |
| Especificação dos testes de aceitação | I093 | A atribuir no Sprint Planning |
| Análise estática e relatório | I094 | A atribuir no Sprint Planning |
| Integração na pipeline | I095 | A atribuir no Sprint Planning |
| Resultados dos testes de sistema | I098 | A atribuir no Sprint Planning |
| Simulação de aceitação | I100 | A atribuir no Sprint Planning |

Cada defeito é registado por quem o encontra e corrigido pelo Executor da funcionalidade. O teste de confirmação é feito por quem o registou.

## 8. Critérios de entrada

**Comuns:** a tarefa cumpre a Definition of Ready, e a funcionalidade a testar está integrada na branch de teste, com o projeto a compilar e a análise estática a passar.

| Nível | Critérios adicionais |
| --- | --- |
| Sistema automático | Especificação I091 revista; I077 a I084 integradas, para os fluxos respetivos; API disponível, com a base de dados migrada; dados de teste criados; emulador Android 10 ou superior disponível |
| Simulação de aceitação | Especificação I093 revista; testes de sistema executados, sem defeitos de severidade alta abertos; ambiente E3 disponível, ou alternativa declarada; APK instalável; participantes e dispositivos identificados |
| Análise estática | Configuração da I094 no projeto |

## 9. Critérios de saída

1. **0 testes falhados** nas execuções de sistema e nas unitárias. Um teste saltado só é aceite com motivo registado.
2. **Cobertura de linhas de pelo menos 80 %,** em `core` e em `app`.
3. **Cada fluxo principal** do glossário da Especificação de Requisitos tem cenário de sistema a passar: registo e confirmação, início de sessão, criar viagem, pesquisar viagens, pedir participação, aceitar ou rejeitar um pedido, enviar mensagem, consultar notificações, avaliar e consultar o histórico.
4. **RNF-COM-02:** os fluxos principais passam em Android 10 ou superior, com versões e dispositivos registados.
5. **Quality gate de análise estática cumprido.** A P-29 deixou os frontends por definir em m4; a I094 fecha-o, e o mínimo proposto é compilação sem erros e 0 erros no Android Lint e no ktlint.
6. **Pipeline verde** no commit entregue, e bloqueio demonstrado quando um teste ou o quality gate falham (RNF-MAN-03).
7. **Simulação de aceitação concluída,** com cada critério avaliado e as observações registadas.
8. **Defeitos registados** em `03.03.07`, com severidade e estado, e nenhum de severidade alta aberto sem decisão registada.
9. **Rastreabilidade:** cada cenário indica os requisitos que verifica, e a RTM é atualizada na I104.

## 10. Riscos e limitações

| Risco | Efeito | Resposta |
| --- | --- | --- |
| I077 a I079 ainda por publicar, com testes de ecrã, cobertura de `app` e testes em dispositivo por executar | Cobertura e RNF-COM-02 por confirmar | Executar na pipeline, no primeiro push, e num dispositivo ou emulador, antes da I098 |
| Emulador lento ou instável na pipeline | Tempos longos e falsos negativos | Definir na I095 o subconjunto de cenários a correr na pipeline; o conjunto completo corre localmente e fica registado |
| Ligação de confirmação aberta no telemóvel | O email aponta para `Frontend:ConfirmUrl` | A I078 aceita a ligação `campuscarpool://confirmar-conta?token=…` e o token colado na aplicação. A I091 especifica os dois caminhos |
| Push depende do Firebase | Sem projeto real, não há entrega real | Serviço substituído nos testes automáticos; entrega real verificada em E3, se configurado |
| Ambiente público com pendentes U2, U3, U4 e U22 | A simulação não corre em E3 | Corrigir antes da I102; alternativa em E1, declarada |
| Sem geometria da rota no backend (DEF-BE-054) | O mapa da I080 fica limitado | Decidir na I080 e refletir na especificação |
| Defeitos do hub (DEF-BE-055, 056) e do reenvio (DEF-BE-045, 046) | Comportamentos a contornar nos testes | Cenários indicam o defeito conhecido |
| SQLite em memória difere do SQL Server | Resultados locais incompletos | Executar em SQL Server na pipeline, E2 |

## 11. Documentos relacionados

Regulamento da UC (secções 8, 14, 15, 17 e 19); Proposta de Sistema v03; Declaração de Âmbito v03; Especificação de Requisitos v02; Regulamento Interno do Grupo; `m3-plano-testes-backend-v01.md`; modelo de classes do frontend mobile; protótipo de baixa fidelidade; `m4-backlog-projeto-v08`.
