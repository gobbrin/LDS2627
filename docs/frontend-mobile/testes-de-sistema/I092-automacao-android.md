# I092 — Automação de testes do frontend mobile — relatório técnico

**Projeto:** G15 — Campus Carpool  
**Milestone / Sprint:** M4 / S09  
**Issue:** I092 — Implementar os testes de sistema automáticos do frontend mobile  
**Versão documental:** v01   
**Branch de entrega:** `test/i092-testes-sistema-mobile`  
**Versão do código efetivamente verificada:** versão mais recente do código do projeto Campus Carpool.  
**Base documental:** I090 — Plano de testes mobile v01; I091 — Especificação dos testes de sistema mobile v01; Registos locais.

## 1. Âmbito e enquadramento no plano I090

A implementação disponibilizada acrescenta testes automatizados ao frontend Android, incidindo sobre contratos Retrofit/HTTP e sobre estados e comportamentos da interface em Jetpack Compose.

De acordo com a descrição e os ficheiros de teste identificados, foram acrescentados:
- **7 testes de contrato Retrofit/HTTP**, com respostas da API controladas pelos testes;
- **12 testes de interface/estado Compose**, executados com Robolectric.

Estes testes exercitam partes do frontend e a sua interação com dependências simuladas/controladas. Não correspondem, por si só, a uma execução ponta a ponta completa contra o backend real.

## 2. Ficheiros de teste e estado de integração

- `core/src/test/kotlin/pt/ipca/g15/campuscarpool/ComunidadeContratoTest.kt`
- `app/src/test/kotlin/pt/ipca/g15/campuscarpool/ComunidadeEcrasTest.kt`
- `app/src/test/kotlin/pt/ipca/g15/campuscarpool/ComunidadeCasosAdicionaisTest.kt` — nove casos adicionais presentes na I092
- `:app:ktlintCheck` e `:app:testDebugUnitTest` passaram na versão atual do código do projeto Campus Carpool

## 3. Matriz de rastreabilidade parcial à especificação I091

A correspondência com os cenários TSM da I091 é parcial: os comportamentos abaixo são exercitados, mas isso não significa que todos os passos e critérios de cada cenário estejam automatizados.

| Comportamento verificado | Cenários I091 relacionados |
|---|---|
| Pesquisa, paginação, detalhe e tratamento de falha dos mapas sem perder o detalhe | TSM-023, TSM-025, TSM-026, TSM-029 |
| Pedido com pontos opcionais e cancelamento apenas após confirmação | TSM-033, TSM-038 |
| Bloqueio da aceitação antes da simulação; rejeição e cancelamento pelo condutor | TSM-031, TSM-036, TSM-039 |
| Envio de mensagem literal, apresentação de erro sem perder o texto e limites de tamanho | TSM-041, TSM-042, TSM-047 |
| Publicação de aviso pelo condutor e ausência dessa ação para o passageiro | TSM-044, TSM-045 |
| Paginação da caixa de notificações, leitura individual/em lote e desativação local de push | TSM-048, TSM-049, TSM-050 e parte de TSM-054 |
| Histórico, seleção de participante e criação/alteração de classificação de 1 a 5 | TSM-055, TSM-056 |
| Datas/horas em `Europe/Lisbon` no inverno e verão e respostas JSON parciais | Parte de TSM-062 e TSM-064 |

Os testes de interface verificam também que as alterações de estado ficam refletidas visualmente após a resposta do serviço.

**Interpretação da matriz:** cada associação significa *cobertura parcial com serviços controlados*, nunca PASS integral do TSM. A I091 define 66 cenários (TSM-001 a TSM-066), com precondições, dados, passos, oráculos e RF/RNF próprios. Sem relatório caso a caso, não atribuir PASS aos 66 cenários. Para cada TSM, a I098 deverá registar ID, teste automatizado, ambiente E1/E2, dados/fixture, resultado, evidência, build/commit e defeito, se aplicável.

**AUX-01 a AUX-13 e E1/E2 (I091):** o plano I090 exige dados de teste isolados (contas confirmadas e por confirmar, viagens em diferentes estados, pedidos, notificações, emails e mapas simulados), uma API de teste com regras reais de autenticação/limitação e emulador Android ≥ API 29. As respostas simuladas nesta implementação não comprovam, por si só, os oráculos de backend e base de dados nem a execução contra a API de teste.

## 4. Execução e evidências de validação do frontend Android

Os seguintes comandos foram executados na pasta `frontend-mobile`, em ambiente Windows/PowerShell, através do Gradle Wrapper:

```powershell
.\gradlew.bat :core:test :core:jacocoTestReport :app:testDebugUnitTest :app:jacocoApp
.\gradlew.bat :core:jacocoTestCoverageVerification :app:verificarCoberturaApp
.\gradlew.bat :core:ktlintCheck :app:ktlintCheck :app:lintDebug :app:assembleDebug
```

