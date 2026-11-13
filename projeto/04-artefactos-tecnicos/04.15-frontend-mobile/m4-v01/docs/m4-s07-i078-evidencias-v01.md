# Evidências da I078: F001 no frontend mobile (v01)

G15 · LDS2627 · m4 · Sprint 07 · 11-11-2026. Issue [#36](https://github.com/gobbrin/LDS2627/issues/36).

Este documento regista as verificações realizadas na I078, incluindo testes contra a API real, testes manuais no emulador Android e verificações automáticas do projeto. Não constitui relatório assinado nem autorização de merge.

**Estado: testes locais concluídos; entrega ainda sujeita à revisão, verificação da branch e pipeline.**

## 1. Critérios do Backlog v08

| Critério | Situação | Evidência |
| --- | --- | --- |
| O registo, a confirmação e o início de sessão funcionam contra o backend | Verificado através dos ViewModels contra a API real e dos fluxos manuais no emulador | Secções 3 e 4 |
| Contas não confirmadas não acedem às funcionalidades de viagens | Verificado: login bloqueado com mensagem de conta por confirmar; sessão não iniciada | Secções 3 e 4 |
| O utilizador edita o seu perfil | Verificado contra a API e manualmente no emulador, incluindo persistência e cancelamento das alterações | Secções 3 e 4 |
| O perfil de outros mostra apenas os dados permitidos | Verificado: o perfil público não apresenta o email académico | Secções 3 e 4 |
| Erros de validação são apresentados | Verificado nos ViewModels e nos ecrãs Compose, incluindo validações de registo, autenticação e ausência de ligação | Secções 3 e 4 |

## 2. Requisitos e casos de uso

Requisitos de F001 com componente mobile: RF-F001-01, 02, 07, 08, 10, 12, 14, 15, 17, 19, 20, 21 e 22.

| Caso de uso | Ecrã | Exceções tratadas |
| --- | --- | --- |
| UC-F001-01 Registar conta | `RegistoScreen` | E1, E2 no cliente; E3 mensagem da API; E4 oferece entrada e nova ligação; E5 oferece nova ligação |
| UC-F001-02 Confirmar conta | `ConfirmacaoScreen` | E1, E2, E3 com o motivo da API e os dois caminhos; falha de serviço permite repetir |
| UC-F001-03 Nova ligação | `ReenvioScreen` | E1 no cliente; resposta neutra da API |
| UC-F001-04 Iniciar sessão | `LoginScreen` | E1 mensagem genérica; E2 em destaque, com nova ligação e o email da tentativa |
| UC-F001-05 Terminar sessão | `InicioScreen` | A1 sessão expirada abre a entrada; E1 sem rede limpa o token e avisa |
| UC-F001-06 Próprio perfil | `PerfilScreen` | Carregamento, erro e repetição |
| UC-F001-07 Alterar dados | `EditarPerfilScreen` | A1 Cancelar sem pedidos; E1 no cliente e da API |
| UC-F001-08 Associar ligação | `EditarPerfilScreen` | E1 no cliente; E2 esconde o formulário e sugere remover, ou lê a lista real depois do 400 |
| UC-F001-09 Remover ligação | `EditarPerfilScreen` | Confirmação na própria linha; E1 mostra o motivo e o perfil atual |
| UC-F001-10 Perfil de outro | `PerfilPublicoScreen` | E1 perfil não encontrado; falha de serviço permite repetir |

## 3. Fluxo contra a API real

ViewModels reais com `ApiFactory`, `SessaoManager` e API local (`Program.cs` real, JWT, limitador, SQLite em memória, email simulado).

Teste temporário, fora da entrega. Resultado completo em `evidencias/i078/fluxo-real-i078.log`: **25 de 25 verificações concluídas**.

| N.º | Verificação |
| --- | --- |
| 1 | Dados inválidos não chegam à API |
| 2 a 5 | Registo: 202, 409, 400 de domínio não académico, 503 com a conta criada |
| 6 e 7 | Login: 403 sem sessão; 401 com a mesma mensagem para palavra-passe errada e email inexistente |
| 8 | Nova ligação: 202 com a mesma resposta, exista ou não a conta |
| 9 a 12 | Confirmação: ligação anterior recusada, ligação colada aceite, ligação usada e adulterada recusadas com o motivo |
| 13 e 14 | Login com conta confirmada; perfil próprio |
| 15 e 16 | Nome acima de P-26 recusado no cliente; alteração persistida numa nova consulta |
| 17 a 21 | Ligações: `javascript:` recusado no cliente, 5 guardadas, 6.ª recusada pela API, remoção, já removida com 404 |
| 22 e 23 | Perfil público visto por outra conta; perfil inexistente com 404 |
| 24 | Logout com o token recusado depois com 401 |
| 25 | Sessão revogada por fora termina e abre a entrada |

## 4. Testes manuais no emulador Android

Foram realizados testes manuais com a aplicação instalada no emulador Android e a API local em execução.

### 4.1 Registo e confirmação
| Verificação | Resultado | Evidência |
| --- | --- | --- |
| Ecrã inicial | Passou | 01-ecrã-inicial.png |
| Campos obrigatórios | Passou | 02-registo-campos-obrigatorios.png |
| Email inválido | Passou | 03-registo-email-invalido.png |
| Email não académico | Passou | 04-registo-email-nao-academico.png |
| Palavra-passe curta | Passou | 05-registo-palavra-passe-curta.png |
| Palavra-passe sem número | Passou | 06-registo-palavra-passe-sem-numero.png |
| Palavras-passe diferentes | Passou | 07-registo-palavras-passe-diferentes.png |
| Registo com dados válidos | Passou | 08-registo-dados-validos.png |
| Receção do email de confirmação | Passou | 09-email-confirmacao-recebido.png |
| Confirmação da conta | Passou | 10-conta-confirmada.png |
| Reutilização da ligação de confirmação | Passou: a aplicação informa que a ligação já foi utilizada | 22-reutilizacao-ligacao-confirmacao.png |

### 4.2 Autenticação e sessão
| Verificação | Resultado | Evidência |
| --- | --- | --- |
| Login com credenciais corretas | Passou | 11-login-sucesso.png |
| Login com palavra-passe incorreta | Passou: apresenta «Credenciais inválidas» | 20-login-credenciais-invalidas.png |
| Login com conta não confirmada | Passou: apresenta «Conta por confirmar» e impede a entrada | 21-login-conta-por-confirmar.png |
| Logout | Passou: apresenta «Sessão terminada» e regressa ao ecrã inicial | 18-logout-sucesso.png |
| Login após logout | Passou | Sem captura adicional |
| Persistência da sessão após fechar e reabrir a aplicação | Passou, sessão mantida | Sem captura adicional |
| Rotação do ecrã | Passou: a sessão e o ecrã principal mantêm-se nas orientações vertical e horizontal | 23-rotacao-horizontal.png |

### 4.3 Perfil e ligações externas

| Verificação | Resultado | Evidência |
| --- | --- | --- |
| Consulta do perfil próprio | Passou | 12-perfil-visualizacao.png |
| Abertura do formulário de edição | Passou | 13-edicao-perfil-formulario.png |
| Atualização dos dados do perfil | Passou: curso e instituição atualizados | 14-perfil-atualizado.png |
| Cancelamento da edição | Passou: as alterações não guardadas não foram aplicadas | 27-cancelamento-edicao-perfil.png e 28-perfil-apos-cancelamento-edicao.png |
| Adição de ligação externa | Passou | 15-perfil-ligacao-externa.png |
| Limite máximo de cinco ligações | Passou: a sexta ligação é impedida | 16-perfil-limite-cinco-ligacoes.png |
| Confirmação da remoção de uma ligação | Passou: é apresentado um pedido de confirmação | 24-confirmacao-remocao-ligacao-externa.png |
| Cancelamento da remoção | Passou: a ligação permanece na lista | 25-cancelamento-remocao-ligacao.png |
| Remoção efetiva de uma ligação | Passou: Stack Overflow desapareceu da lista | 26-perfil-apos-remocao-ligacao.png |
| Perfil público sem exposição do email | Passou: o email académico não é apresentado | 17-perfil-publico-sem-email.png |

### 4.4 Rede e recuperação
| Verificação | Resultado | Evidência |
| --- | --- | --- |
| Acesso ao perfil sem ligação à Internet | Passou: a aplicação apresenta mensagens de erro e permite tentar novamente | 19-perfil-sem-ligacao-internet.png |
| Recuperação da ligação à Internet | Passou: depois de reativar a rede e selecionar «Tentar novamente», o perfil voltou a carregar | Sem captura adicional |

### 4.5 Configuração de rede local
Durante os testes foi identificado que o ficheiro `app/src/main/res/xml/network_security_config.xml` bloqueava todas as ligações HTTP sem encriptação.

Para permitir a comunicação com a API local no emulador, foi alterada a configuração para autorizar HTTP apenas para `10.0.2.2`, mantendo o tráfego HTTP bloqueado para os restantes destinos.

A aplicação foi novamente compilada e instalada, tendo a comunicação com a API local passado a funcionar.

A configuração local `Frontend:ConfirmUrl` foi também sobreposta através de User Secrets para utilizar o deep link `campuscarpool://confirmar-conta` nos testes de confirmação. Esta alteração não foi efetuada no `appsettings.json` do projeto.

## 5. Quality gates

Executados em 11-11-2026 com JDK 21, Gradle 8.13 e Android SDK 36.

### 5.1 Verificações gerais
Comando executado:

```bash
.\gradlew.bat :core:check :app:ktlintCheck :app:verificarCoberturaApp :app:lintDebug :app:assembleDebug
```

Resultado: **BUILD SUCCESSFUL.**

| Verificação | Resultado |
| --- | --- |
| `:core:check` | Passou |
| `:app:ktlintCheck` | Passou |
| `:app:verificarCoberturaApp` | Passou, incluindo a verificação do limiar de cobertura de 80% |
| `:app:lintDebug` | Passou |
| `:app:assembleDebug` | Passou |

### 5.2 Testes instrumentados
Comando executado com o emulador Android ligado:

```bash
.\gradlew.bat :app:connectedDebugAndroidTest
```

Resultado: **BUILD SUCCESSFUL.**

Os testes instrumentados terminaram sem erro reportado pelo Gradle.

### 5.3 Resultados anteriores
Mantêm-se os resultados do fluxo temporário contra a API real e dos testes anteriormente documentados:

- Fluxo contra a API real: 25 de 25 verificações concluídas.
- Cobertura do núcleo: linhas 337/351 = 96,01%; ramos 265/290 = 91,38%, conforme o registo anterior.
- ViewModels em JVM: 42 de 42, numa cópia sem o Robolectric, conforme o registo anterior.

Os valores históricos de cobertura e testes não devem ser confundidos com os resultados da execução mais recente.

## 6. Decisões e limitações

1. **`AppViewModel` deu lugar a `LoginViewModel`, `RegistoViewModel` e `PerfilViewModel`,** como prevê o modelo de classes. Fechou-se também a validação repetida depois do login: a sessão só é validada ao arrancar, se foi restaurada.
2. **A ligação do email abre, por omissão, a página web.** O valor `Frontend:ConfirmUrl` é único no backend, que está congelado. Por isso a aplicação aceita a ligação colada e `campuscarpool://confirmar-conta`. App Links ficam para quando houver domínio publicado.
3. **Sem pontos de entrada para o perfil de outros, como no web (I070):** as funcionalidades que o abrem chegam nas issues seguintes. Para já há «Ver o meu perfil público» e a ligação `campuscarpool://utilizadores/{id}`.
4. **Fim de sessão no ecrã inicial,** como no UC-F001-05; o protótipo diz «volta ao login». É a decisão tomada na I070.
5. **A API só distingue ligação expirada, usada e inválida no texto,** todas 400. Mostra-se o motivo dela e oferecem-se sempre os dois caminhos.
6. **Fora da I078:** avaliações recebidas no perfil (I084), registo do dispositivo para push (UC-F001-04 A1, UC-F006-02) e `tokenDispositivo` no logout (I083).
7. Testes instrumentados: a execução `connectedDebugAndroidTest` foi concluída com sucesso no emulador. Não se regista aqui uma contagem de testes porque o resultado disponível apenas confirma o sucesso da tarefa Gradle.
8. Ambiente de teste: os testes manuais foram realizados com a API local. A validação contra o ambiente público e SQL Server do ambiente de entrega continua dependente da disponibilidade e configuração desse ambiente.

## 7. Verificações pendentes para a entrega
| Verificação | Situação | Responsável |
| --- | --- | --- |
| Confirmar a inclusão da pasta `docs/capturas-i078/` no projeto | Confirmado | Executora |
| Confirmar a atualização deste documento no projeto |Confirmado | Executora |
| Rever alterações e ficheiros modificados | Confirmado | Executora |
| Confirmar que a branch da I078 deriva da base correta da I077 | Confirmado | Executora |
| Confirmar a integração da correção de network_security_config.xml | Confirmado | Executora |
| Executar pipeline no GitHub | Pendente | Executora |
| Validar a entrega e os artefactos na revisão | Pendente | Revisora |
| Confirmar o ambiente público/SQL Server, quando disponível | Pendente | Revisora |

**Conclusão:** os testes manuais realizados no emulador e as verificações automáticas locais foram concluídos com sucesso. A I078 fica preparada para a fase seguinte de verificação da branch, documentação, pipeline e revisão. Este documento não constitui aprovação da issue nem autorização de merge.