# Validação da base I077 — v02, 09-11-2026

Estado: preparada para revisão, **com verificações por executar** (secção 3). Este documento regista verificações executadas; não é relatório assinado nem autorização de merge.

A v02 parte da v01 do Executor (08-11-2026) e acrescenta as correções da secção 2. Backlog de referência: **v08**. Modelo de classes: frontend mobile v04.

## 1. Resultados observados na v02

Executado a 09-11-2026, com JDK 21.0.7, Gradle 8.13 do Wrapper (SHA-256 confirmado) e Android SDK 36 / Build Tools 35.0.0.

| Verificação | Resultado | Evidência |
| --- | --- | --- |
| `:core:check` (compilação, testes, ktlint, JaCoCo) | Passou | `gates-i077.log` |
| Testes JVM do núcleo | 42 de 42 | `junit/` |
| Cobertura do núcleo | Linhas 237/239 = 99,16 %; ramos 167/186 = 89,78 % | `jacoco-core.xml` |
| `:app:ktlintCheck` | Passou, incluindo os testes novos | `gates-i077.log` |
| `:app:lintDebug` | Sem problemas. Executado numa cópia sem o Robolectric (secção 3) | `lint-debug.txt` |
| `:app:assembleDebug` e `:app:assembleDebugAndroidTest` | Os dois APK gerados | `gates-i077.log` |
| Configuração do endereço | `10.0.2.2` e HTTPS aceites; HTTP noutro anfitrião e caminho errado recusados na compilação | Execução de `:app:help -PcampusApiUrl=…` |
| Fluxo do núcleo contra a API real local | 401 credenciais erradas, 403 conta por confirmar, login, `/me`, perfil, logout 204, token depois recusado com 401 | Teste temporário, fora da entrega |

A API local corre o `Program.cs` real com SQLite em memória. Não foram usados SQL Server, o ambiente público nem dispositivo Android.

## 2. Alterações face à v01

| # | Problema encontrado | Correção |
| --- | --- | --- |
| 1 | `lintDebug` falhava com 17 erros; nunca tinha corrido | Corrigidos. `GradleDependency` e `NewerVersionAvailable` desativadas, com justificação em `app/build.gradle.kts`; `UseKtx` suprimida em `AndroidSessionStore`, com justificação |
| 2 | Sem pipeline (DoD: pipeline verde) | `.github/workflows/frontend-mobile-ci.yml` |
| 3 | QG-11: o módulo `app` não tinha testes nem limiar | 29 testes Robolectric e limiar de 80 % em `:app:verificarCoberturaApp` |
| 4 | `ErrosApi.executar` tratava um 204 como resposta incompleta; a I078 e a I079 iam precisar | `ErrosApi.executarSemCorpo` |
| 5 | Mensagens da API perdidas em 502, 503 e 504 | Passam a ser mostradas, como no frontend web; os outros 5xx continuam genéricos |
| 6 | `ConnectivityMonitor` consultava o sistema dentro do callback, com valores desatualizados | Usa as capacidades recebidas |
| 7 | HTTP para anfitrião não local compilava e a aplicação falhava ao arrancar | Recusado na compilação |
| 8 | Sessão expirada voltava ao início sem explicação | Mensagem ao utilizador |
| 9 | O email do login perdia-se ao rodar o ecrã | `rememberSaveable`, só no email |
| 10 | Sem ícone da aplicação; pasta `.kotlin/` por ignorar | Ícone provisório; `.gitignore` |

## 3. Por executar

| Verificação | Motivo | Quem |
| --- | --- | --- |
| 29 testes Robolectric e cobertura da app (QG-10, QG-11) | Compilam, mas não correram: o Maven Central recusou mais transferências a esta máquina (429, cerca de 24 h) antes de obter os ficheiros do Robolectric | Executor, ou a pipeline no primeiro push |
| `lintDebug` no projeto completo | Depende dos mesmos ficheiros | Executor, ou a pipeline |
| `connectedDebugAndroidTest` (6 testes, incluindo o Keystore real) | Exige dispositivo; este PC não tem aceleração para o emulador | Executor |
| Arranque, navegação, restauro e rotação em Android 10+ | Idem | Executor |
| Capturas dos ecrãs e fluxos (evidência da Issue) | Idem | Executor |
| Pipeline no GitHub | Só corre depois do push | Executor |
| SQL Server e ambiente público | API local em SQLite; a API pública é anterior à I110 | Revisor, quando disponível |

Um limiar de 80 % ainda não medido pode falhar. Se a pipeline falhar nesse passo, ver o relatório `app/build/reports/jacoco/jacocoApp/html`.

## 4. Critérios do Backlog v08

| Critério | Situação |
| --- | --- |
| O projeto compila e arranca com instruções documentadas | Compila. Arranque em dispositivo por verificar |
| O endereço do backend é configurável sem alterar código | Verificado |
| A sessão é mantida e terminada corretamente | Regras e revogação verificadas contra a API. Keystore real por verificar |
| Erros da API são apresentados de forma consistente | Normalização verificada em testes e contra a API. Apresentação nos ecrãs coberta pelos testes por executar |
| A estrutura corresponde ao modelo de classes | Camadas e núcleo correspondem. `AppViewModel` deve dar lugar a `LoginViewModel` e `PerfilViewModel` na I078; `TempoRealClient` e `DataHoraFormatter` chegam com as funcionalidades que os usam |

Análise estática: ktlint e Android Lint. O detekt não está configurado; o Regulamento Interno §11.4 admite detekt ou ktlint.
