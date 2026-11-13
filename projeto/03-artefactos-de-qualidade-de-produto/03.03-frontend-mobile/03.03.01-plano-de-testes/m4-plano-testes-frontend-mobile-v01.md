# Plano de testes do frontend mobile

| Campo | Informação |
| --- | --- |
| **Projeto** | G15 — Campus Carpool |
| **Artefacto** | `m4-plano-testes-frontend-mobile-v01.md` |
| **Milestone / Sprint** | m4 / Sprint 07 |
| **Issue de origem** | I090 |
| **Pasta de destino** | `/projeto/03-artefactos-de-qualidade-de-produto/03.03-frontend-mobile/03.03.01-plano-de-testes` |
| **Versão** | v01, 10/11/2026; revista no mesmo dia (secção 13) |

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

**Requisitos não funcionais com componente mobile** (coluna «Âmb.» com M na Especificação de Requisitos v02):

| Requisito | Como se verifica | Onde |
| --- | --- | --- |
| RNF-COM-02 — Android 10 ou superior | Teste: os fluxos principais em Android 10 (API 29) e na versão estável mais recente | Especificação I091 (TSM-065), resultados I098 |
| RNF-FIA-04 — datas na hora de Portugal | Teste, com o dispositivo noutro fuso | TSM-064 |
| RNF-SEG-12 — conteúdo dos utilizadores como texto (XSS) | Teste, com os payloads da secção 6 | TSM-059 |
| RNF-USA-01 e RNF-USA-02 — português europeu e erros com a causa e a correção | Inspeção e demonstração, apoiadas por um cenário de sistema | TSM-062 |
| RNF-PRI-02 — sem localização contínua | Inspeção das permissões e do comportamento | TSM-061 |
| RNF-SEG-08 — sem credenciais no código nem no APK | Inspeção do repositório e do APK | TSM-058 |
| RNF-SEG-01 — HTTPS | Inspeção; tráfego sem TLS só na variante debug, em E1 | TSM-057 |
| RNF-COM-03 — mesma API que o web | Inspeção do código | TSM-057 |
| RNF-MAN-02 e RNF-MAN-03 — quality gate e bloqueio pela pipeline | Teste e demonstração | TSM-066; I094 e I095 |

**Fora de âmbito:** o backend, verificado em m3 pelo seu plano; o frontend web; testes de carga, de desempenho e de consumo de bateria; versões anteriores ao Android 10; a receção real de email, substituída por uma leitura do token no ambiente de teste, exceto na simulação de aceitação em ambiente público.

## 3. Níveis de teste

| Nível | Obrigatório | Objetivo | Ferramenta | Especificação | Resultados |
| --- | --- | --- | --- | --- | --- |
| Unitário | Não (Regulamento da UC, secção 17); assumido no Plano de Qualidade v02 (QG-10 e QG-11) e no projeto desde a I077 | Núcleo (repositórios, sessão, validações, contrato da API), ViewModels e ecrãs, com a API simulada | JUnit, MockWebServer e Robolectric | Testes em `core/src/test` e `app/src/test` | `03.03.04-testes-unitarios` e `03.03.05-cobertura-de-testes` |
| Sistema automático | Sim | Fluxos da aplicação em emulador ou dispositivo, integrada com o backend | Compose UI Test, Espresso e UI Automator (secção 4) | I091, em `03.03.02-testes-de-sistema` | I098, na mesma pasta |
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

Não são obrigatórios em m4, mas são recomendados (Regulamento da UC, secção 17), e o Plano de Qualidade v02 assume-os como quality gates: QG-10 (todos os testes passam) e QG-11 (cobertura de pelo menos **80 % das linhas**, igual ao frontend web). O projeto aplica-os desde a I077, com JaCoCo em cada módulo: `core`, com `:core:check`, e `app`, com `:app:verificarCoberturaApp`. Os resultados são arquivados em `03.03.04` e `03.03.05`. A situação no fim do Sprint 07, com o que ficou por executar, está na secção 11.

## 4. Ferramentas