**Resultado registado:** todos os comandos terminaram com `BUILD SUCCESSFUL`. As verificações de cobertura foram repetidas e voltaram a terminar com sucesso. O `ktlint`, o `lint` e a compilação do APK de debug também terminaram com sucesso.

### Cobertura JaCoCo do frontend

Os relatórios JaCoCo fornecidos apresentam os seguintes resultados globais:

| Módulo | Instruções | Branches | Linhas |
|---|---:|---:|---:|
| `core` | 89,46% | 93,09% | 91,84% (439/478) |
| `app` | 86,10% | 68,35% | 84,80% (2076/2448) |

A verificação dos limiares de cobertura configurados (80%) terminou com sucesso nos dois módulos, conforme os resultados das tarefas `:core:jacocoTestCoverageVerification` e `:app:verificarCoberturaApp`.

A cobertura global não deve ser interpretada como cobertura uniforme de todas as funcionalidades. No relatório do módulo `app`, por exemplo, os pacotes de comunidade apresentam 84% de instruções e 59% de branches; `data.remote` apresenta 38% de instruções e 19% de branches; e `data.session` apresenta 13% de instruções e 16% de branches. Estes valores identificam áreas ainda pouco exercitadas, apesar de a verificação dos limiares configurados ter passado.

Os relatórios HTML foram abertos a partir dos seguintes caminhos:

```text
core/build/reports/jacoco/test/html/index.html
app/build/reports/jacoco/jacocoApp/html/index.html
```

## 5. Execução instrumentada em emulador

Foi apresentado um registo de execução local adicional do seguinte comando:

```powershell
.\gradlew.bat :app:connectedDebugAndroidTest
```

| Campo | Resultado comprovado pelo excerto do terminal |
|---|---|
| Dispositivo | `Pixel_10a(AVD) - 17` (identificação literal do Gradle) |
| Testes iniciados | **6** |
| Testes concluídos | **6** |
| Falhados | **0** |
| Ignorados | **0** |
| Resultado Gradle | **BUILD SUCCESSFUL** |
| Duração | **3m38s** |
| Tarefas Gradle | 75 (1 executada, 74 `up-to-date`) |

### 5.1. Identificação nominal dos testes instrumentados

A listagem extraída por PowerShell dos elementos `testcase` dos relatórios XML em `app/build/outputs/androidTest-results`, identificam quatro testes da classe `AndroidSessionStoreTest` e dois da classe `LoginScreenTest`, ambas no pacote `pt.ipca.g15.campuscarpool`.

| Classe | Método de teste | Comportamento indicado pelo nome | Relação com I091 |
|---|---|---|---|
| AndroidSessionStoreTest | `conteudoAdulteradoEDescartado` | Descarte de conteúdo adulterado | F001; segurança da sessão, complementar a TSM-010 |
| AndroidSessionStoreTest | `limparRemoveASessao` | Limpeza da sessão local | TSM-010, parcial |
| AndroidSessionStoreTest | `guardaELeOMesmoValor` | Armazenamento e leitura da sessão | TSM-010, parcial |
| AndroidSessionStoreTest | `oTokenNaoFicaEmTextoSimples` | Proteção do token armazenado | F001; segurança da sessão, complementar a TSM-010 |
| LoginScreenTest | `credenciaisVaziasNaoSubmetem` | Bloqueio de submissão vazia | F001; validação local, complementar a TSM-009 |
| LoginScreenTest | `credenciaisValidasSaoEntreguesAoViewModel` | Entrega dos dados ao ViewModel | TSM-009, parcial |

A associação é efetuada pelo nome dos métodos e pelos objetivos da I091, não pela inspeção das respetivas assertions. A própria I091 (§2.2) identifica estas duas classes como testes instrumentados de infraestrutura da I077. A sua presença é evidência complementar à I092; não equivale a novos testes de sistema ponta a ponta.

O log da execução inicial confirma 6/6 testes aprovados. A listagem XML da versão atual do projeto confirma os seis nomes, mas não inclui resultados individuais, duração, dispositivo ou commit. Assim, a coincidência da quantidade e das classes é consistente com a suite anteriormente identificada na I091, sem permitir comprovar que os dois registos correspondem à mesma revisão do código. 

### 5.2. Reprodução e arquivo

Com um emulador ativo e JDK 21, executar `./gradlew.bat :app:connectedDebugAndroidTest` em PowerShell. Os resultados completos devem ser preservados em `app/build/outputs/androidTest-results/` e o relatório HTML em `app/build/reports/androidTests/`, quando gerado.

## 6. Evidência complementar do backend (não atribuída à I092)

