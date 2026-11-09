# I070 — Registo de implementação e validação

Grupo G15 · M4 · Sprint 07 · 2026-11-09. Issue [#30](https://github.com/gobbrin/LDS2627/issues/30).

Executor: Vítor Samuel Vieira Lopes. Revisora: Cristiana Gabriela Oliveira Macedo.
Auditora: Joana Francisca Pacheco Rodrigues. Este registo é do Executor: não é revisão
nem auditoria, e não autoriza o merge.

## Critérios de aceitação

| Critério                                                 | Onde se vê                                                                                                                         |
| -------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Registo, confirmação e início de sessão contra o backend | Fluxo completo no Chrome contra a API local: 202 no registo, 204 na confirmação, entrada com sessão real. Capturas 02, 05, 09 e 15 |
| Contas não confirmadas não acedem às funcionalidades     | O login responde 403 e não cria sessão; a página explica e oferece nova ligação. Captura 10. Rotas privadas exigem sessão          |
| O utilizador edita o seu perfil                          | Alterações persistem depois de recarregar e aparecem no perfil público. Capturas 19, 20 e 22                                       |
| O perfil de outros mostra apenas os dados permitidos     | Só os campos de P-10; sem email na página nem na resposta da API. Captura 23                                                       |
| Erros de validação são apresentados                      | Junto do campo e com a mensagem da API. Capturas 03, 13 e 19                                                                       |

## Requisitos

| Requisito                  | Ecrã                                            | Testes (`src/app/features`)                                        |
| -------------------------- | ----------------------------------------------- | ------------------------------------------------------------------ |
| RF-F001-01                 | `/`                                             | `app.spec.ts`, verificação no browser                              |
| RF-F001-02                 | `/registo`                                      | `autenticacao/registo.page.spec.ts`                                |
| RF-F001-07, 08             | `/confirmar-conta`                              | `autenticacao/confirmacao.page.spec.ts`                            |
| RF-F001-21                 | `/reenviar-confirmacao`                         | `autenticacao/reenvio-confirmacao.page.spec.ts`                    |
| RF-F001-10, 12             | `/entrar`, menu «Terminar sessão»               | `autenticacao/login.page.spec.ts`, `app.spec.ts`                   |
| RF-F001-14, 15, 20, 22     | `/perfil`                                       | `perfil/perfil.page.spec.ts`                                       |
| RF-F001-17, 19, RF-F007-10 | `/utilizadores/:id` (média também em `/perfil`) | `perfil/perfil-publico.page.spec.ts`, `perfil/perfil.page.spec.ts` |

## Estados dos ecrãs

| Ecrã                   | Carregamento           | Erro                                                                   | Vazio                                        |
| ---------------------- | ---------------------- | ---------------------------------------------------------------------- | -------------------------------------------- |
| Registo                | Botão inativo          | Validação; 400, 409, 503 com a conta criada; sem servidor              | —                                            |
| Confirmação            | «A confirmar…»         | Ligação inválida, usada ou expirada; sem servidor, com repetição       | Sem token: não chama a API                   |
| Reenvio                | Botão inativo          | Validação; sem servidor                                                | —                                            |
| Entrada                | Botão inativo          | 401 genérico; 403 com reenvio; sem servidor                            | —                                            |
| Perfil próprio         | «A carregar o perfil…» | Validação; recusa da API; sem servidor, com repetição; sessão revogada | Sem curso, instituição, ligações, avaliações |
| Perfil de outra pessoa | «A carregar o perfil…» | Não encontrado; sem servidor, com repetição; sessão revogada           | Sem curso, instituição, ligações, avaliações |

## Verificação

Corrido a 2026-11-09 sobre o código deste commit, com Node 24.22.2 e Chrome 156.

| Verificação                | Resultado                                              |
| -------------------------- | ------------------------------------------------------ |
| `npm run format:check`     | Passou                                                 |
| `npm run lint`             | Passou, sem erros nem avisos                           |
| `npm test` (Karma/Chrome)  | 196/196; cobertura de linhas 99,87 % (mínimo 80 %)     |
| `npm run build`            | Passou; 284,17 kB iniciais (limite 500 kB)             |
| Browser contra a API local | 458/458, sem erros JavaScript                          |
| Pipeline do frontend web   | Corre no push; o resultado fica ligado ao Pull Request |

As 458 verificações no browser dividem-se em registo (30), confirmação (40), entrada e
saída (48), perfil próprio (93), perfil de outro utilizador (35) e 212 na passagem por 25
estados de ecrã. Essa passagem mede cada estado a 360, 768, 1280 e 1920 px, sem
transbordo horizontal e com um só `h1`, incluindo nome, curso e instituição no tamanho
máximo e um URL de cerca de 1900 caracteres.

### Como foi testado

A API local executa o `Program.cs` real, com JWT, limitador e CORS reais. Difere da
produção em dois pontos: usa SQLite em memória e escreve os tokens de confirmação num
ficheiro, em vez de enviar email. O 503 do registo é provocado por um serviço de email
simulado.

### Não verificado

- SQL Server e ambiente público (Azure): continuam pendentes U2, U3, U4 e U22, fora
  desta tarefa.
- Receção real do email de confirmação.
- Ligação expirada contra a API real, porque não dá para avançar o relógio do serviço.
  Está coberta no Karma com o texto exato da API.

## Diferenças face ao protótipo e ao modelo

1. **Terminar sessão vai para `/`.** O protótipo refere o login; o UC-F001-05 e o
   RF-F001-01 pedem o ecrã inicial, e seguiu-se a especificação. Com a sessão já expirada,
   vai para `/entrar` (fluxo A1).
2. **«Voltar ao perfil» na edição.** Não está no protótipo. As ligações guardam-se de
   imediato (uma chamada por operação) e «Cancelar» só afeta nome, curso e instituição;
   o texto da secção explica-o.
3. **Consulta do perfil de outra pessoa em `/utilizadores/:id`,** sem pontos de entrada
   por agora. O UC-F001-10 abre-a a partir de pesquisa, viagem, conversa ou pedido, que
   chegam com as tarefas seguintes.
4. **Fora desta tarefa:** a lista de avaliações recebidas (I076, UC-F007-03) e os botões
   «Avaliar» (I076) e «Enviar mensagem» (I074) do ecrã [05]. O perfil só mostra a média.
5. **O modelo de classes antigo** não tem as rotas `/autenticacao/*` atuais, o reenvio
   nem as ligações externas. Seguiu-se a API, como na I069.

## Capturas

Em [`capturas-i070/`](capturas-i070/), a 1280 px e, nos ecrãs principais, também a
360 px. Os dados são fictícios.

| N.º     | Conteúdo                                                  |
| ------- | --------------------------------------------------------- |
| 01      | Página inicial de visitante                               |
| 02 a 05 | Registo: formulário, validação, sem servidor, concluído   |
| 06 a 09 | Confirmação: inválida, sem token, a carregar, concluída   |
| 10      | Entrada com conta por confirmar                           |
| 11, 12  | Pedido de nova ligação                                    |
| 13, 14  | Entrada: credenciais erradas, sem servidor                |
| 15      | Área principal com sessão                                 |
| 16 a 18 | Perfil: a carregar, vazio, sem servidor                   |
| 19 a 22 | Edição: validação, ligação, dados longos, perfil completo |
| 23, 24  | Perfil de outra pessoa: dados públicos, inexistente       |
| 25      | Rota inexistente                                          |

## Pendente de terceiros

Revisão (Cristiana): executar os cenários dos critérios contra o backend e confirmar a
pipeline. Auditoria (Joana): arquivo da revisão, checklist, datas e autorização do merge.
Este trabalho assenta na I069, que ainda não foi integrada em `develop`.
