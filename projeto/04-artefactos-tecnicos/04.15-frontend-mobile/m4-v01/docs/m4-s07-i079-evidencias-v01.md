# Evidências da I079: F002 no frontend mobile (v01)

G15 · LDS2627 · m4 · Sprint 07 · 09-11-2026. Issue [#37](https://github.com/gobbrin/LDS2627/issues/37). Regista o que foi executado e o que falta; não é relatório assinado nem autorização de merge.

Executora: Joana Francisca Pacheco Rodrigues. Revisor: Vítor Samuel Vieira Lopes. Auditor: Gil Gabriel da Cunha Leão.

**Estado: preparada para revisão, com verificações por executar (secção 5).** Os ecrãs Compose nunca correram: não houve Robolectric nem dispositivo nesta preparação.

## 1. Critérios do Backlog v08

| Critério | Situação | Evidência |
| --- | --- | --- |
| O condutor cria, consulta, altera e indisponibiliza as suas viagens | Verificado ao nível dos ViewModels contra a API real. Ecrãs por executar | Secção 3, passos 8 a 12 e 22 |
| O estado e os lugares disponíveis são apresentados e atualizados | Dados relidos da API depois de cada operação, verificados contra a API real. Apresentação nos ecrãs por executar | Passos 9 a 12, 18 e 22 |
| Não existe opção de eliminação permanente | Verificado: não há botão nem chamada `DELETE` em `ApiService`; a API responde 405 e o registo continua a existir | Passos 22, 24 e 27; `ViagensEcrasTest.naoHaOpcaoDeEliminar` por executar |
| Dados inválidos são rejeitados com mensagem clara | Validação e mensagens verificadas nos ViewModels e contra a API. Apresentação nos ecrãs por executar | Passos 2 a 5, 13 e 19; `ValidacaoViagemTest` |

## 2. Requisitos e casos de uso

Requisitos de F002 com componente mobile: RF-F002-01, 07, 09, 15 e 17. Os restantes de F002 são do backend e estão repetidos no cliente só para usabilidade (RF-F002-02, 03, 04, 08 e 16).

| Caso de uso | Ecrã | Exceções tratadas |
| --- | --- | --- |
| UC-F002-01 Criar viagem | `CriarViagemScreen` | E1 a E4 no cliente e da API; E5 mensagem da API (422); E6 mensagem da API, com os dados mantidos |
| UC-F002-02 Consultar as minhas viagens | `MinhasViagensScreen` | A1 oferece criar; carregamento, erro e repetição |
| UC-F002-03 Alterar viagem | `GerirViagemScreen` | Validações iguais à criação; lugares abaixo dos ocupados; serviço de mapas em baixo; 409 volta a ler; só viagens ativas e por partir |
| UC-F002-04 Indisponibilizar viagem | `GerirViagemScreen` | Confirmação com aviso dos cancelamentos; 409 volta a ler; outras falhas na própria zona |

## 3. Fluxo contra a API real

ViewModels reais com `ApiFactory`, `SessaoManager` e a API local (`Program.cs` real, JWT, limitador, SQLite em memória, Serviço de Mapas simulado). Teste temporário, fora da entrega. Resultado completo em `evidencias/i079/fluxo-real-i079.log`: **27 de 27**.

| N.º | Verificação |
| --- | --- |
| 1 | Lista vazia |
| 2 a 5 | Dados inválidos não chegam à API: campos em falta, mesmo local, 9 lugares, partida passada |
| 6 e 7 | Criar com o serviço de mapas em baixo: recusado com mensagem, sem criar viagem |
| 8 e 9 | Viagem criada, com a rota; lista com lugares e partida em UTC |
| 10 a 12 | Gestão com estado e campos em hora de Portugal; alteração relida do servidor e visível na lista |
| 13 | A alteração usa as mesmas validações |
| 14 e 15 | Alterar com o serviço de mapas em baixo: recusado, viagem intacta |
| 16 e 17 | Outro utilizador não gere; a API recusa a alteração com 403 |
| 18 a 20 | Participação aceite real reduz os lugares; a API recusa 0 lugares; lugares iguais aos ocupados aceites |
| 21 | Viagem inexistente, sem repetição |
| 22 a 24 | Indisponibilizada; sai da lista de ativas; registo preservado |
| 25 e 26 | Alterar ou indisponibilizar outra vez: 409 |
| 27 | `DELETE /viagens/{id}` responde 405 |

## 4. Quality gates

Executados a 09-11-2026 com JDK 21.0.7, Gradle 8.13 e Android SDK 36, em modo offline. Registo em `evidencias/i079/gates-i079.log`.

| Verificação | Resultado |
| --- | --- |
| `:core:check` | Passou. 83 de 83 testes (61 anteriores e 22 novos) |
| Cobertura do núcleo | Linhas 434/470 = 92,34 %; ramos 348/374 = 93,05 % |
| `:app:ktlintCheck` | Passou, incluindo os testes |
| ViewModels em JVM | 66 de 66 (42 anteriores e 24 novos), numa cópia sem o Robolectric |
| `:app:lintDebug` | Sem problemas, na mesma cópia |
| `:app:assembleDebug` e `:app:assembleDebugAndroidTest` | Os dois APK gerados |
| Compilação de `EcrasTest`, `NavegacaoTest` e `ViagensEcrasTest` | Compilam, mas não correram |

## 5. Por executar

| Verificação | Motivo | Quem |
| --- | --- | --- |
| Testes Robolectric dos ecrãs e da navegação (`EcrasTest`, `NavegacaoTest`, `ViagensEcrasTest`) | O Maven Central recusa transferências a esta máquina (429, cerca de 24 h desde 09-11 às 21:47) | Executora, ou a pipeline no primeiro push |
| `:app:verificarCoberturaApp` (limiar de 80 %) | Depende dos testes acima; ainda não medido | Idem |
| `:app:lintDebug` no projeto completo | Depende dos mesmos ficheiros | Idem |
| `:app:connectedDebugAndroidTest` e fluxos num dispositivo Android 10+, incluindo os seletores de data e hora | Sem dispositivo nem aceleração para o emulador neste PC | Executora |
| Capturas dos ecrãs e fluxos | Idem | Executora |
| Pipeline no GitHub | Só corre depois do push | Executora |
| SQL Server, ambiente público e Serviço de Mapas real | API local em SQLite com mapas simulados; a API pública é anterior à I110 | Revisor, quando disponível |

Os testes dos ecrãs foram escritos sem nunca terem corrido: podem precisar de acertos.

## 6. Decisões e limitações

1. **Lista só com viagens ativas.** É o que `GET /viagens/minhas` devolve (RF-F002-15, DEF-BE-010). As viagens noutros estados pertencem ao histórico (RF-F009-02). Decisão igual à da I071.
2. **Hora de Portugal, independentemente do telemóvel.** O `DataHoraFormatter` usa `Europe/Lisbon` (RNF-FIA-03, modelo de classes), à frente do fuso do aparelho. O campo da hora indica-o.
3. **Sem mapa.** O ecrã 07 do protótipo prevê mapa, mas a API não devolve a geometria (DEF-BE-054, FE-7). Mostram-se a distância e a duração.
4. **Origem e destino alteráveis.** O ecrã 13 do protótipo só altera data, hora e lugares; RF-F002-07 e UC-F002-03 permitem os cinco campos, e a I071 também.
5. **Depois de criar volta-se à lista,** com a rota numa mensagem. UC-F002-01 diz «apresenta os detalhes»; o detalhe é a I080. Decisão igual à da I071.
6. **`indisponibilizar` está no `ViagemFormViewModel`.** O modelo de classes põe-no no `MinhasViagensViewModel`. A ação fica no ecrã de gestão, que já tem o estado que ela atualiza.
7. **Conclusão manual fora do âmbito.** `POST /viagens/{id}/concluir` não faz parte dos critérios nem dos requisitos mobile da I079.
8. **RTM não atualizada:** a RTM v3 é a I104; até lá vale a secção 2.