A suite `CampusCarpool.Tests` (`net10.0`) teve **674 testes registados: 668 aprovados, 0 falhados e 6 ignorados**; o build Release terminou com sucesso. O relatório Cobertura regista **95,51% de linhas e 87,71% de branches**. Os seis ignorados dizem respeito a cenários de integração/concorrência com SQL Server. Estas verificações suportam a qualidade do backend usado pelo projeto, **não a execução ponta a ponta do frontend Android** e não contam como cobertura dos cenários TSM.

## 7. Lacunas e validação de sistema exigida por I090/I091

As evidências disponíveis demonstram:
- execução bem-sucedida dos testes unitários do frontend Android e das respetivas verificações JaCoCo;
- sucesso do `ktlint`, `lint` e compilação do APK de debug;
- execução bem-sucedida de 668 testes backend, sem falhas, com 6 testes ignorados;
- cobertura de linhas/branches do backend indicada pelo relatório Cobertura;
- compilação Release e restore da solução backend com sucesso;
- **6 testes instrumentados no emulador Pixel_10a**, sem falhas nem testes ignorados.

Estes resultados demonstram a execução de seis testes instrumentados no emulador, **mas não demonstram a execução integral dos cenários de sistema** nem a validação ponta a ponta contra o backend real.

Para completar a validação de sistema da I092, ficam por realizar ou documentar:

1. Preparar e aplicar as fixtures E1/E2 previstas no plano I090.
2. Completar a automatização dos cenários TSM-001 a TSM-066, confrontando os resultados com os RF/RNF e com a ER/RTM em vigor.
3. Executar os fluxos do frontend mobile contra a API de teste, utilizando as regras reais de autenticação e autorização do backend, validando também a persistência e consistência dos dados.
4. Verificar cenários de concorrência, reconexão, mapas, ciclo de vida da aplicação e entrega de notificações FCM em dispositivos.
5. Executar e registar a matriz de dispositivos/versões Android prevista, anexando os resultados à I098.
6. Completar os casos dependentes de funcionalidades ainda não disponíveis; a consulta da avaliação própria e a geometria dependem da I112.
7. Avaliar a execução dos seis testes backend ignorados num ambiente SQL Server adequado, caso estes cenários façam parte da validação exigida para a entrega.

O comando `:app:assembleDebug` comprova a compilação do APK de debug, não a execução de testes instrumentados num emulador/dispositivo. Do mesmo modo, a compilação de um APK de testes, caso seja efetuada, não equivale à sua execução. Os resultados backend são evidência complementar e não substituem os testes de sistema do frontend mobile.

## 8. Conclusão e estado da evidência

A implementação de testes Android identificada e as verificações locais documentadas foram executadas com sucesso. Existe também evidência positiva de uma suite automatizada extensa no backend (668 testes com sucesso e zero falhas), acompanhada por cobertura Cobertura de 95,51% de linhas e 87,71% de branches.

Os limiares JaCoCo de 80% do frontend foram mantidos e as respetivas tarefas de verificação passaram. A cobertura de código e o sucesso dos testes unitários/backend são indicadores de qualidade, mas não substituem a execução e o registo dos testes de sistema mobile.


## 9. Critérios de saída, qualidade e relação com outras issues

| Referência | Situação documental |
|---|---|
| I090 §3.1 / I091 | Testes de sistema automáticos separados dos testes de aceitação simulados; os testes HTTP/Robolectric são suporte à automação, não equivalem a E1/E2 completo. |
| I090 QG-01, QG-05 e QG-12 | `assembleDebug`, Android Lint e ktlint passaram localmente. |
| I090 QG-10 e QG-11 | Testes unitários e verificações JaCoCo passaram; cobertura global não prova a execução de cada TSM. |
| I090 QG-07 | A evidência disponibilizada não comprova execução sistemática da suite I091 contra a API de teste; requer relatório de execução específico. |
| I091 TSM-057 a TSM-066 | Os requisitos não funcionais incluem TLS/configuração, segredos, XSS, privacidade, usabilidade, datas, compatibilidade Android e bloqueio de gates; não são demonstrados integralmente pelos testes locais aqui registados. |
| I095 / I098 | A I095 integra testes na pipeline do sistema; a I098 regista resultados de sistema, ambientes, dispositivos, PASS/FAIL/BLOCKED e defeitos. |
| I094 | Relatório próprio de análise estática mobile, com logs e métricas JaCoCo por módulo. |

**Resultado documentado da I092:** automação parcial implementada e verificações locais bem-sucedidas. 

**Referências do projeto:** `docs/frontend-mobile/plano-de-testes/m4-plano-testes-frontend-mobile-v01.md` (I090); `docs/frontend-mobile/testes-de-sistema/m4-especificacao-testes-sistema-frontend-mobile-v01.md` (I091); documentação do projeto e relatórios JaCoCo/Gradle.