| Finalidade | Ferramenta | Nota |
| --- | --- | --- |
| Aplicação | Kotlin, Jetpack Compose, Retrofit, Android 10 ou superior (API 29) | Na I077: Kotlin 2.2.21, Compose BOM 2025.10.01, Retrofit 2.11.0, `compileSdk` e `targetSdk` 36 |
| Testes de sistema | Compose UI Test e Espresso dentro da aplicação; UI Automator fora dela, com AndroidJUnitRunner | O UI Automator cobre o que acontece fora da aplicação: a ligação de confirmação aberta noutra aplicação, a barra de notificações do push e os diálogos do sistema. São as ferramentas do QG-07 do Plano de Qualidade v02 e a tecnologia equivalente ao Selenium para Android, como o Regulamento admite. A I092 pode propor outra, por exemplo Appium, se a justificar |
| Unitários e cobertura | JUnit 4.13.2, MockWebServer 4.12.0, Robolectric 4.16; JaCoCo 0.8.13 | Ponto 3.2 |
| Análise estática | Android Lint; ktlint (motor 1.5.0) | Configurados na I077. O Regulamento Interno, §11.4, admite detekt ou ktlint. A I094 fecha o quality gate |
| Compilação | Gradle 8.13, JDK 21, Android SDK (plataforma 36) e Android Gradle Plugin 8.13.2 | Instruções no `README.md` do projeto |
| Pipeline | GitHub Actions | `frontend-mobile-ci.yml`, criado na I077: `:core:check`, ktlint, testes e cobertura da app, Android Lint e os dois APK. Ainda não corre testes em emulador; a I095 acrescenta os testes de sistema |
| Defeitos | Registo em `03.03.07-defeitos` | `m4-registo-defeitos-frontend-mobile-v01.xlsx` |

## 5. Ambiente

| Ambiente | Aplicação | Backend e dados | Utilização |
| --- | --- | --- | --- |
| **E1 Local** | Emulador Android | API de teste no computador de desenvolvimento, acedida pelo emulador em `http://10.0.2.2:5100`, com SQL Server de teste ou SQLite em memória | Desenvolvimento dos testes |
| **E2 Pipeline** | Emulador em GitHub Actions | API de teste e SQL Server em contentor | Execução em cada integração, depois da I095 |
| **E3 Público** | APK instalado em dispositivos reais | API pública, depois de corrigidos os pendentes U2, U3, U4 e U22 | Simulação de aceitação e verificação final |

**API de teste (E1 e E2).** É o `Program.cs` real, com JWT, CORS e limitador reais, mas com os serviços de email, push e mapas substituídos e com acesso de teste à base de dados, como o `AplicacaoReal` dos testes de integração do backend (`CampusCarpool/tests/CampusCarpool.Tests/Integration/AplicacaoReal.cs`). É necessária por duas razões: sem a Brevo configurada, a API real responde 503 ao registo e deixa a conta por confirmar; e as operações auxiliares da especificação alteram datas na base de dados (secção 6). Nunca se usa em E3. A I092 define como a arranca em E2, e a I095 integra-a na pipeline.

**Versões (RNF-COM-02):** no mínimo, Android 10 (API 29) e a versão estável mais recente disponível no emulador. O relatório regista a versão e o dispositivo de cada execução.

**Configuração:** o endereço da API define-se na compilação, sem alterar o código-fonte, pela propriedade Gradle `campusApiUrl` ou pela variável `CAMPUS_API_URL`, e tem de terminar em `/api/v1/`. Em dispositivo físico ligado por USB, usa-se `adb reverse tcp:5100 tcp:5100`. Nenhuma chave, palavra-passe, token ou `google-services.json` real entra no repositório, que é público. O tráfego HTTP sem TLS, necessário em E1, só é aceite na variante debug, para `10.0.2.2`, `127.0.0.1` e `localhost`.

## 6. Dados de teste

- Contas fictícias, com domínio académico aceite pelo backend (por exemplo `@estg.ipp.pt`) e sem dados pessoais reais.
- Cada teste automático cria os seus dados e não depende da ordem de execução.
- Papéis necessários: conta por confirmar, condutor confirmado, passageiro confirmado e um terceiro utilizador sem relação com a viagem.
- Viagens futuras, já partidas, concluídas, indisponibilizadas e sem lugares. As datas são relativas ao instante da execução e enviadas em UTC com `Z`.
- **O backend não tem relógio controlável** e não aceita partidas no passado. Viagens já partidas ou concluídas, ligações de confirmação expiradas e o intervalo do reenvio preparam-se com operações auxiliares que alteram datas na base de dados da API de teste, só em E1 e E2. A especificação I091 define-as (AUX-01 a AUX-13).
- Datas de inverno e de verão, com o dispositivo noutro fuso, para confirmar que a interface usa a hora de Portugal (RNF-FIA-04).
- O token de confirmação é obtido pelo serviço de email substituído na API de teste, e nunca em ambiente público.
- Dados nos limites: nome, curso e instituição no tamanho máximo, URL longo, 5 ligações externas.

**Payloads de XSS (RNF-SEG-12).** O requisito remete para este plano. Cada payload é guardado pela API nos campos de texto do perfil, na origem e no destino das viagens, nas mensagens e nos avisos, dentro do tamanho máximo de cada campo, e depois apresentado a outro utilizador. A aplicação não usa `WebView`; o teste confirma que nada é interpretado como HTML:

