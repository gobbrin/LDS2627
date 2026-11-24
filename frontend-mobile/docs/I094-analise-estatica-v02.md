# I094 — Relatório de análise estática do frontend mobile

**Projeto:** G15 — Campus Carpool  
**Milestone / Sprint:** M4 / S09  
**Issue:** I094 — Análise estática mobile  
**Tipo:** CI / qualidade de código  
**Versão documental:** v02, atualização de evidências em 24/11/2026  
**Componente analisado:** `frontend-mobile` da versão local integrada S08/S09/S10 disponibilizada pelo grupo  
**Branch da I094:** `ci/i094-analise-estatica-mobile`  
**Código consultado:** [develop](https://github.com/gobbrin/LDS2627/tree/develop) — commit `184ee94934e8478d389c3f0e6e541127a6561613` (consulta ao repositório; os logs locais não identificam o SHA).

## 1. Objetivo, âmbito e base normativa

A análise estática cumpre o enquadramento do plano de testes do frontend mobile **I090 v01**, especialmente §3 (níveis de teste), §4 (ferramentas) e §9 (critérios de saída). A especificação **I091 v01** distingue os cenários de sistema TSM-001–066, e em particular TSM-066 (quality gates): os resultados estáticos aqui documentados não se confundem com a execução dos testes de sistema. A I092 documenta a automação parcial; a I095 trata da integração na pipeline e a I098 dos resultados dos testes de sistema.

**Mapa de quality gates do plano I090:** QG-01 = compilação; QG-05 = Android Lint/ktlint; QG-10 = testes unitários; QG-11 = cobertura de linhas >=80%; QG-12 = formatação/ktlint. O QG-07 exige testes de sistema automáticos e não é demonstrado pelos comandos desta I094.


Registar as verificações de análise estática, qualidade e cobertura do frontend mobile, conforme a I094. Incluem-se os módulos `core` e `app`, com Android Lint, ktlint, regras de cobertura JaCoCo, compilação e testes unitários como evidências complementares. O relatório regista a execução local e a confirmação do quality gate final comunicada pelo grupo.

## 2. Configuração e ferramentas

- **Ambiente de referência da I094:** JDK 21 e Android SDK 36.
- **Formatação e estilo:** ktlint 1.5.0 na configuração registada no documento original.
- **Análise estática Android:** tarefa Gradle `:app:lintDebug` com `warningsAsErrors` e `abortOnError = true`; regras `OldTargetApi`, `GradleDependency` e `NewerVersionAvailable` desativadas em `frontend-mobile/app/build.gradle.kts`.
- **Cobertura:** JaCoCo 0.8.13 (indicado nos relatórios HTML enviados); verificação configurada para mínimo de 80% das linhas em core e app, de acordo com a preparação da I094.
- **Artefactos:** relatórios HTML/XML JaCoCo e saídas PowerShell da execução Gradle.

## 3. Comandos e resultados efetivamente evidenciados

| Execução local registada | Resultado | Evidência |
|---|---|---|
| `:core:test :core:jacocoTestReport :app:testDebugUnitTest :app:jacocoApp` | **BUILD SUCCESSFUL**, 38 tarefas executadas (2m17s) | `frontend-mobile.txt` |
| `:core:jacocoTestCoverageVerification :app:verificarCoberturaApp` | **BUILD SUCCESSFUL**, 40 tarefas (7s), repetido posteriormente com sucesso | `frontend-mobile.txt` |
| `:core:ktlintCheck :app:ktlintCheck :app:lintDebug :app:assembleDebug` | **BUILD SUCCESSFUL**, 70 tarefas (4m08s), repetido posteriormente com sucesso | `frontend-mobile.txt` |

**Data dos resultados apresentados:** 24/11/2026 (datas nos ficheiros e no conjunto fornecido). A última execução repetida de lint/build e cobertura consta do mesmo registo. O aviso `Unable to strip ... libandroidx.graphics.path.so` ocorreu durante o empacotamento, mas o comando terminou com `BUILD SUCCESSFUL`; não constitui, por si só, falha do lint.

## 4. Resultados JaCoCo da execução mais recente

| Módulo | Linhas cobertas | Cobertura de linhas | Instruções cobertas | Cobertura de instruções | Ramos cobertos |
|---|---:|---:|---:|---:|---:|
| core | 439/478 | **91,84%** | 3464/3872 | 89,46% | 93,09% |
| app | 2076/2448 | **84,80%** | 27773/32256 | 86,10% | 68,35% |

**Fontes:** `test.zip` (`test/jacocoTestReport.xml` e HTML) e `jacocoApp.zip` (`jacocoApp/jacocoApp.xml` e HTML). Os valores foram calculados a partir dos contadores dos relatórios XML. O critério de 80% de linhas foi cumprido nos dois módulos, confirmado também pelas tarefas Gradle de verificação de cobertura. A cobertura de ramos da app é inferior a 80%; esse valor não é o indicador usado pelo limite de linhas aqui documentado.

Os contadores correspondem aos relatórios de 24/11. O JaCoCo da app utiliza os dados dos testes unitários; a execução instrumentada é uma evidência complementar.

## 5. Observações e avisos

| Aviso | Localização / origem | Resultado |
|---|---|---|
| `Unable to strip ... libandroidx.graphics.path.so` | Tarefa `:app:stripDebugDebugSymbols`; biblioteca nativa de terceiros | Empacotada sem remoção de símbolos; build aprovado |
| `Deprecated Gradle features` | Gradle 8.13, durante a configuração/execução local anterior | Aviso de compatibilidade futura com Gradle 9; origem específica não identificada |
| `Restricted method in java.lang.System` | `NativeLibraryLoader` do Gradle, execução anterior com Java 25 | Aviso do ambiente Java; utilizar JDK 21 |

As tarefas de ktlint, Android Lint e compilação Debug terminaram com sucesso nos logs de 24/11. As regras de lint ativas tratam avisos como erros; as três exclusões indicadas na secção 2 delimitam essa verificação. Não foram fornecidos relatórios individuais de lint para localizar outros avisos por ficheiro e linha.

## 6. Evidência complementar de testes instrumentados

Em 24/11/2026, a tarefa `:app:connectedDebugAndroidTest` registou seis testes no emulador Pixel_10a(AVD) - 17, zero falhados, zero ignorados e `BUILD SUCCESSFUL` em 3m38s. A identificação nominal foi recolhida posteriormente numa cópia local distinta, mediante leitura dos XML:

| Classe | Teste |
|---|---|
| AndroidSessionStoreTest | `conteudoAdulteradoEDescartado` |
| AndroidSessionStoreTest | `limparRemoveASessao` |
| AndroidSessionStoreTest | `guardaELeOMesmoValor` |
| AndroidSessionStoreTest | `oTokenNaoFicaEmTextoSimples` |
| LoginScreenTest | `credenciaisVaziasNaoSubmetem` |
| LoginScreenTest | `credenciaisValidasSaoEntreguesAoViewModel` |

A transcrição das duas saídas consta de `evidencias/testes-instrumentados.txt`. A rastreabilidade funcional é tratada na I092/I098; estes testes não representam a totalidade dos 66 cenários TSM nem integram o cálculo JaCoCo unitário da app.

## 7. Correspondência aos critérios da I094 e ao plano I090

| Critério | Evidência | Resultado |
|---|---|---|
| QG-01 — Compilação | assembleDebug | Conforme localmente |
| QG-05 — Análise estática | ktlint core/app e lintDebug | Conforme localmente |
| QG-10 — Testes unitários | core:test e app:testDebugUnitTest | Conforme localmente |
| QG-11 — Cobertura de linhas ≥80% | core 91,84%; app 84,80% | Conforme localmente |
| QG-12 — Formatação | ktlintCheck | Conforme localmente |
| Arquivo de evidências | Logs e relatórios HTML/XML anexos | Documentado |
| Quality gate final da develop | Confirmação comunicada pelo grupo em 24/11/2026 | Confirmado pelo grupo |

O QG-07 é tratado nas I092/I098 e não constitui uma regra de análise estática da I094.

## 8. Conclusão técnica

**Análise estática: CONFORME nas verificações documentadas.** Os gates locais aplicáveis passaram e a cobertura de linhas excede 80% nos dois módulos. O quality gate final da develop foi confirmado pelo grupo. A confirmação do grupo é distinta dos logs locais anexos.

**Evidências anexas:** `evidencias/frontend-mobile.txt` (original), `evidencias/test.zip` (JaCoCo core original), `evidencias/jacocoApp.zip` (JaCoCo app original), `evidencias/testes-instrumentados.txt` (transcrição das saídas comunicadas) e `evidencias/confirmacao-grupo.txt` (registo da confirmação comunicada).

**Referências internas:** [I094](https://github.com/gobbrin/LDS2627/issues/69); [branch da I094](https://github.com/gobbrin/LDS2627/tree/ci/i094-analise-estatica-mobile); `docs/frontend-mobile/plano-de-testes/m4-plano-testes-frontend-mobile-v01.md` (I090); `docs/frontend-mobile/testes-de-sistema/m4-especificacao-testes-sistema-frontend-mobile-v01.md` (I091); `docs/frontend-mobile/testes-de-sistema/I092-automacao-android.md` (I092).
