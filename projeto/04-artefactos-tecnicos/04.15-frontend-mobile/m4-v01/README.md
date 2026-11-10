# Campus Carpool — base frontend mobile (I077)

G15 · LDS2627 · M4 · Sprint07 · v01 · 08-11-2026

Projeto Android nativo em Kotlin e Jetpack Compose, mínimo Android10/API29. Esta entrega estabelece a base de I077: ecrã inicial, login de conta já confirmada, sessão persistida de forma cifrada, navegação protegida, consulta básica do perfil próprio, cliente da API e tratamento de erros. Não é a implementação completa de F001–F009. Registo/confirmação/edição completa do perfil e restantes funcionalidades pertencem aos incrementos seguintes.

A implementação está preparada para revisão; a aceitação final depende das verificações Android e das pendências registadas em `VALIDACAO.md`. Não existe aprovação de revisor/auditor nem publicação remota implícita neste pacote.

## Requisitos de desenvolvimento

| Componente | Versão/configuração |
| --- | --- |
| JDK de desenvolvimento | 21 (toolchain do core); bytecode JVM17 |
| Gradle Wrapper | 8.13, com SHA-256 da distribuição |
| Android Gradle Plugin | 8.13.2 |
| Kotlin / Compose compiler plugin | 2.2.21 |
| Android SDK | Plataforma36; Build Tools35.0.0 (predefinição AGP8.13); Platform Tools |
| Android mínimo / target | 29 / 36 |
| Compose BOM | 2025.10.01 |
| HTTP | Retrofit2.11.0, OkHttp4.12.0, Gson2.13.2 |
| Testes/análise | JUnit4.13.2, Robolectric4.16, MockWebServer4.12.0, JaCoCo0.8.13, plugin ktlint13.1.0 / motor1.5.0 |

Abrir **esta pasta** no Android Studio compatível com AGP8.13.2. Instalar os componentes SDK pelo SDK Manager; definir `ANDROID_HOME` ou `sdk.dir` em `local.properties` (ficheiro local, excluído de Git). Configurar JDK21 para Gradle. A primeira execução precisa de rede para dependências. No Windows, substituir `./gradlew` por `gradlew.bat`.

## Compilar e executar

```bash
./gradlew :app:assembleDebug
./gradlew :app:installDebug
```

Selecionar emulador/dispositivo API29 ou superior. Abrir Campus Carpool. Usar credenciais de uma conta académica **já confirmada** no backend selecionado; não existem credenciais embutidas nem contas de demonstração automáticas. Após entrar, abrir Perfil. Terminar sessão elimina o estado local e chama a revogação no servidor. Não há refresh token; após expiração é necessário novo login.

### Escolher o backend sem alterar código

O endereço público predefinido é:

`https://campuscarpool-api-g15-ase2b6g6cbabf7d7.germanywestcentral-01.azurewebsites.net/api/v1/`

Configuração por propriedade Gradle (prioridade superior) ou variável `CAMPUS_API_URL`, aplicada na compilação:

```bash
./gradlew :app:assembleDebug -PcampusApiUrl=https://api-de-teste.exemplo/api/v1/
./gradlew :app:installDebug -PcampusApiUrl=http://10.0.2.2:5100/api/v1/
```

O segundo comando aponta do emulador para o backend local do computador, cujo perfil HTTP atual usa porta5100. Iniciar o backend separadamente com a configuração de desenvolvimento do projeto. Em dispositivo físico por USB, usar `adb reverse tcp:5100 tcp:5100` e compilar com `http://127.0.0.1:5100/api/v1/`. Num dispositivo/emulador, `localhost` é o próprio Android. Não usar localhost para representar automaticamente o computador.

A URL deve terminar exatamente em `/api/v1/`, sem query, fragmento ou credenciais. HTTP só é permitido em **debug** para `10.0.2.2`, `127.0.0.1` e `localhost`. A variante release exige HTTPS; certificados inválidos não são aceites. Não há desativação de TLS. Uma alteração da URL exige recompilar/reinstalar, mas não editar código. Uma sessão guardada para outro backend é descartada.

```bash
./gradlew :app:assembleRelease -PcampusApiUrl=https://api-de-teste.exemplo/api/v1/
```

Produz a variante release não assinada para distribuição; a assinatura de distribuição é uma configuração posterior, fora deste incremento. Não incluir chaves de assinatura no repositório.

## Estrutura e relação com o modelo de classes

| Pasta/classe | Responsabilidade |
| --- | --- |
| `app/src/main/.../ui/inicio` | Descrição e entradas na aplicação |
| `app/src/main/.../ui/autenticacao` | Formulário de login, estado ocupado e mensagens por campo |
| `app/src/main/.../ui/perfil` | Perfil próprio básico de leitura; não inclui ainda edição/ligações externas |
| `ui/AppNavigation`, `ui/AppViewModel` | Navegação Compose e estado observável com StateFlow; UI observa ciclo de vida |
| `core/.../data/remote/ApiService` | Contrato HTTP realmente utilizado neste incremento |
| `core/.../data/remote/ApiFactory` | URL validada, timeouts e cliente sem seguir redirecionamentos |
| `AuthInterceptor`, `ErroInterceptor`, `ErrosApi` | Bearer só na API autorizada;401 encerra sessão correspondente; normalização de erros |
| `core/.../data/repository` | Operações de autenticação e leitura do perfil |
| `core/.../data/session` | Interface SessionStore e SessaoManager: restauro/expiração/limpeza |
| `app/.../data/session/AndroidSessionStore` | AES-GCM, chave no Android Keystore e conteúdo cifrado nas preferências |
| `app/.../data/remote/ConnectivityMonitor` | Indicação de ligação; não implementa modo offline |
| `core/.../domain/model` | DTOs e resultados usados pela base |
| `app/.../di/AppContainer` | Composição explícita de dependências e âmbito de aplicação |
| `core/src/test` | Testes JVM da sessão, erros, repositórios e contrato HTTP simulado |
| `app/src/test` | Testes JVM (Robolectric) do ViewModel, dos ecrãs, da navegação e do arranque |
| `app/src/androidTest` | Formulário Compose e Android Keystore real, a executar em dispositivo |