| N.º | Payload | Resultado esperado |
| --- | --- | --- |
| X1 | `<script>alert(1)</script>` | Aparece como texto, sem diálogo |
| X2 | `<img src=x onerror=alert(1)>` | Aparece como texto, sem imagem nem diálogo |
| X3 | `"><svg onload=alert(1)>` | Aparece como texto |
| X4 | `<b>negrito</b>` | Aparece com as etiquetas, sem negrito |
| X5 | `javascript:alert(1)`, como endereço de uma ligação externa | Recusado pela aplicação e pela API, que só aceitam `http://` e `https://` |

**Limitadores da API:** 10 pedidos por minuto e por IP em login, registo e reenvio, e 100 por minuto nos restantes. Os limites estão fixos no código (`LimitesPedidos`) e **não se desligam por configuração**. O emulador e as operações auxiliares saem do mesmo IP, por isso a I092 cria as contas na preparação, de preferência diretamente na base de dados da API de teste, e reutiliza sessões. Nenhum teste pode depender da janela do limitador; só o cenário próprio do RNF-SEG-13 os exercita, num ambiente isolado. O relatório declara a estratégia usada.

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
| Sistema automático | Especificação I091 revista; I077 a I084 integradas, para os fluxos respetivos; API de teste disponível (secção 5), com a base de dados migrada; dados de teste criados; emulador Android 10 ou superior disponível |
| Simulação de aceitação | Especificação I093 revista; testes de sistema executados, sem defeitos de severidade alta abertos; ambiente E3 disponível, ou alternativa declarada; APK instalável; participantes e dispositivos identificados |
| Análise estática | Configuração da I094 no projeto |

## 9. Critérios de saída

1. **0 testes falhados** nas execuções de sistema e nas unitárias. Um teste saltado só é aceite com motivo registado.
2. **Cobertura de linhas de pelo menos 80 %,** em `core` e em `app` (QG-11).
3. **Cada fluxo principal** do glossário da Especificação de Requisitos tem cenário de sistema a passar: registo e confirmação, início de sessão, criar viagem, pesquisar viagens, pedir participação, aceitar ou rejeitar um pedido, enviar mensagem, consultar notificações, avaliar e consultar o histórico.
4. **RNF-COM-02:** os fluxos principais passam em Android 10 ou superior, com versões e dispositivos registados.
5. **Quality gates cumpridos.** Os do Plano de Qualidade v02 para o frontend mobile são QG-01 (compilação sem erros), QG-05 (Android Lint e ktlint sem erros), QG-07 (testes de sistema), QG-10 e QG-11 (unitários e cobertura) e QG-12 (formatação com `ktlintCheck`). A pipeline atual já verifica QG-01, QG-05, QG-10, QG-11 e QG-12; o QG-07 entra com a I095. A P-29 deixou o quality gate de análise estática dos frontends por definir em m4; a I094 fecha-o, e o mínimo proposto é compilação sem erros e 0 erros no Android Lint e no ktlint.
6. **Pipeline verde** no commit entregue, e bloqueio demonstrado quando um teste ou o quality gate falham (RNF-MAN-03).
7. **Simulação de aceitação concluída,** com cada critério avaliado e as observações registadas.
8. **Defeitos registados** em `03.03.07`, com severidade e estado, e nenhum de severidade alta aberto sem decisão registada.
9. **Rastreabilidade:** cada cenário indica os requisitos que verifica, e a RTM é atualizada na I104.

## 10. Riscos e limitações

| Risco | Efeito | Resposta |
| --- | --- | --- |
| I077 a I079 ainda por publicar, com testes em dispositivo por executar e, na I078 e na I079, testes de ecrã e cobertura de `app` por executar | Cobertura e RNF-COM-02 por confirmar | A pipeline corre os testes de ecrã e a cobertura no primeiro push; os testes em dispositivo correm num telemóvel ou emulador antes da aceitação de cada issue (secção 11) |
| Emulador lento ou instável na pipeline | Tempos longos e falsos negativos | Definir na I095 o subconjunto de cenários a correr na pipeline; o conjunto completo corre localmente e fica registado |
| API de teste ainda por montar em E2 | Testes de sistema fora da pipeline | A I092 parte do `AplicacaoReal`; a I095 integra-a |
| Ligação de confirmação aberta no telemóvel | O email aponta para `Frontend:ConfirmUrl` | A I078 aceita a ligação `campuscarpool://confirmar-conta?token=…` e o token colado na aplicação. A I091 especifica os dois caminhos |
| Push depende do Firebase | Sem projeto real, não há entrega real | Serviço substituído na API de teste; entrega real verificada em E3, se configurado |
| Ambiente público com pendentes U2, U3, U4 e U22 | A simulação não corre em E3 | Corrigir antes da I102; alternativa em E1, declarada |
| Sem geometria da rota no backend (DEF-BE-054) | O mapa da I080 fica limitado | Decidir na I080 e refletir na especificação |
| Defeitos do hub (DEF-BE-055, 056) e do reenvio (DEF-BE-045, 046) | Comportamentos a contornar nos testes | Cenários indicam o defeito conhecido |
| SQLite em memória difere do SQL Server | Resultados locais incompletos | Executar em SQL Server na pipeline, E2 |

