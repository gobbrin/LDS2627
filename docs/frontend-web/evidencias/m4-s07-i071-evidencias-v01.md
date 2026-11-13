# I071 — Registo de implementação e validação

Grupo G15 · M4 · Sprint 07 · 2026-11-09. Issue [#34](https://github.com/gobbrin/LDS2627/issues/34).

Executor: Joana Francisca Pacheco Rodrigues. Revisor: Gil Gabriel da Cunha Leão.
Auditor: Vítor Samuel Vieira Lopes. Este registo foi preparado em entreajuda pelo Vítor,
para o Executor confirmar antes de submeter. Não é revisão nem auditoria, e não autoriza o merge.

## Critérios de aceitação

| Critério                                                         | Onde se vê                                                                                                      |
| ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| O condutor cria, consulta, altera e indisponibiliza              | Fluxo completo no Chrome contra a API local, confirmado também por chamadas diretas. Capturas 04, 05, 06 e 08   |
| O estado e os lugares disponíveis são apresentados e atualizados | Lista e gestão; depois de alterar ou indisponibilizar, os dados voltam a ser lidos da API. Capturas 04, 06 e 08 |
| Não existe opção de eliminação permanente                        | Sem botão nem chamada `DELETE`; o registo continua a existir depois de indisponibilizado. Captura 08            |
| Dados inválidos são rejeitados com mensagem clara                | Validação junto do campo e mensagem da API. Capturas 02 e 03                                                    |

## Requisitos

| Requisito    | Ecrã                                   | Testes                              |
| ------------ | -------------------------------------- | ----------------------------------- |
| RF-F002-01   | `/viagens/nova`                        | `features/viagens/viagens.spec.ts`  |
| RF-F002-07   | `/viagens/:id/editar`                  | `features/viagens/viagens.spec.ts`  |
| RF-F002-09   | `/viagens/:id/editar`, com confirmação | `features/viagens/viagens.spec.ts`  |
| RF-F002-15   | `/viagens`                             | `features/viagens/viagens.spec.ts`  |
| RF-F002-17   | `/viagens/nova`, `/viagens/:id/editar` | Karma e API real com mapas em baixo |
| RR-05, RR-10 | Formulário só com os cinco campos      | `features/viagens/viagens.spec.ts`  |

## Estados dos ecrãs

| Ecrã    | Carregamento                  | Erro                                                                           | Vazio                               |
| ------- | ----------------------------- | ------------------------------------------------------------------------------ | ----------------------------------- |
| Lista   | «A carregar as tuas viagens…» | Mensagem com repetição                                                         | «Ainda não tens viagens ativas.»    |
| Criação | Botão inativo                 | Validação; 400, 422; 502, 503 e 504 do serviço de mapas, com os dados mantidos | —                                   |
| Gestão  | «A carregar a viagem…»        | 404 sem repetição; outro condutor; conflito (409) com os dados atualizados     | Viagem não alterável: só a consulta |

## Verificação

Corrido a 2026-11-10, depois da correção do fuso horário, com Node 24.22.2 e Chrome 156.

| Verificação                 | Resultado                                                       |
| --------------------------- | --------------------------------------------------------------- |
| `npm run format:check`      | Passou                                                          |
| `npm run lint`              | Passou, sem erros nem avisos                                    |
| `npm test` (Karma/Chrome)   | 228/228; linhas 99,91 %, ramos 96,64 % (mínimo 80 % de linhas)  |
| `npm run build`             | Passou; 285,69 kB iniciais                                      |
| Fluxos no Chrome, API local | 75/75, a 360, 768, 1280 e 1920 px, com textos de 200 caracteres |
| Regressão de layout da I070 | 212/212, a 2026-11-09; a correção do fuso não alterou o layout  |
| Browser em Nova Iorque      | 9/9 no Chrome: lista, criação e gestão na hora de Portugal      |
| Funções em 6 fusos          | 9/9 casos e 35 040 conversões de ida e volta em cada fuso       |

A API local é o `Program.cs` real com SQLite em memória, Serviço de Mapas simulado e email em ficheiro.

## Decisões e limitações

- **Lista só com viagens ativas.** É o que `GET /viagens/minhas` devolve (RF-F002-15 na RTM v02, DEF-BE-010). As viagens noutros estados pertencem ao histórico (RF-F009-02).
- **Sem mapa.** O ecrã 07 do protótipo prevê mapa, mas a API não devolve a geometria (DEF-BE-054, FE-7). Mostram-se a distância e a duração.
- **Conclusão manual fora do âmbito.** `POST /viagens/{id}/concluir` não faz parte dos critérios nem dos requisitos web da I071.
- **Serviço no formulário.** O modelo de classes liga `ViagemFormComponent` ao `ViagemApiService`. Aqui o formulário emite os dados e as páginas chamam o serviço, porque a criação e a alteração usam chamadas diferentes.
- **Fuso horário.** Os campos e as datas usam a hora de Portugal continental, seja qual for o fuso do browser (RNF-FIA-04). Nas mudanças de hora, uma hora repetida conta como a primeira e uma hora inexistente avança, como no mobile.
- **Não verificado:** SQL Server, ambiente público e Serviço de Mapas real; aceitação de pedidos real, com participações ocupadas contra a API (coberta no Karma, depende da I073 na interface).
- **RTM não atualizada:** a RTM v3 é a I104; até lá vale a tabela de requisitos acima.

## Capturas (`capturas-i071/`, dados fictícios)

| Ficheiro                            | Conteúdo                                  |
| ----------------------------------- | ----------------------------------------- |
| `01-lista-vazia.png`                | Lista sem viagens                         |
| `02-criar-erros.png`                | Validação do formulário                   |
| `03-criar-mapas-indisponiveis.png`  | Serviço de mapas em baixo, dados mantidos |
| `04-lista-com-viagem.png`           | Viagem criada, com a rota                 |
| `05-gerir-viagem.png`               | Estado, lugares e formulário              |
| `06-viagem-alterada.png`            | Dados atualizados                         |
| `07-confirmar-indisponibilizar.png` | Confirmação, com os cancelamentos         |
| `08-viagem-indisponibilizada.png`   | Estado final, sem ações                   |
| `09-*-360.png`                      | Lista, criação e gestão a 360 px          |