O módulo `core` permite testar a parte independente do Android sem SDK; `app` contém a plataforma, Keystore e UI. Mantém as responsabilidades do modelo, sem criar classes vazias para funcionalidades ainda não implementadas. SignalR, FCM, formatação de datas do domínio e ViewModels por futura área serão acrescentados nas respetivas funcionalidades. Não há chave FCM nem serviço de push fictício. `logout(tokenDispositivo)` já aceita o token opcional para a futura integração; a UI atual não regista dispositivos nem prova RF-F006-18.

## Contrato e sessão

| Operação | Endpoint relativo | Dados/resposta |
| --- | --- | --- |
| Login | `POST autenticacao/login` | `{email,password}` → `{accessToken,expiresIn}` |
| Validar identidade | `GET autenticacao/me` | Bearer → `{id}` |
| Perfil próprio | `GET utilizadores/me` | Bearer → dados do próprio perfil |
| Logout | `POST autenticacao/logout` | Bearer; corpo opcional `{tokenDispositivo}` →204 |

A base foi conciliada com o backend `develop` no commit `069764e07b12301bc88f1a79ad6fbfe7a54ff56e`. Algumas versões históricas do modelo usavam `/auth/` ou sessão com perfil embutido; o contrato vigente usa `/autenticacao/` e consulta separada.

O cliente extrai `sub` e `exp` para gerir a sessão local, e confirma a identidade em `/autenticacao/me`. Essa leitura **não verifica a assinatura JWT**: a autorização e validade criptográfica são responsabilidades do backend. O prazo local é o menor entre `exp` e `expiresIn`. A senha não é persistida, registada ou guardada em estado restaurável da UI. Não se regista tráfego com headers/corpos. O backup Android está desativado para não transportar credenciais cifradas para outra instalação.

Logout limpa localmente mesmo sem rede, mostrando que a revogação remota não foi confirmada; não afirma revogação se não recebeu resposta. Uma401 atrasada de um token antigo não elimina uma sessão nova. Alterar sessão recria o grafo de navegação, retirando destinos privados da pilha Voltar.

Erros `{erro:"..."}`, ProblemDetails e formato histórico com campos são normalizados. A mensagem da API é mostrada nos 4xx e em 502, 503 e 504; os restantes 5xx ficam com texto genérico, para não exporem detalhes internos. Respostas sem corpo usam `ErrosApi.executarSemCorpo`. Timeouts HTTP: ligação15s, leitura30s, chamada40s. O limite10s do serviço de mapas pertence ao backend/futura funcionalidade de rotas; estes valores não substituem esse requisito.

## Verificação reproduzível

Sem Android SDK, só o núcleo JVM:

```bash
./gradlew -PcoreOnly :core:check
```

Com SDK:

```bash
./gradlew :core:check :app:ktlintCheck :app:verificarCoberturaApp :app:lintDebug :app:assembleDebug
./gradlew :app:connectedDebugAndroidTest
```

A pipeline `.github/workflows/frontend-mobile-ci.yml` corre o primeiro comando em cada push que altere esta pasta.

`core:check` inclui testes, ktlint e limiar JaCoCo de80% de linhas **do core**. Relatórios em `core/build/reports/tests/test/`, `core/build/reports/jacoco/test/html/` e `core/build/reports/ktlint/`. O plugin ktlint usa a versão de motor explicitada em cada módulo. Não há exclusões globais de fontes para aumentar cobertura. A exceção de nomenclatura aplica-se apenas a funções anotadas `@Composable`, cuja convenção Compose usa PascalCase; não desativa a regra para as restantes funções.

`:app:verificarCoberturaApp` aplica o mesmo limiar de 80% ao módulo `app`, excluindo apenas `R` e `BuildConfig`; relatório em `app/build/reports/jacoco/jacocoApp/html`. O estado de cada verificação está em `VALIDACAO.md`. Não se cria baseline para ocultar avisos. O ficheiro `.editorconfig` aplica o estilo Android Studio e as regras do ktlint; a análise Android Lint trata avisos como erros.

## Ligação à I091 e fontes

A especificação de sistema está em `../../../03-artefactos-de-qualidade-de-produto/03.03-frontend-mobile/03.03.02-testes-de-sistema/m4-especificacao-testes-sistema-frontend-mobile-v01.md`, relativa a esta pasta. Os cenários cobrem o produto M4 previsto; os testes da base não constituem resultados de execução da I091.

Fontes de definição: backlog do projeto v08 (I077); Sprint07 v01; modelo de classes frontend mobile v04; especificação de requisitos M3 ficheiro v02/versão interna v05; contrato backend no commit acima; regulamento de grupo M3 v02 (§11.3–11.5).

Executor previsto I077: Gil Gabriel da Cunha Leão. Revisora prevista: Joana Francisca Pacheco Rodrigues. Auditor previsto: Vítor Samuel Vieira Lopes. Os nomes indicam a distribuição, não aprovação.