## 11. Situação dos testes no fim do Sprint 07

Situação a 10/11/2026. Os testes de sistema, a simulação de aceitação e o relatório de análise estática pertencem às issues das secções 3 e 7. Esta secção regista o que as issues de funcionalidade do Sprint 07 já executaram e o que ficou por executar, para que nada se perca entre sprints.

| Issue | Executado | Por executar | Quem e quando |
| --- | --- | --- | --- |
| I077 | `:core:check`, com 42/42 testes e 99,16 % das linhas; 29 testes da app, com 88,27 % das linhas (relatório JaCoCo de 10/11, noutro PC); ktlint; Android Lint numa cópia sem o Robolectric; os dois APK; fluxo real contra a API local | `connectedDebugAndroidTest` (6 testes, incluindo o Keystore real); arranque, rotação e restauro em Android 10 ou superior, com capturas; resultado JUnit dos 29 testes da app; Android Lint no projeto completo; pipeline no GitHub | Executor da I077, antes da aceitação |
| I078 | Núcleo 61/61, com 96,01 % das linhas; ViewModels 42/42; 25/25 verificações contra a API local; Android Lint numa cópia sem o Robolectric; os dois APK | Testes de ecrã (`EcrasTest`, `NavegacaoTest`) e cobertura da app; testes em dispositivo e capturas; pipeline no GitHub | Executora da I078: a pipeline corre os testes de ecrã e a cobertura no push; o dispositivo antes da aceitação |
| I079 | Núcleo 83/83, com 92,34 % das linhas; ViewModels 66/66; 27/27 verificações contra a API local; Android Lint numa cópia sem o Robolectric; os dois APK | Testes de ecrã (`ViagensEcrasTest`) e cobertura da app; testes em dispositivo e capturas; pipeline no GitHub | Executora da I079, como na I078 |
| I077 a I079 | — | Android 10 (API 29) e a versão mais recente (RNF-COM-02); SQL Server; payloads de XSS da secção 6; testes de sistema em emulador, versionados no projeto | I092 (implementação) e I098 (resultados) |

As verificações contra a API local foram feitas com SQLite, sem dispositivo. Os testes de ecrã com Robolectric não correram no computador onde as I078 e I079 foram preparadas, por falta de dependências; correm na pipeline.

**Testes recomendados, por fazer:** nenhum teste de ecrã confirma que o perfil (I078) e as viagens (I079) mostram os payloads X1 a X4 como texto. O Compose não interpreta HTML, mas falta a prova. Ficam para quem voltar a estes ecrãs nas issues seguintes, ou para a I092, no TSM-059.

## 12. Documentos relacionados

Regulamento da UC (secções 8, 14, 15, 17 e 19); Proposta de Sistema v03; Declaração de Âmbito v03; Especificação de Requisitos v02; Plano de Qualidade v02 (secção 4.1, quality gates); Regulamento Interno do Grupo; `m3-plano-testes-backend-v01.md`; modelo de classes do frontend mobile; protótipo de baixa fidelidade; `m4-backlog-projeto-v08`; `m4-especificacao-testes-sistema-frontend-mobile-v01.md` (I091).

## 13. Histórico

| Data | Alteração |
| --- | --- |
| 10/11/2026 | Versão inicial |
| 10/11/2026 | Revista antes da revisão formal. Secção 2: todos os requisitos não funcionais com componente mobile, com a forma de verificação e os cenários da I091. Secções 3, 4 e 9: quality gates do Plano de Qualidade v02 e UI Automator, previsto no QG-07, para o que acontece fora da aplicação. Secção 5: API de teste. Secção 6: relógio do backend, payloads de XSS e limitador fixo no código (a versão anterior admitia desligá-lo por configuração, o que o backend não permite). Secções 3.2, 10 e 11: situação dos testes no fim do Sprint 07, com a evidência da app da I077 recebida a 10/11 |
