# Especificação dos testes de sistema do frontend web

| Campo | Informação |
| --- | --- |
| **Projeto** | G15 — Campus Carpool |
| **Artefacto** | `m4-especificacao-testes-sistema-frontend-web-v01.md` |
| **Milestone / Sprint** | m4 / Sprint 07 |
| **Issue de origem** | I086 |
| **Pasta de destino** | `/projeto/03-artefactos-de-qualidade-de-produto/03.02-frontend-web/03.02.02-testes-de-sistema` |
| **Versão** | v01, 10/11/2026 |

## 1. Objetivo

Definir, de forma verificável, o que os testes de sistema automáticos do frontend web devem demonstrar. Cada cenário indica precondições, dados, passos, resultado esperado e requisitos que verifica. A implementação pertence à I087 e o registo dos resultados à I097. Este documento não contém resultados de execução.

Segue a estratégia e o ambiente do plano de testes (`m4-plano-testes-frontend-web-v01.md`, I085).

## 2. Âmbito

Abrange os **58 requisitos funcionais com componente web** (coluna «Âmb.» com W) da Especificação de Requisitos v02, nas nove funcionalidades do plano, e os requisitos não funcionais observáveis no browser.

| Funcionalidade | Conteúdo | Implementação | Base dos cenários |
| --- | --- | --- | --- |
| F001 | Registo, confirmação, sessão, perfil | I070 | Requisitos, casos de uso e ecrãs existentes |
| F002 | Criação e gestão de viagens | I071 | Requisitos, casos de uso e ecrãs existentes |
| F003 | Pesquisa e consulta de viagens | I072 | Requisitos e casos de uso; ecrãs por implementar |
| F004 | Pedidos e simulação | I073 | Requisitos e casos de uso; ecrãs por implementar |
| F005 | Mensagens e avisos | I074 | Requisitos e casos de uso; ecrãs por implementar |
| F006 | Notificações e tempo real | I075 | Requisitos e casos de uso; ecrãs por implementar |
| F007 | Avaliações | I076 | Requisitos e casos de uso; ecrãs por implementar |
| F008 | Mapas, rotas e custo | I072 | Requisitos e casos de uso; ecrãs por implementar |
| F009 | Histórico | I076 | Requisitos e casos de uso; ecrãs por implementar |

**Requisitos não funcionais:** RNF-COM-01, RNF-FIA-04, RNF-SEG-12, RNF-USA-01, RNF-USA-02 e RNF-USA-03.

**Fora de âmbito:**

- Requisitos só de backend ou só de mobile, verificados pelos respetivos planos.
- RNF-COM-03 e RNF-SEG-08, verificados por inspeção do código, e RNF-SEG-01 (HTTPS), verificado no ambiente público.
- RNF-MAN-02 e RNF-MAN-03, verificados na pipeline (I089 e I095).
- Implementação dos testes (I087), execução e relatório (I097) e testes de aceitação (I088).

### 2.1. Estado da base de trabalho

Os textos e identificadores de F001 e F002 foram confrontados com o código da branch `feature/i071-criacao-gestao-viagens-web`, que inclui a I070. Se esse código mudar antes da I087, a especificação é atualizada numa nova versão.

Os ecrãs de F003 a F009 ainda não existem. Os respetivos cenários descrevem comportamento observável, sem nomes de campos nem rotas. Só se cita texto exato quando vem de um requisito ou de uma mensagem da API. As I072 a I076 devem dar identificadores estáveis (`id` ou `data-testid`) aos elementos usados aqui.

## 3. Convenções

**Identificador:** `ST-WEB-<área>-<número>`, em que a área é a funcionalidade ou `TR`, para os cenários transversais. Um identificador não é reutilizado.

**Etiquetas do título:**

| Etiqueta | Significado |
| --- | --- |
| `[S]` | Cenário de sucesso ou fluxo alternativo válido |
| `[E]` | Cenário de erro, rejeição ou falha de serviço |
| `[FP]` | Pertence a um fluxo principal do glossário da Especificação de Requisitos (secção 8) |

**Texto entre « »:** texto exato a comparar no ecrã. A comparação ignora espaços nas pontas e, quando o requisito o escreve de outra forma, maiúsculas.

**Mensagens no ecrã:** os erros têm `role="alert"` e as confirmações `role="status"`. Em F001 e F002 os testes usam os `id` dos campos (`#email`, `#origem`), os textos dos botões e as rotas indicadas.

**Automatização:** todos os cenários são automatizáveis com Selenium WebDriver e com as operações auxiliares da secção 4.4. Os testes usam esperas explícitas por condição, nunca tempos fixos. Quando um cenário depender de uma capacidade específica do Chrome, isso é indicado no cenário.

**Critério de passagem:** um cenário passa quando todos os pontos do resultado esperado se verificam. Um cenário que não possa correr regista-se como saltado, com motivo.

## 4. Preparação

### 4.1. Ambiente

Ambientes E1 (local) e E2 (pipeline) do plano: frontend em `http://localhost:4200` e API real em `http://localhost:5100`, com a base de dados migrada e os serviços de email e de mapas substituídos. Fuso do browser: `Europe/Lisbon`, exceto em ST-WEB-TR-04.

**Execução:** o conjunto completo corre em Chrome. Os cenários `[FP]` repetem-se em Chrome, Firefox, Edge e Safari, para verificar RNF-COM-01, com o número de cada versão no relatório.

### 4.2. Contas

Cada teste cria as suas contas, com email único no domínio aceite pela API de teste (`<papel>-<uuid>@estg.ipp.pt`) e uma palavra-passe válida gerada na execução (`P-VALIDA`: 8 ou mais caracteres, com letra e dígito). Não se usam dados pessoais reais.

| Conta | Papel | Estado |
| --- | --- | --- |
| `NC` | Utilizador por confirmar | Registada, sem confirmação |
| `CD` | Condutor | Confirmada |
| `PA` | Passageiro A | Confirmada |
| `PB` | Passageiro B | Confirmada |
| `T3` | Terceiro, sem relação com a viagem | Confirmada |

«Com sessão de X» significa que X iniciou sessão antes do primeiro passo. Só os cenários de início de sessão usam o formulário; os restantes podem reutilizar uma sessão.

### 4.3. Viagens

As datas são relativas ao instante T da execução. A origem e o destino levam um sufixo único por teste, para as pesquisas serem determinísticas.

| Viagem | Condutor | Partida | Lugares | Estado |
| --- | --- | --- | --- | --- |
| `V-FUT` | `CD` | T + 2 dias | 3 | Ativa |
| `V-UM` | `CD` | T + 2 dias | 1 | Ativa |
| `V-CHEIA` | `CD` | T + 2 dias | 1, com `PA` aceite | Ativa, 0 disponíveis |
| `V-PART` | `CD` | T − 1 hora (AUX-06) | 3 | Ativa, já partiu |
| `V-IND` | `CD` | T + 2 dias | 3 | Indisponibilizada |
| `V-CONC` | `CD` | Passada (AUX-07) | 3, com `PA` aceite | Concluída |

### 4.4. Operações auxiliares

Preparam o estado e não são passos do utilizador. As que alteram a base de dados ou simulam serviços existem só em E1 e E2, nunca no ambiente público. A I087 decide como as expõe.

| Código | Operação | Como |
| --- | --- | --- |
| AUX-01 | Criar conta confirmada | `POST /autenticacao/registo`, token de AUX-02, `POST /autenticacao/confirmar` |
| AUX-02 | Obter token de confirmação | Lido do serviço de email substituído |
| AUX-03 | Expirar a ligação de confirmação | Recuar `ExpiraEm` da confirmação 25 horas na base de dados de teste |
| AUX-04 | Passar o limite de reenvio | Recuar a confirmação 2 minutos: a API aceita um reenvio por minuto |
| AUX-05 | Criar dados pela API | `POST /viagens`, `POST /pedidos`, `POST /participacoes/{id}/aceitar` ou `/rejeitar`, com o token do utilizador |
| AUX-06 | Recuar a partida | Alterar `PartidaEm` da viagem para T − 1 hora na base de dados de teste |
| AUX-07 | Concluir viagem | AUX-06, seguido de `POST /viagens/{id}/concluir` |
| AUX-08 | Serviço de mapas indisponível | Ligar a falha simulada do serviço de mapas substituído |
| AUX-09 | Serviço de email indisponível | O serviço de email substituído lança exceção |
| AUX-10 | API inacessível | Cortar a rede do browser (CDP, Chrome) ou parar a API |
| AUX-11 | Segunda sessão | Outro contexto do browser: a sessão fica em `sessionStorage`, por separador |
| AUX-12 | Revogar a sessão | `POST /autenticacao/logout` com o token da sessão aberta |
| AUX-13 | Ler o token da sessão | `sessionStorage`, chave `campus-carpool:sessao:<apiBaseUrl>` |

**Limitadores da API:** 10 pedidos por minuto por IP em login, registo e reenvio. AUX-01 conta para esse limite. A I087 reutiliza sessões ou desliga o limitador na configuração de teste, e declara-o no relatório, como pede o plano.

### 4.5. Defeitos conhecidos que condicionam cenários

| Defeito | Efeito | Cenários |
| --- | --- | --- |
| DEF-BE-041 | O autor não consegue ler a sua avaliação antes de a alterar | ST-WEB-F007-06 |
| DEF-BE-042 | A simulação não traz coordenadas: o percurso simulado não se desenha no mapa | ST-WEB-F004-09 |
| DEF-BE-043 | Um pedido sem novos pontos também depende do serviço de mapas | ST-WEB-F004-07 |
| DEF-BE-045 | A API responde 202 a um email malformado no reenvio, pelo que a validação fica no cliente | ST-WEB-F001-13 |
| DEF-BE-054 | Nenhuma resposta traz a geometria da rota | ST-WEB-F008-01 |
| DEF-BE-056 | Uma ligação ao hub aberta antes do fim de sessão mantém-se até o token expirar | Não condiciona; os cenários de F006 abrem sempre uma sessão nova |

## 5. Cenários

### 5.1. F001 — Registo, autenticação e perfil

#### ST-WEB-F001-01 — Página inicial para visitantes `[S]`

- **Rastreio:** RF-F001-01.
- **Precondições:** sem sessão.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `/`.
- **Resultado esperado:** o `h1` contém «O caminho para o campus»; existem as ligações «Criar conta» (para `/registo`) e «Entrar na comunidade» (para `/entrar`); o menu lateral não é apresentado.

#### ST-WEB-F001-02 — Registo com dados válidos `[S]` `[FP]`

- **Rastreio:** RF-F001-02; UC-F001-01.
- **Precondições:** sem sessão; email ainda não registado.
- **Dados:** nome «Conta de Teste», email único, `P-VALIDA` nos dois campos.
- **Passos:** 1. Abrir `/registo`. 2. Preencher `#nome`, `#email`, `#password` e `#confirmacao`. 3. Premir «Criar conta».
- **Resultado esperado:** o `h1` passa a «Verifica o teu email.»; o email usado aparece no texto, com a validade de 24 horas; existe a ligação «Pede uma nova»; um início de sessão com essa conta é recusado com «Conta por confirmar.».

#### ST-WEB-F001-03 — Registo com campos inválidos `[E]`

- **Rastreio:** RF-F001-02; UC-F001-01; RNF-USA-02.
- **Precondições:** sem sessão.
- **Dados e mensagem esperada, um caso por execução:** nome vazio: «Indica o teu nome.»; nome com 101 caracteres: «O nome tem no máximo 100 caracteres.»; email vazio: «Indica o teu email académico.»; email `nao-e-email`: «Indica um email válido.»; palavra-passe vazia: «Indica uma palavra-passe.»; palavra-passe `abc12` ou `abcdefgh`: «Usa pelo menos 8 caracteres, com uma letra e um dígito.»; confirmação vazia: «Repete a palavra-passe.»; confirmação diferente: «As palavras-passe não coincidem.».
- **Passos:** 1. Abrir `/registo`. 2. Preencher os restantes campos com valores válidos e o campo do caso com o valor indicado. 3. Premir «Criar conta».
- **Resultado esperado:** a mensagem do caso aparece junto do campo; o campo fica com `aria-invalid="true"`; o `h1` continua a ser «Cria a tua conta.»; não é enviado nenhum pedido à API.

#### ST-WEB-F001-04 — Registo com domínio de email não permitido `[E]`

- **Rastreio:** RF-F001-02; UC-F001-01; RNF-USA-02.
- **Precondições:** sem sessão.
- **Dados:** email `teste@gmail.com`, restantes campos válidos.
- **Passos:** 1. Abrir `/registo`. 2. Preencher e premir «Criar conta».
- **Resultado esperado:** alerta «É necessário um email académico permitido.»; o formulário mantém-se.

#### ST-WEB-F001-05 — Registo com email já usado `[E]`

- **Rastreio:** RF-F001-02; UC-F001-01.
- **Precondições:** conta `CD` criada (AUX-01); sem sessão.
- **Dados:** email de `CD`, restantes campos válidos.
- **Passos:** 1. Abrir `/registo`. 2. Preencher e premir «Criar conta».
- **Resultado esperado:** alerta «Email já registado.»; o formulário mantém-se.

#### ST-WEB-F001-06 — Falha no envio do email de confirmação `[E]`

- **Rastreio:** RF-F001-02; UC-F001-01.
- **Precondições:** AUX-09 ativo; sem sessão.
- **Dados:** registo válido.
- **Passos:** 1. Abrir `/registo`. 2. Preencher e premir «Criar conta».
- **Resultado esperado:** o `h1` passa a «Falta a confirmação.»; alerta «Conta criada, mas o envio do email falhou. Aguarda um minuto e pede o reenvio da ligação de confirmação.»; existe a ligação «Pede uma nova».

#### ST-WEB-F001-07 — Confirmação dentro do prazo `[S]` `[FP]`

- **Rastreio:** RF-F001-07; UC-F001-02.
- **Precondições:** conta `NC`; token obtido (AUX-02).
- **Dados:** token de `NC`.
- **Passos:** 1. Abrir `/confirmar-conta?token=<token>`.
- **Resultado esperado:** navegação para `/entrar`; mensagem «Conta confirmada. Já podes entrar.»; o início de sessão de `NC` passa a ser aceite.

#### ST-WEB-F001-08 — Ligação de confirmação expirada `[E]`

- **Rastreio:** RF-F001-08; UC-F001-02.
- **Precondições:** conta `NC`; token obtido; AUX-03 aplicado.
- **Dados:** token de `NC`.
- **Passos:** 1. Abrir `/confirmar-conta?token=<token>`.
- **Resultado esperado:** `h1` «Esta ligação não pode ser usada.»; alerta «Esta ligação expirou. Pede uma nova ligação de confirmação.»; ligações «Pedir nova ligação» e «Ir para a entrada»; o início de sessão de `NC` continua a ser recusado.

#### ST-WEB-F001-09 — Ligação de confirmação já usada `[E]`

- **Rastreio:** RF-F001-08; UC-F001-02.
- **Precondições:** conta confirmada com o token indicado.
- **Dados:** o mesmo token.
- **Passos:** 1. Abrir `/confirmar-conta?token=<token>`.
- **Resultado esperado:** `h1` «Esta ligação não pode ser usada.»; alerta «Esta ligação já foi usada. Se a conta está confirmada, inicia sessão.».

#### ST-WEB-F001-10 — Ligação de confirmação inválida `[E]`

- **Rastreio:** RF-F001-08; UC-F001-02.
- **Precondições:** nenhuma.
- **Dados:** caso A, sem parâmetro `token`; caso B, token inventado.
- **Passos:** 1. Abrir `/confirmar-conta` com os dados do caso.
- **Resultado esperado:** `h1` «Esta ligação não pode ser usada.»; caso A: alerta «Esta ligação de confirmação não é válida. Pede uma nova ligação.»; caso B: alerta «Ligação de confirmação inválida. Pede uma nova ligação.».

#### ST-WEB-F001-11 — Reenvio da ligação de confirmação `[S]`

- **Rastreio:** RF-F001-21; RF-F001-07; UC-F001-03; UC-F001-02.
- **Precondições:** conta `NC`; token antigo guardado; AUX-04 aplicado.
- **Dados:** email de `NC`.
- **Passos:** 1. Abrir `/reenviar-confirmacao`. 2. Preencher `#email` e premir «Enviar nova ligação». 3. Obter o token novo (AUX-02). 4. Abrir a ligação com o token antigo. 5. Abrir a ligação com o token novo.
- **Resultado esperado:** depois do passo 2, `h1` «Pedido recebido.»; no passo 4, `h1` «Esta ligação não pode ser usada.»; no passo 5, navegação para `/entrar` e «Conta confirmada. Já podes entrar.».

#### ST-WEB-F001-12 — Reenvio para email sem conta `[S]`

- **Rastreio:** RF-F001-21; UC-F001-03.
- **Precondições:** sem sessão.
- **Dados:** email académico válido sem conta.
- **Passos:** 1. Executar o pedido de reenvio para o email sem conta e guardar o texto com `role="status"`. 2. Repetir para uma conta `NC`, depois de AUX-04.
- **Resultado esperado:** nos dois casos o `h1` é «Pedido recebido.» e o texto guardado é igual.

#### ST-WEB-F001-13 — Reenvio com email malformado `[E]`

- **Rastreio:** RF-F001-21; UC-F001-03; RNF-USA-02.
- **Precondições:** sem sessão.
- **Dados:** caso A, vazio; caso B, `nao-e-email`.
- **Passos:** 1. Abrir `/reenviar-confirmacao`. 2. Preencher e premir «Enviar nova ligação».
- **Resultado esperado:** caso A: «Indica o teu email académico.»; caso B: «Indica um email válido.»; o `h1` continua a ser «Pede uma nova ligação.». Ver DEF-BE-045.

#### ST-WEB-F001-14 — Início de sessão válido `[S]` `[FP]`

- **Rastreio:** RF-F001-10; UC-F001-04.
- **Precondições:** conta `CD`; sem sessão.
- **Dados:** credenciais de `CD`.
- **Passos:** 1. Abrir `/entrar`. 2. Preencher `#email` e `#password`. 3. Premir «Entrar».
- **Resultado esperado:** navegação para `/inicio`; menu lateral com «As minhas viagens», «Perfil» e «Terminar sessão».

#### ST-WEB-F001-15 — Credenciais incorretas `[E]`

- **Rastreio:** RF-F001-10; UC-F001-04; RNF-USA-02.
- **Precondições:** conta `CD`; sem sessão.
- **Dados:** email de `CD` e palavra-passe errada; depois, email sem conta.
- **Passos:** 1. Abrir `/entrar`. 2. Preencher e premir «Entrar». 3. Repetir com o email sem conta.
- **Resultado esperado:** nos dois casos, alerta «Credenciais inválidas.», a página continua em `/entrar`, a palavra-passe é limpa e não é criada sessão.

#### ST-WEB-F001-16 — Início de sessão com conta por confirmar `[E]`

- **Rastreio:** RF-F001-10; UC-F001-04.
- **Precondições:** conta `NC`; sem sessão.
- **Dados:** credenciais de `NC`.
- **Passos:** 1. Abrir `/entrar`. 2. Preencher e premir «Entrar». 3. Seguir «Pedir nova ligação».
- **Resultado esperado:** alerta «Conta por confirmar.»; não é criada sessão; no passo 3, `/reenviar-confirmacao` com `#email` já preenchido.

#### ST-WEB-F001-17 — Início de sessão com campos inválidos `[E]`

- **Rastreio:** RF-F001-10; UC-F001-04; RNF-USA-02.
- **Precondições:** sem sessão.
- **Dados:** caso A, os dois campos vazios; caso B, email `nao-e-email`.
- **Passos:** 1. Abrir `/entrar`. 2. Preencher e premir «Entrar».
- **Resultado esperado:** caso A: «Indica o teu email académico.» e «Indica a tua palavra-passe.»; caso B: «Indica um email válido.»; não é enviado nenhum pedido à API.

#### ST-WEB-F001-18 — Terminar sessão `[S]`

- **Rastreio:** RF-F001-12; UC-F001-05.
- **Precondições:** com sessão de `CD`; token guardado (AUX-13).
- **Dados:** nenhum.
- **Passos:** 1. Premir «Terminar sessão». 2. Chamar `GET /autenticacao/me` com o token guardado. 3. Abrir `/perfil`.
- **Resultado esperado:** depois do passo 1, navegação para `/`; no passo 2, resposta 401; no passo 3, navegação para `/entrar`.

#### ST-WEB-F001-19 — Área reservada sem sessão `[E]`

- **Rastreio:** RF-F001-10; UC-F001-04.
- **Precondições:** conta `CD`; sem sessão.
- **Dados:** credenciais de `CD`.
- **Passos:** 1. Abrir `/viagens`. 2. Iniciar sessão no formulário apresentado.
- **Resultado esperado:** no passo 1, caminho `/entrar` com o parâmetro `regresso` igual a `/viagens`; depois do passo 2, caminho `/viagens`.

#### ST-WEB-F001-20 — Sessão revogada durante a utilização `[E]`

- **Rastreio:** RF-F001-12; UC-F001-05.
- **Precondições:** com sessão de `CD`; AUX-12 aplicado depois de a sessão abrir.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `/perfil`.
- **Resultado esperado:** navegação para `/entrar`; alerta «A sessão não é válida. Inicia sessão novamente.»; o menu lateral desaparece.

#### ST-WEB-F001-21 — Consultar o próprio perfil `[S]`

- **Rastreio:** RF-F001-20; RF-F007-10; UC-F001-06.
- **Precondições:** com sessão de `CD`, conta sem curso, instituição, ligações nem avaliações.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `/perfil`.
- **Resultado esperado:** apresenta o nome e o email de `CD`, «Não indicado» no curso, «Não indicada» na instituição, «Sem ligações externas» e «Sem avaliações».

#### ST-WEB-F001-22 — Alterar os dados do perfil `[S]`

- **Rastreio:** RF-F001-14; UC-F001-07.
- **Precondições:** com sessão de `CD`.
- **Dados:** nome com 100 caracteres, curso com 150, instituição com 150.
- **Passos:** 1. Abrir `/perfil` e premir «Editar perfil». 2. Preencher `#nome`, `#curso` e `#instituicao`. 3. Premir «Guardar alterações». 4. Recarregar a página.
- **Resultado esperado:** mensagem «Perfil atualizado.»; os valores novos aparecem antes e depois de recarregar.

#### ST-WEB-F001-23 — Alterar o perfil com dados inválidos `[E]`

- **Rastreio:** RF-F001-14; UC-F001-07; RNF-USA-02.
- **Precondições:** com sessão de `CD`, em modo de edição.
- **Dados e mensagem esperada, um caso por execução:** nome vazio ou só com espaços: «Indica o teu nome.»; nome com 101 caracteres: «O nome tem no máximo 100 caracteres.»; curso com 151: «O curso tem no máximo 150 caracteres.»; instituição com 151: «A instituição tem no máximo 150 caracteres.».
- **Passos:** 1. Preencher o campo do caso. 2. Premir «Guardar alterações». 3. Premir «Cancelar» e recarregar.
- **Resultado esperado:** a mensagem do caso aparece junto do campo; o formulário mantém-se; depois do passo 3, os dados são os anteriores.

#### ST-WEB-F001-24 — Adicionar ligação externa `[S]`

- **Rastreio:** RF-F001-15; UC-F001-08.
- **Precondições:** com sessão de `CD`, em modo de edição, sem ligações.
- **Dados:** `#url` `https://exemplo.pt/perfil`, `#descricao` «Portefólio».
- **Passos:** 1. Preencher e premir «Adicionar ligação». 2. Premir «Voltar ao perfil». 3. Recarregar.
- **Resultado esperado:** mensagem «Ligação adicionada.»; contador «Tens 1 de 5.» depois do passo 1; no perfil há uma ligação com o texto «Portefólio», para esse endereço e com `target="_blank"`.

#### ST-WEB-F001-25 — Ligação externa inválida `[E]`

- **Rastreio:** RF-F001-15; UC-F001-08; RNF-USA-02.
- **Precondições:** com sessão de `CD`, em modo de edição.
- **Dados e mensagem esperada:** vazio: «Indica o endereço da ligação.»; `javascript:alert(1)` e `exemplo.pt`: «Indica um endereço completo, a começar por http:// ou https://.»; descrição com 101 caracteres: «A descrição tem no máximo 100 caracteres.».
- **Passos:** 1. Preencher e premir «Adicionar ligação».
- **Resultado esperado:** a mensagem do caso aparece junto do campo; a lista não muda.

#### ST-WEB-F001-26 — Máximo de ligações atingido `[E]`

- **Rastreio:** RF-F001-15; UC-F001-08.
- **Precondições:** com sessão de `CD`, em modo de edição, com 4 ligações.
- **Dados:** quinta ligação válida.
- **Passos:** 1. Adicionar a quinta ligação.
- **Resultado esperado:** contador «Tens 5 de 5.»; aviso «Atingiste o máximo de 5 ligações. Remove uma para acrescentares outra.»; o formulário de adição desaparece.

#### ST-WEB-F001-27 — Máximo atingido noutra sessão `[E]`

- **Rastreio:** RF-F001-15; UC-F001-08.
- **Precondições:** com sessão de `CD`, em modo de edição, com 4 ligações; a quinta é adicionada pela API depois de a página abrir.
- **Dados:** outra ligação válida.
- **Passos:** 1. Preencher e premir «Adicionar ligação».
- **Resultado esperado:** alerta «O perfil já tem o número máximo de ligações.»; a lista passa a mostrar 5 ligações.

#### ST-WEB-F001-28 — Remover ligação externa `[S]`

- **Rastreio:** RF-F001-22; UC-F001-09.
- **Precondições:** com sessão de `CD`, em modo de edição, com 1 ligação.
- **Dados:** nenhum.
- **Passos:** 1. Premir «Remover». 2. Premir «Não». 3. Premir «Remover». 4. Premir «Sim, remover». 5. Recarregar e abrir o perfil.
- **Resultado esperado:** o passo 1 pergunta «Remover esta ligação?»; depois do passo 2, a ligação continua na lista; depois do passo 4, «Ligação removida.»; depois do passo 5, «Sem ligações externas».

#### ST-WEB-F001-29 — Consultar o perfil de outro utilizador `[S]`

- **Rastreio:** RF-F001-17; RF-F001-19; RF-F007-10; UC-F001-10.
- **Precondições:** `CD` com curso, instituição e uma ligação; com sessão de `T3`.
- **Dados:** identificador de `CD`.
- **Passos:** 1. Abrir `/utilizadores/<id>`.
- **Resultado esperado:** apresenta o nome, o curso, a instituição, a ligação e a classificação de `CD`; o texto da página não contém o email de `CD`.

#### ST-WEB-F001-30 — Perfil de utilizador inexistente `[E]`

- **Rastreio:** RF-F001-19; UC-F001-10.
- **Precondições:** com sessão de `T3`.
- **Dados:** caso A, identificador UUID sem conta; caso B, `abc`.
- **Passos:** 1. Abrir `/utilizadores/<id>`.
- **Resultado esperado:** alerta «Perfil não encontrado.»; existe a ligação «Voltar ao início».

### 5.2. F002 — Criação e gestão de viagens

#### ST-WEB-F002-01 — Criar viagem válida `[S]` `[FP]`

- **Rastreio:** RF-F002-01; UC-F002-01.
- **Precondições:** com sessão de `CD`, sem viagens ativas.
- **Dados:** origem e destino diferentes, com sufixo; data de T + 2 dias; hora 10:00; 3 lugares.
- **Passos:** 1. Abrir `/viagens/nova`. 2. Preencher `#origem`, `#destino`, `#data`, `#hora` e `#lugares`. 3. Premir «Criar viagem».
- **Resultado esperado:** navegação para `/viagens`; mensagem a começar por «Viagem criada.»; a lista tem a viagem, com o percurso, o estado «Ativa» e «3 de 3 lugares disponíveis».

#### ST-WEB-F002-02 — Criar viagem com dados inválidos `[E]`

- **Rastreio:** RF-F002-01; UC-F002-01; RNF-USA-02.
- **Precondições:** com sessão de `CD`.
- **Dados e mensagem esperada, um caso por execução:** origem vazia: «Indica a origem.»; origem com 201 caracteres: «A origem tem no máximo 200 caracteres.»; destino vazio: «Indica o destino.»; destino com 201: «O destino tem no máximo 200 caracteres.»; destino igual à origem, em maiúsculas: «A origem e o destino não podem ser o mesmo local.»; data vazia: «Indica a data da viagem.»; hora vazia: «Indica a hora da viagem.»; data e hora passadas: «A data e a hora têm de ser posteriores ao momento atual.»; 0 lugares: «Indica pelo menos 1 lugar.»; 9 lugares: «O máximo é 8 lugares.»; lugares vazio: «Indica o número de lugares.»; `1.5` lugares: «Indica um número inteiro de lugares.».
- **Passos:** 1. Abrir `/viagens/nova`. 2. Preencher os restantes campos com valores válidos e o campo do caso com o valor indicado. 3. Premir «Criar viagem».
- **Resultado esperado:** a mensagem do caso aparece junto do campo; a página continua em `/viagens/nova`; não é criada nenhuma viagem.

#### ST-WEB-F002-03 — Criar viagem com o serviço de mapas indisponível `[E]`

- **Rastreio:** RF-F002-17; UC-F002-01.
- **Precondições:** com sessão de `CD`; AUX-08 ativo.
- **Dados:** dados válidos.
- **Passos:** 1. Abrir `/viagens/nova`. 2. Preencher e premir «Criar viagem». 3. Desligar AUX-08 e abrir `/viagens`.
- **Resultado esperado:** depois do passo 2, alerta com a mensagem informativa da API, a página continua no formulário e os valores mantêm-se; no passo 3, «Ainda não tens viagens ativas.».

#### ST-WEB-F002-04 — Consultar as viagens ativas `[S]`

- **Rastreio:** RF-F002-15; UC-F002-02.
- **Precondições:** com sessão de `CD`; duas viagens `V-FUT` e uma `V-IND` (AUX-05); a uma `V-FUT` falta um lugar, ocupado por `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `/viagens`.
- **Resultado esperado:** a lista tem exatamente 2 entradas; uma com «3 de 3 lugares disponíveis» e outra com «2 de 3 lugares disponíveis»; a viagem indisponibilizada não aparece.

#### ST-WEB-F002-05 — Sem viagens ativas `[S]`

- **Rastreio:** RF-F002-15; UC-F002-02.
- **Precondições:** com sessão de `T3`, sem viagens.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `/viagens`.
- **Resultado esperado:** texto «Ainda não tens viagens ativas.» e ligação «Criar viagem».

#### ST-WEB-F002-06 — Alterar viagem `[S]`

- **Rastreio:** RF-F002-07; UC-F002-03.
- **Precondições:** com sessão de `CD`; `V-FUT` criada.
- **Dados:** nova origem, novo destino, nova data, 5 lugares.
- **Passos:** 1. Em `/viagens`, seguir «Gerir viagem». 2. Confirmar que o formulário vem preenchido. 3. Alterar os campos e premir «Guardar alterações». 4. Recarregar.
- **Resultado esperado:** no passo 2, os campos têm os valores atuais; depois do passo 3, mensagem «Viagem atualizada.»; depois do passo 4, o percurso, a partida e «5 de 5» nos lugares disponíveis refletem a alteração.

#### ST-WEB-F002-07 — Alterar viagem com o serviço de mapas indisponível `[E]`

- **Rastreio:** RF-F002-17; UC-F002-03.
- **Precondições:** com sessão de `CD`; `V-FUT` criada; AUX-08 ativo depois de a página abrir.
- **Dados:** nova origem.
- **Passos:** 1. Abrir a gestão da viagem. 2. Alterar `#origem` e premir «Guardar alterações». 3. Desligar AUX-08 e recarregar.
- **Resultado esperado:** alerta com a mensagem informativa da API; depois do passo 3, a origem é a anterior.

#### ST-WEB-F002-08 — Viagem que já partiu não é alterável `[E]`

- **Rastreio:** RF-F002-07; UC-F002-03.
- **Precondições:** com sessão de `CD`; `V-PART` preparada.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `/viagens/<id>/editar`.
- **Resultado esperado:** texto «Esta viagem já não pode ser alterada.»; não há formulário de alteração.

#### ST-WEB-F002-09 — Oferecer menos lugares do que os ocupados `[E]`

- **Rastreio:** RF-F002-07; UC-F002-03; RNF-USA-02.
- **Precondições:** com sessão de `CD`; `V-FUT` com `PA` e `PB` aceites.
- **Dados:** 1 lugar.
- **Passos:** 1. Abrir a gestão da viagem. 2. Preencher `#lugares` e premir «Guardar alterações».
- **Resultado esperado:** mensagem «Já tens 2 lugares ocupados nesta viagem. Não podes oferecer menos.»; a viagem não muda.

#### ST-WEB-F002-10 — Indisponibilizar viagem `[S]`

- **Rastreio:** RF-F002-09; UC-F002-04.
- **Precondições:** com sessão de `CD`; `V-FUT` criada.
- **Dados:** nenhum.
- **Passos:** 1. Abrir a gestão da viagem. 2. Premir «Indisponibilizar». 3. Premir «Não». 4. Premir «Indisponibilizar». 5. Premir «Sim, indisponibilizar». 6. Abrir `/viagens`.
- **Resultado esperado:** depois do passo 3, a viagem continua «Ativa»; depois do passo 5, «Viagem indisponibilizada.», estado «Indisponibilizada», texto «Esta viagem já não pode ser alterada.»; não existe nenhuma opção de eliminar; no passo 6, a viagem não aparece. A ausência na pesquisa verifica-se em ST-WEB-F003-01.

#### ST-WEB-F002-11 — Gerir viagem de outro condutor ou inexistente `[E]`

- **Rastreio:** RF-F002-07; RF-F002-09; UC-F002-03; UC-F002-04.
- **Precondições:** `V-FUT` de `CD`; com sessão de `T3`.
- **Dados:** caso A, identificador de `V-FUT`; caso B, UUID sem viagem; caso C, `abc`.
- **Passos:** 1. Abrir `/viagens/<id>/editar`.
- **Resultado esperado:** caso A: alerta «Só o condutor pode gerir esta viagem.»; casos B e C: alerta «Viagem não encontrada.»; nos três, ligação «Voltar às minhas viagens» e nenhum formulário.

#### ST-WEB-F002-12 — Viagem alterada noutra sessão `[E]`

- **Rastreio:** RF-F002-07; UC-F002-03.
- **Precondições:** com sessão de `CD`, na gestão de `V-FUT`; a viagem é indisponibilizada pela API depois de a página abrir.
- **Dados:** nova origem.
- **Passos:** 1. Alterar `#origem` e premir «Guardar alterações».
- **Resultado esperado:** alerta com a mensagem da API; a página volta a ler a viagem e apresenta o estado «Indisponibilizada».

### 5.3. F003 — Pesquisa e consulta de viagens

Se o ecrã de pesquisa usar campos nativos de data e hora, um formato inválido não é introduzível (UC-F003-01, E1), e essa exceção fica coberta pelos testes unitários da I072.

#### ST-WEB-F003-01 — Pesquisar sem filtros `[S]` `[FP]`

- **Rastreio:** RF-F003-01; RF-F003-03; RF-F003-04; RF-F002-09; UC-F003-01; UC-F002-04.
- **Precondições:** `V-FUT`, `V-CHEIA`, `V-IND` e `V-PART` criadas; `CD` com classificação média conhecida; com sessão de `T3`.
- **Dados:** nenhum filtro.
- **Passos:** 1. Abrir a pesquisa. 2. Pesquisar.
- **Resultado esperado:** `V-FUT` aparece, com origem, destino, data, hora, duração estimada, lugares disponíveis, número de pontos adicionais, nome do condutor, ligação ao perfil de `CD` e classificação média; `V-CHEIA`, `V-IND` e `V-PART` não aparecem.

#### ST-WEB-F003-02 — Pesquisar por origem e destino `[S]`

- **Rastreio:** RF-F003-01; UC-F003-01.
- **Precondições:** viagens «Porto — ESTG <sufixo>» → «Famalicão <sufixo>» e «Braga <sufixo>» → «Guimarães <sufixo>»; com sessão de `T3`.
- **Dados:** caso A, origem `porto`; caso B, destino `famalicao`; caso C, origem `porto` e destino `guimaraes`; todos com o sufixo.
- **Passos:** 1. Abrir a pesquisa. 2. Preencher os filtros do caso. 3. Pesquisar.
- **Resultado esperado:** casos A e B: só a primeira viagem; caso C: nenhuma, porque os filtros são cumulativos.

#### ST-WEB-F003-03 — Pesquisar por data e hora `[S]`

- **Rastreio:** RF-F003-01; UC-F003-01.
- **Precondições:** viagens com o mesmo sufixo em T + 2 dias às 09:00, T + 2 dias às 15:00 e T + 3 dias às 15:00; com sessão de `T3`.
- **Dados:** caso A, data T + 2 dias; caso B, hora 12:00, sem data; caso C, data T + 2 dias e hora 12:00.
- **Passos:** 1. Abrir a pesquisa. 2. Preencher os filtros do caso. 3. Pesquisar.
- **Resultado esperado:** caso A: as duas de T + 2 dias; caso B: as duas das 15:00; caso C: só a de T + 2 dias às 15:00; sempre por ordem crescente de partida.

#### ST-WEB-F003-04 — Pesquisa sem resultados `[E]`

- **Rastreio:** RF-F003-01; UC-F003-01.
- **Precondições:** com sessão de `T3`.
- **Dados:** origem com um texto que nenhuma viagem contém.
- **Passos:** 1. Abrir a pesquisa. 2. Preencher e pesquisar.
- **Resultado esperado:** a lista fica vazia; é apresentada uma mensagem a indicar que não há viagens com esses critérios e a sugerir alterar os filtros.

#### ST-WEB-F003-05 — Consultar os detalhes de uma viagem `[S]`

- **Rastreio:** RF-F003-06; RF-F003-03; RF-F003-04; UC-F003-02.
- **Precondições:** `V-FUT` criada; com sessão de `T3`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `V-FUT` a partir da pesquisa.
- **Resultado esperado:** apresenta o estado «Ativa», os dados de ST-WEB-F003-01, o acesso ao mapa e a ação de pedir participação.

#### ST-WEB-F003-06 — Detalhes de viagem inexistente ou não disponível `[E]`

- **Rastreio:** RF-F003-06; UC-F003-02.
- **Precondições:** `V-IND` criada; com sessão de `T3`, sem pedido na viagem.
- **Dados:** caso A, UUID sem viagem; caso B, identificador de `V-IND`.
- **Passos:** 1. Abrir os detalhes pelo endereço direto.
- **Resultado esperado:** caso A: mensagem a indicar que a viagem não foi encontrada; caso B: mensagem a indicar que a viagem não está disponível; nos dois, não são apresentados dados da viagem.

#### ST-WEB-F003-07 — Passageiros visíveis ao condutor e aos aceites `[S]`

- **Rastreio:** RF-F003-07; RF-F008-09; UC-F003-02.
- **Precondições:** `V-FUT` com `PA` e `PB` aceites, cada um com pontos próprios.
- **Dados:** nenhum.
- **Passos:** 1. Com sessão de `CD`, abrir os detalhes. 2. Com sessão de `PA`, abrir os detalhes.
- **Resultado esperado:** nos dois passos aparecem os nomes de `PA` e `PB` e os respetivos pontos.

#### ST-WEB-F003-08 — Passageiros ocultos a terceiros `[E]`

- **Rastreio:** RF-F003-07; RF-F003-08; RF-F008-09; UC-F003-02.
- **Precondições:** como em ST-WEB-F003-07; com sessão de `T3`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os detalhes.
- **Resultado esperado:** o texto da página não contém os nomes de `PA` e `PB` nem os seus pontos; a rota entre origem e destino continua acessível.

### 5.4. F004 — Pedido e gestão de participação

#### ST-WEB-F004-01 — Pedir participação com recolha e destino `[S]` `[FP]`

- **Rastreio:** RF-F004-01; RF-F004-02; RF-F004-20; RF-F004-21; UC-F004-01.
- **Precondições:** `V-FUT` criada; com sessão de `PA`.
- **Dados:** recolha e destino próprios, geocodificáveis e diferentes.
- **Passos:** 1. Abrir os detalhes da viagem. 2. Pedir participação com os dois pontos. 3. Abrir os meus pedidos.
- **Resultado esperado:** confirmação do pedido; no passo 3, o pedido aparece com o estado «Pendente».

#### ST-WEB-F004-02 — Pedir participação só com um ponto `[S]`

- **Rastreio:** RF-F004-02; RF-F004-20; RF-F004-21; UC-F004-01.
- **Precondições:** `V-FUT` criada; com sessão de `PA` (caso A) e de `PB` (caso B).
- **Dados:** caso A, só recolha; caso B, só destino.
- **Passos:** 1. Pedir participação com os dados do caso. 2. Com sessão de `CD`, consultar o pedido.
- **Resultado esperado:** o pedido fica registado como «Pendente»; caso A: o destino é o da viagem; caso B: a recolha é a origem da viagem (P-20).

#### ST-WEB-F004-03 — Pedir participação sem pontos `[E]`

- **Rastreio:** RF-F004-02; UC-F004-01; RNF-USA-02.
- **Precondições:** `V-FUT` criada; com sessão de `PA`.
- **Dados:** recolha e destino vazios.
- **Passos:** 1. Abrir o pedido de participação. 2. Submeter.
- **Resultado esperado:** mensagem a pedir pelo menos um ponto; nenhum pedido criado.

#### ST-WEB-F004-04 — Pedir participação numa viagem que deixou de o permitir `[E]`

- **Rastreio:** RF-F004-01; UC-F004-01; RNF-USA-02.
- **Precondições:** `V-UM` com o formulário de pedido aberto por `PB`; depois de abrir, aplicar o caso.
- **Dados:** caso A, `PA` aceite, sem lugares; caso B, viagem indisponibilizada; caso C, partida recuada (AUX-06).
- **Passos:** 1. Submeter o pedido com um ponto válido.
- **Resultado esperado:** alerta com o motivo: caso A, «Não existem lugares disponíveis nesta viagem.»; nos casos B e C, mensagem da API a indicar que a viagem já não aceita pedidos; nenhum pedido criado.

#### ST-WEB-F004-05 — O condutor não pode pedir na própria viagem `[E]`

- **Rastreio:** RF-F004-01; UC-F004-01.
- **Precondições:** `V-FUT` criada; com sessão de `CD`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os detalhes da viagem.
- **Resultado esperado:** a ação de pedir participação não é oferecida.

#### ST-WEB-F004-06 — Pontos não permitidos `[E]`

- **Rastreio:** RF-F004-20; RF-F004-21; UC-F004-01; RNF-USA-02.
- **Precondições:** `V-FUT` criada; com sessão de `PA`.
- **Dados:** caso A, recolha igual ao destino da viagem; caso B, destino igual à origem da viagem.
- **Passos:** 1. Pedir participação com os dados do caso.
- **Resultado esperado:** alerta a indicar que os pontos não são válidos; nenhum pedido criado.

#### ST-WEB-F004-07 — Pedir participação com o serviço de mapas indisponível `[E]`

- **Rastreio:** RF-F004-01; UC-F004-01; RNF-USA-02.
- **Precondições:** `V-FUT` criada; com sessão de `PA`; AUX-08 ativo.
- **Dados:** recolha e destino próprios.
- **Passos:** 1. Pedir participação.
- **Resultado esperado:** alerta com mensagem informativa; nenhum pedido criado; os valores mantêm-se no formulário. Ver DEF-BE-043: o pedido sem novos pontos também é recusado.

#### ST-WEB-F004-08 — Consultar os pedidos pendentes de uma viagem `[S]`

- **Rastreio:** RF-F004-08; UC-F004-02.
- **Precondições:** viagens V1 e V2 de `CD`; `PA` e `PB` com pedido em V1 e `T3` com pedido em V2; com sessão de `CD`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os pedidos de V1.
- **Resultado esperado:** a lista tem exatamente os pedidos de `PA` e `PB`.

#### ST-WEB-F004-09 — Simulação antes da decisão `[S]`

- **Rastreio:** RF-F004-09; UC-F004-03.
- **Precondições:** `V-FUT` com pedido pendente de `PA`; com sessão de `CD`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os pedidos da viagem. 2. Abrir a simulação do pedido. 3. Voltar aos detalhes da viagem.
- **Resultado esperado:** no passo 2 aparecem o percurso e a duração estimada com a participação; no passo 3, os dados da viagem e os lugares não mudaram. Ver DEF-BE-042: o percurso apresenta-se como lista de pontos, sem desenho no mapa.

#### ST-WEB-F004-10 — Simulação com o serviço de mapas indisponível `[E]`

- **Rastreio:** RF-F004-09; UC-F004-03.
- **Precondições:** como em ST-WEB-F004-09; AUX-08 ativo.
- **Dados:** nenhum.
- **Passos:** 1. Abrir a simulação. 2. Tentar aceitar o pedido. 3. Rejeitar o pedido.
- **Resultado esperado:** no passo 1, mensagem de simulação indisponível; no passo 2, a aceitação é recusada e o pedido continua «Pendente»; no passo 3, o pedido passa a «Rejeitada».

#### ST-WEB-F004-11 — Aceitar pedido `[S]` `[FP]`

- **Rastreio:** RF-F004-10; UC-F004-04.
- **Precondições:** `V-FUT` com pedido pendente de `PA`; com sessão de `CD`.
- **Dados:** caso A, partida futura; caso B, partida recuada (AUX-06) com a viagem ainda Ativa.
- **Passos:** 1. Abrir os pedidos da viagem. 2. Aceitar o pedido. 3. Abrir `/viagens`.
- **Resultado esperado:** o pedido sai dos pendentes; no passo 3, «2 de 3 lugares disponíveis»; a participação fica «Aceite» nos pedidos de `PA`.

#### ST-WEB-F004-12 — Aceitar sem lugares disponíveis `[E]`

- **Rastreio:** RF-F004-10; UC-F004-04; RNF-USA-02.
- **Precondições:** `V-UM` com pedidos pendentes de `PA` e `PB`; com sessão de `CD`, na lista; `PA` é aceite pela API depois de a página abrir.
- **Dados:** nenhum.
- **Passos:** 1. Aceitar o pedido de `PB`.
- **Resultado esperado:** alerta com o motivo; o pedido de `PB` continua «Pendente» e pode ser rejeitado.

#### ST-WEB-F004-13 — Rejeitar pedido `[S]` `[FP]`

- **Rastreio:** RF-F004-12; UC-F004-05.
- **Precondições:** `V-FUT` com pedido pendente de `PA`; com sessão de `CD`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os pedidos da viagem. 2. Rejeitar o pedido. 3. Abrir `/viagens`.
- **Resultado esperado:** o pedido fica «Rejeitada»; no passo 3, «3 de 3 lugares disponíveis».

#### ST-WEB-F004-14 — Consultar os próprios pedidos `[S]`

- **Rastreio:** RF-F004-14; UC-F004-06.
- **Precondições:** `PA` com um pedido «Pendente», um «Aceite», um «Rejeitada» e um «Cancelada»; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os meus pedidos.
- **Resultado esperado:** a lista tem os 4 pedidos, cada um com o estado indicado.

#### ST-WEB-F004-15 — Cancelar pedido pendente `[S]`

- **Rastreio:** RF-F004-15; UC-F004-07.
- **Precondições:** pedido pendente de `PA`; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os meus pedidos e pedir o cancelamento. 2. Não confirmar. 3. Pedir de novo e confirmar.
- **Resultado esperado:** depois do passo 2, continua «Pendente»; depois do passo 3, «Cancelada».

#### ST-WEB-F004-16 — Cancelar participação aceite antes da partida `[S]`

- **Rastreio:** RF-F004-16; UC-F004-08.
- **Precondições:** `V-FUT` com `PA` aceite; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Cancelar a participação e confirmar. 2. Com sessão de `CD`, abrir `/viagens`.
- **Resultado esperado:** a participação fica «Cancelada»; no passo 2, «3 de 3 lugares disponíveis».

#### ST-WEB-F004-17 — Cancelar participação aceite depois da partida `[E]`

- **Rastreio:** RF-F004-16; UC-F004-08; RNF-USA-02.
- **Precondições:** `V-FUT` com `PA` aceite; AUX-06 aplicado depois de a página dos pedidos abrir; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Cancelar a participação e confirmar.
- **Resultado esperado:** alerta a indicar que o cancelamento só é possível até à partida; a participação continua «Aceite».

#### ST-WEB-F004-18 — Pedidos de viagem alheia `[E]`

- **Rastreio:** RF-F004-08; UC-F004-02.
- **Precondições:** `V-FUT` com pedido de `PA`; com sessão de `T3`.
- **Dados:** identificador de `V-FUT`.
- **Passos:** 1. Abrir o endereço direto dos pedidos da viagem.
- **Resultado esperado:** acesso recusado, com mensagem; o texto da página não contém o nome de `PA`.

### 5.5. F005 — Comunicação

#### ST-WEB-F005-01 — Enviar mensagem individual `[S]` `[FP]`

- **Rastreio:** RF-F005-01; UC-F005-01.
- **Precondições:** com sessão de `PA`; conta `CD`.
- **Dados:** texto com 1000 caracteres.
- **Passos:** 1. Abrir a conversa com `CD`. 2. Escrever e enviar. 3. Com sessão de `CD` (AUX-11), abrir a conversa com `PA`.
- **Resultado esperado:** nos passos 2 e 3, a mensagem aparece completa.

#### ST-WEB-F005-02 — Mensagem vazia ou demasiado longa `[E]`

- **Rastreio:** RF-F005-01; UC-F005-01; RNF-USA-02.
- **Precondições:** com sessão de `PA`, na conversa com `CD`.
- **Dados:** caso A, vazio; caso B, só espaços; caso C, 1001 caracteres.
- **Passos:** 1. Escrever e enviar.
- **Resultado esperado:** mensagem a indicar o limite de 1 a 1000 caracteres; nada é acrescentado à conversa.

#### ST-WEB-F005-03 — Destinatário inválido `[E]`

- **Rastreio:** RF-F005-01; UC-F005-01; RNF-USA-02.
- **Precondições:** conta `NC`; com sessão de `PA`.
- **Dados:** caso A, identificador de `NC`; caso B, UUID sem conta; caso C, identificador de `PA`.
- **Passos:** 1. Abrir a conversa pelo endereço direto. 2. Enviar uma mensagem válida, se for oferecido.
- **Resultado esperado:** casos A e B: alerta «O destinatário não existe ou não tem a conta confirmada.» ou envio não oferecido; caso C: alerta «Não é possível enviar mensagens para si próprio.» ou envio não oferecido; em nenhum caso a mensagem fica registada.

#### ST-WEB-F005-04 — Lista de conversas `[S]`

- **Rastreio:** RF-F005-08; UC-F005-02.
- **Precondições:** `PA` trocou mensagens com `CD` e com `PB`; `PB` e `T3` trocaram mensagens entre si; `T3` não tem conversas com `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Com sessão de `PA`, abrir as conversas. 2. Com uma conta nova, abrir as conversas.
- **Resultado esperado:** no passo 1, exatamente 2 entradas, `CD` e `PB`; no passo 2, a indicação de que ainda não há mensagens.

#### ST-WEB-F005-05 — Ordem das mensagens `[S]`

- **Rastreio:** RF-F005-09; UC-F005-03.
- **Precondições:** conversa entre `PA` e `CD` com as mensagens M1, M2 e M3, por esta ordem de envio; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir a conversa.
- **Resultado esperado:** as mensagens aparecem na ordem M1, M2, M3.

#### ST-WEB-F005-06 — Enviar aviso aos passageiros `[S]`

- **Rastreio:** RF-F005-05; RF-F005-06; UC-F005-04; UC-F005-05.
- **Precondições:** `V-FUT` com `PA` e `PB` aceites; com sessão de `CD`.
- **Dados:** texto com 100 caracteres.
- **Passos:** 1. Enviar o aviso na viagem. 2. Com sessão de `PA`, abrir os avisos da viagem. 3. Repetir com sessão de `PB`.
- **Resultado esperado:** nos passos 2 e 3, o aviso aparece completo.

#### ST-WEB-F005-07 — Aviso inválido `[E]`

- **Rastreio:** RF-F005-05; UC-F005-04; RNF-USA-02.
- **Precondições:** `V-FUT` com `PA` aceite; com sessão de `CD`.
- **Dados:** caso A, vazio; caso B, 101 caracteres.
- **Passos:** 1. Escrever e enviar o aviso.
- **Resultado esperado:** mensagem a indicar o limite de 100 caracteres (P-28); `PA` não recebe aviso.

#### ST-WEB-F005-08 — Aviso em viagem não Ativa `[E]`

- **Rastreio:** RF-F005-05; UC-F005-04.
- **Precondições:** com sessão de `CD`, na viagem `V-FUT`; viagem indisponibilizada pela API depois de a página abrir.
- **Dados:** aviso válido.
- **Passos:** 1. Enviar o aviso.
- **Resultado esperado:** alerta a indicar que só se enviam avisos em viagens Ativas; o aviso não fica registado.

#### ST-WEB-F005-09 — Avisos ocultos a quem não participa `[E]`

- **Rastreio:** RF-F005-06; UC-F005-05.
- **Precondições:** `V-FUT` com um aviso; `PA` aceite; `PB` com pedido pendente; sem sessão aberta.
- **Dados:** identificador de `V-FUT`.
- **Passos:** 1. Com sessão de `PB`, abrir os detalhes e os avisos da viagem. 2. Repetir com sessão de `T3`.
- **Resultado esperado:** nos dois passos, o texto do aviso não aparece.

### 5.6. F006 — Notificações

#### ST-WEB-F006-01 — Consultar notificações `[S]` `[FP]`

- **Rastreio:** RF-F006-07; UC-F006-01.
- **Precondições:** `PA` tem uma viagem própria, com um pedido de `PB` (PedidoRecebido), e recebeu pedido aceite, pedido rejeitado, alteração da viagem, aviso e nova mensagem, por esta ordem; `T3` recebeu uma nova mensagem de `CD`; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir as notificações.
- **Resultado esperado:** a lista tem as 6 notificações, uma de cada tipo, da mais recente para a mais antiga; a notificação de `T3` não aparece.

#### ST-WEB-F006-02 — Atualização do passageiro em 5 segundos `[S]`

- **Rastreio:** RF-F006-09; UC-F006-03; UC-F004-06.
- **Precondições:** duas viagens `V-FUT`, cada uma com um pedido pendente de `PA`; `PA` com a lista de pedidos aberta, em sessão nova.
- **Dados:** nenhum.
- **Passos:** 1. O condutor aceita um pedido pela API e regista o instante. 2. Rejeita o outro e regista o instante.
- **Resultado esperado:** sem recarregar, cada novo estado aparece em 5 segundos ou menos depois da decisão (P-05).

#### ST-WEB-F006-03 — Atualização do condutor em 5 segundos `[S]`

- **Rastreio:** RF-F006-15; UC-F006-03; UC-F004-02.
- **Precondições:** `V-FUT`; `CD` com os pedidos da viagem abertos, em sessão nova.
- **Dados:** pedido válido de `PA`.
- **Passos:** 1. Submeter o pedido pela API e registar o instante.
- **Resultado esperado:** sem recarregar, o pedido aparece em 5 segundos ou menos depois da submissão (P-05).

#### ST-WEB-F006-04 — Marcar notificações como lidas `[S]`

- **Rastreio:** RF-F006-19; UC-F006-01.
- **Precondições:** `PA` com 3 notificações por ler; `PB` com 1 por ler; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Marcar uma notificação como lida. 2. Recarregar. 3. Marcar todas como lidas. 4. Recarregar. 5. Com sessão de `PB`, abrir as notificações.
- **Resultado esperado:** depois do passo 2, 1 lida e 2 por ler; depois do passo 4, todas lidas; no passo 5, a notificação de `PB` continua por ler.

#### ST-WEB-F006-05 — Falha do canal de tempo real `[E]`

- **Rastreio:** RF-F006-07; UC-F006-03; UC-F006-01.
- **Precondições:** `CD` com a aplicação aberta e `/hubs/pedidos` bloqueado (CDP, só Chrome).
- **Dados:** pedido válido de `PA` numa viagem de `CD`.
- **Passos:** 1. Submeter o pedido pela API. 2. Abrir as notificações.
- **Resultado esperado:** a aplicação não apresenta erro bloqueante; no passo 2, a notificação do pedido aparece.

### 5.7. F007 — Avaliações

#### ST-WEB-F007-01 — Avaliar participante `[S]` `[FP]`

- **Rastreio:** RF-F007-01; RF-F007-10; UC-F007-01; UC-F001-10.
- **Precondições:** `V-CONC`; `CD` sem avaliações; com sessão de `PA`.
- **Dados:** classificação 4.
- **Passos:** 1. Abrir a avaliação de `CD` nessa viagem. 2. Escolher 4 estrelas e submeter. 3. Abrir o perfil de `CD`.
- **Resultado esperado:** confirmação da avaliação; no passo 3, «4,0 / 5».

#### ST-WEB-F007-02 — Classificação inválida `[E]`

- **Rastreio:** RF-F007-01; UC-F007-01; RNF-USA-02.
- **Precondições:** `V-CONC`; com sessão de `PA`, na avaliação de `CD`.
- **Dados:** 0, 6 e 2,5, se a interface permitir introduzir valores.
- **Passos:** 1. Tentar submeter cada valor.
- **Resultado esperado:** só existem as opções de 1 a 5; se os outros valores forem introduzíveis, alerta «A classificação deve ser um número inteiro entre 1 e 5.»; nenhuma avaliação fica registada.

#### ST-WEB-F007-03 — Avaliar antes de a viagem estar concluída `[E]`

- **Rastreio:** RF-F007-01; UC-F007-01.
- **Precondições:** `V-FUT` com `PA` aceite; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os detalhes da viagem.
- **Resultado esperado:** a ação de avaliar não é oferecida ou, ao ser usada, apresenta «Só é possível avaliar viagens concluídas.»; nenhuma avaliação fica registada.

#### ST-WEB-F007-04 — Avaliar sem ter participado `[E]`

- **Rastreio:** RF-F007-01; UC-F007-01.
- **Precondições:** `V-CONC`; com sessão de `T3`.
- **Dados:** identificador de `CD`.
- **Passos:** 1. Tentar abrir a avaliação de `CD` nessa viagem pelo endereço direto.
- **Resultado esperado:** a avaliação é recusada, com mensagem; a classificação de `CD` não muda.

#### ST-WEB-F007-05 — Avaliação repetida `[E]`

- **Rastreio:** RF-F007-01; UC-F007-01.
- **Precondições:** `V-CONC`; com sessão de `PA`, com a avaliação de `CD` aberta; entretanto, `PA` avalia `CD` pela API.
- **Dados:** classificação 2.
- **Passos:** 1. Submeter a avaliação na página aberta.
- **Resultado esperado:** alerta «Já existe uma avaliação deste utilizador. Pode alterar a avaliação existente.» e acesso à alteração.

#### ST-WEB-F007-06 — Alterar avaliação `[S]`

- **Rastreio:** RF-F007-05; RF-F007-10; UC-F007-02; UC-F001-10.
- **Precondições:** `V-CONC`; `PA` avaliou `CD` com 3, sendo a única avaliação; com sessão de `PA`.
- **Dados:** classificação 5.
- **Passos:** 1. Abrir a alteração da avaliação. 2. Escolher 5 e submeter. 3. Abrir o perfil de `CD`.
- **Resultado esperado:** no passo 3, «5,0 / 5»; a lista de avaliações de `CD` tem 1 entrada. Ver DEF-BE-041: o formulário pode não mostrar o valor anterior.

#### ST-WEB-F007-07 — Avaliações recebidas são anónimas `[S]`

- **Rastreio:** RF-F007-07; RF-F007-11; UC-F007-03.
- **Precondições:** `PA` e `PB` avaliaram `CD`; com sessão de `T3`.
- **Dados:** identificador de `CD`.
- **Passos:** 1. Abrir as avaliações recebidas por `CD`.
- **Resultado esperado:** 2 entradas com valor e data; o texto da página não contém os nomes de `PA` e `PB`.

#### ST-WEB-F007-08 — Média com uma casa decimal `[S]`

- **Rastreio:** RF-F007-10; UC-F007-03.
- **Precondições:** `CD` com classificações 4 e 5; `T3` sem avaliações; com sessão de `PA`.
- **Dados:** identificadores de `CD` e de `T3`.
- **Passos:** 1. Abrir o perfil de `CD`. 2. Abrir o perfil de `T3`.
- **Resultado esperado:** no passo 1, «4,5 / 5»; no passo 2, «Sem avaliações».

### 5.8. F008 — Mapas, rotas e estimativa de custo

#### ST-WEB-F008-01 — Rota da viagem num mapa `[S]`

- **Rastreio:** RF-F008-07; RF-F003-06; UC-F008-01; UC-F003-02.
- **Precondições:** `V-FUT` criada; com sessão de `T3`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os detalhes da viagem. 2. Abrir o mapa.
- **Resultado esperado:** o elemento do mapa fica visível, com marcadores na origem e no destino. **Condicionado a DEF-BE-054:** o traçado da rota só é verificável depois da decisão a tomar na I072; até lá, o resultado regista essa limitação.

#### ST-WEB-F008-02 — Distância e duração `[S]`

- **Rastreio:** RF-F008-08; UC-F008-01.
- **Precondições:** `V-FUT` criada; distância e duração obtidas por `GET /rotas/viagens/{id}`; com sessão de `T3`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os detalhes da viagem.
- **Resultado esperado:** a distância e a duração apresentadas coincidem com as devolvidas pelo backend.

#### ST-WEB-F008-03 — Pontos visíveis conforme o acesso `[S]`

- **Rastreio:** RF-F008-09; UC-F008-01.
- **Precondições:** `V-FUT` com `PA` aceite, com pontos próprios.
- **Dados:** nenhum.
- **Passos:** 1. Com sessão de `CD`, abrir o mapa. 2. Repetir com sessão de `PA`. 3. Repetir com sessão de `T3`.
- **Resultado esperado:** nos passos 1 e 2 aparecem os pontos de `PA`; no passo 3, apenas a origem e o destino.

#### ST-WEB-F008-04 — Estimativa de custo `[S]`

- **Rastreio:** RF-F008-11; UC-F008-02.
- **Precondições:** `V-FUT` com `PA` aceite; distância D conhecida; com sessão de `PA`.
- **Dados:** preço por km configurado em 0,10 €.
- **Passos:** 1. Abrir a estimativa de custo.
- **Resultado esperado:** apresenta D × 0,10 ÷ 2 €, arredondado como na API, com a indicação «valor indicativo»; não existe nenhuma ação de pagamento.

#### ST-WEB-F008-05 — Serviço de mapas indisponível na rota e no custo `[E]`

- **Rastreio:** RF-F008-12; UC-F008-01; UC-F008-02.
- **Precondições:** `V-FUT` com `PA` aceite; `PA` cancelou a participação com AUX-08 ativo, de modo que a rota ficou por recalcular; AUX-08 continua ativo; com sessão de `CD`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir os detalhes da viagem e o mapa. 2. Abrir a estimativa de custo.
- **Resultado esperado:** nos dois passos, mensagem a indicar que a informação não está disponível; os restantes detalhes da viagem continuam visíveis; a aplicação não falha.

### 5.9. F009 — Histórico

#### ST-WEB-F009-01 — Histórico do condutor `[S]` `[FP]`

- **Rastreio:** RF-F009-02; UC-F009-01.
- **Precondições:** `CD` com uma `V-FUT`, uma `V-CONC` e uma `V-IND`; com sessão de `CD`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir o histórico de viagens como condutor.
- **Resultado esperado:** as 3 viagens aparecem, com os estados «Ativa», «Concluída» e «Indisponibilizada».

#### ST-WEB-F009-02 — Histórico do passageiro `[S]`

- **Rastreio:** RF-F009-03; UC-F009-01.
- **Precondições:** `PA` aceite numa viagem Ativa, numa Concluída, numa participação depois cancelada por si e numa viagem depois indisponibilizada; pedido rejeitado noutra viagem; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir o histórico de viagens como passageiro.
- **Resultado esperado:** 4 entradas, cada uma com o estado da viagem e o da participação; a viagem do pedido rejeitado não aparece.

#### ST-WEB-F009-03 — Histórico de pedidos `[S]`

- **Rastreio:** RF-F009-04; UC-F009-02.
- **Precondições:** `PA` fez um pedido «Pendente» e um «Rejeitada» e recebeu um pedido «Aceite» numa viagem própria; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir o histórico de pedidos.
- **Resultado esperado:** os 3 pedidos aparecem, identificados como efetuados ou recebidos, cada um com o estado atual.

#### ST-WEB-F009-04 — Notificações e avisos no histórico `[S]`

- **Rastreio:** RF-F009-05; UC-F006-01; UC-F005-05.
- **Precondições:** `PA` recebeu os seis tipos de notificação; `V-FUT` com um aviso e `PA` aceite; com sessão de `PA`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir o histórico. 2. Cancelar a participação em `V-FUT`. 3. Abrir de novo o histórico.
- **Resultado esperado:** no passo 1, as notificações dos seis tipos e o aviso; no passo 3, o aviso deixa de aparecer.

#### ST-WEB-F009-05 — Histórico vazio e falha ao carregar `[E]`

- **Rastreio:** RF-F009-02; RF-F009-04; UC-F009-01; UC-F009-02; RNF-USA-02.
- **Precondições:** com sessão de uma conta nova.
- **Dados:** nenhum.
- **Passos:** 1. Abrir o histórico. 2. Aplicar AUX-10 e recarregar os dados.
- **Resultado esperado:** no passo 1, a indicação de que ainda não há viagens nem pedidos; no passo 2, alerta «Não foi possível contactar o servidor. Verifica a ligação e tenta novamente.» e opção de tentar novamente.

### 5.10. Cenários transversais

#### ST-WEB-TR-01 — Sem deslocamento horizontal `[S]`

- **Rastreio:** RNF-USA-03.
- **Precondições:** todas as rotas implementadas têm dados; `T3` com sessão nas rotas autenticadas.
- **Dados:** larguras de 360, 768, 1280 e 1920 px.
- **Passos:** 1. Para cada largura, abrir cada rota.
- **Resultado esperado:** em todas, `document.documentElement.scrollWidth` é menor ou igual a `clientWidth`.

#### ST-WEB-TR-02 — Textos em português europeu `[S]`

- **Rastreio:** RNF-USA-01.
- **Precondições:** cada rota implementada, com um estado de erro provocado.
- **Dados:** nenhum.
- **Passos:** 1. Percorrer as rotas e guardar o texto visível. 2. Procurar textos genéricos em inglês (`Error`, `Not Found`, `Unauthorized`, `Submit`, `Loading`).
- **Resultado esperado:** nenhum dos textos procurados aparece, excluindo conteúdo introduzido pelos utilizadores.

#### ST-WEB-TR-03 — Conteúdo dos utilizadores apresentado como texto `[E]`

- **Rastreio:** RNF-SEG-12.
- **Precondições:** conta `CD`; com sessão de `T3`.
- **Dados:** `<img src=x onerror="window.__xss=1">` no nome de `CD`, na origem de uma viagem e numa mensagem.
- **Passos:** 1. Abrir o perfil de `CD`. 2. Com sessão de `CD`, abrir `/viagens`. 3. Abrir a conversa e a pesquisa.
- **Resultado esperado:** o texto aparece literalmente; `window.__xss` continua `undefined`; não é criado nenhum elemento `img` a partir do conteúdo.

#### ST-WEB-TR-04 — Datas em hora de Portugal `[S]`

- **Rastreio:** RNF-FIA-04; RF-F002-15.
- **Precondições:** viagem com partida às 10:00 UTC de um dia em horário de inverno, e outra de um dia em horário de verão (AUX-05); browser com fuso `America/New_York` (CDP, só Chrome); com sessão de `CD`.
- **Dados:** nenhum.
- **Passos:** 1. Abrir `/viagens`.
- **Resultado esperado:** a primeira viagem apresenta 10:00 e a segunda 11:00.

#### ST-WEB-TR-05 — Servidor inacessível `[E]`

- **Rastreio:** RNF-USA-02; RF-F001-10; UC-F001-04.
- **Precondições:** conta `CD`; sem sessão; AUX-10 ativo depois de a página abrir.
- **Dados:** credenciais de `CD`.
- **Passos:** 1. Abrir `/entrar`. 2. Preencher e premir «Entrar».
- **Resultado esperado:** alerta «Não foi possível contactar o servidor. Verifica a ligação e tenta novamente.»; o formulário mantém-se.

## 6. Rastreabilidade

### 6.1. Requisitos funcionais

| Requisito | Cenários |
| --- | --- |
| RF-F001-01 | ST-WEB-F001-01 |
| RF-F001-02 | ST-WEB-F001-02, ST-WEB-F001-03, ST-WEB-F001-04, ST-WEB-F001-05, ST-WEB-F001-06 |
| RF-F001-07 | ST-WEB-F001-07, ST-WEB-F001-11 |
| RF-F001-08 | ST-WEB-F001-08, ST-WEB-F001-09, ST-WEB-F001-10 |
| RF-F001-10 | ST-WEB-F001-14, ST-WEB-F001-15, ST-WEB-F001-16, ST-WEB-F001-17, ST-WEB-F001-19, ST-WEB-TR-05 |
| RF-F001-12 | ST-WEB-F001-18, ST-WEB-F001-20 |
| RF-F001-14 | ST-WEB-F001-22, ST-WEB-F001-23 |
| RF-F001-15 | ST-WEB-F001-24, ST-WEB-F001-25, ST-WEB-F001-26, ST-WEB-F001-27 |
| RF-F001-17 | ST-WEB-F001-29 |
| RF-F001-19 | ST-WEB-F001-29, ST-WEB-F001-30 |
| RF-F001-20 | ST-WEB-F001-21 |
| RF-F001-21 | ST-WEB-F001-11, ST-WEB-F001-12, ST-WEB-F001-13 |
| RF-F001-22 | ST-WEB-F001-28 |
| RF-F002-01 | ST-WEB-F002-01, ST-WEB-F002-02 |
| RF-F002-07 | ST-WEB-F002-06, ST-WEB-F002-08, ST-WEB-F002-09, ST-WEB-F002-11, ST-WEB-F002-12 |
| RF-F002-09 | ST-WEB-F002-10, ST-WEB-F002-11, ST-WEB-F003-01 |
| RF-F002-15 | ST-WEB-F002-04, ST-WEB-F002-05, ST-WEB-TR-04 |
| RF-F002-17 | ST-WEB-F002-03, ST-WEB-F002-07 |
| RF-F003-01 | ST-WEB-F003-01, ST-WEB-F003-02, ST-WEB-F003-03, ST-WEB-F003-04 |
| RF-F003-03 | ST-WEB-F003-01, ST-WEB-F003-05 |
| RF-F003-04 | ST-WEB-F003-01, ST-WEB-F003-05 |
| RF-F003-06 | ST-WEB-F003-05, ST-WEB-F003-06, ST-WEB-F008-01 |
| RF-F003-07 | ST-WEB-F003-07, ST-WEB-F003-08 |
| RF-F003-08 | ST-WEB-F003-08 |
| RF-F004-01 | ST-WEB-F004-01, ST-WEB-F004-04, ST-WEB-F004-05, ST-WEB-F004-07 |
| RF-F004-02 | ST-WEB-F004-01, ST-WEB-F004-02, ST-WEB-F004-03 |
| RF-F004-08 | ST-WEB-F004-08, ST-WEB-F004-18 |
| RF-F004-09 | ST-WEB-F004-09, ST-WEB-F004-10 |
| RF-F004-10 | ST-WEB-F004-11, ST-WEB-F004-12 |
| RF-F004-12 | ST-WEB-F004-13 |
| RF-F004-14 | ST-WEB-F004-14 |
| RF-F004-15 | ST-WEB-F004-15 |
| RF-F004-16 | ST-WEB-F004-16, ST-WEB-F004-17 |
| RF-F004-20 | ST-WEB-F004-01, ST-WEB-F004-02, ST-WEB-F004-06 |
| RF-F004-21 | ST-WEB-F004-01, ST-WEB-F004-02, ST-WEB-F004-06 |
| RF-F005-01 | ST-WEB-F005-01, ST-WEB-F005-02, ST-WEB-F005-03 |
| RF-F005-05 | ST-WEB-F005-06, ST-WEB-F005-07, ST-WEB-F005-08 |
| RF-F005-06 | ST-WEB-F005-06, ST-WEB-F005-09 |
| RF-F005-08 | ST-WEB-F005-04 |
| RF-F005-09 | ST-WEB-F005-05 |
| RF-F006-07 | ST-WEB-F006-01, ST-WEB-F006-05 |
| RF-F006-09 | ST-WEB-F006-02 |
| RF-F006-15 | ST-WEB-F006-03 |
| RF-F006-19 | ST-WEB-F006-04 |
| RF-F007-01 | ST-WEB-F007-01, ST-WEB-F007-02, ST-WEB-F007-03, ST-WEB-F007-04, ST-WEB-F007-05 |
| RF-F007-05 | ST-WEB-F007-06 |
| RF-F007-07 | ST-WEB-F007-07 |
| RF-F007-10 | ST-WEB-F001-21, ST-WEB-F001-29, ST-WEB-F007-01, ST-WEB-F007-06, ST-WEB-F007-08 |
| RF-F007-11 | ST-WEB-F007-07 |
| RF-F008-07 | ST-WEB-F008-01 |
| RF-F008-08 | ST-WEB-F008-02 |
| RF-F008-09 | ST-WEB-F003-07, ST-WEB-F003-08, ST-WEB-F008-03 |
| RF-F008-11 | ST-WEB-F008-04 |
| RF-F008-12 | ST-WEB-F008-05 |
| RF-F009-02 | ST-WEB-F009-01, ST-WEB-F009-05 |
| RF-F009-03 | ST-WEB-F009-02 |
| RF-F009-04 | ST-WEB-F009-03, ST-WEB-F009-05 |
| RF-F009-05 | ST-WEB-F009-04 |

### 6.2. Requisitos não funcionais

| Requisito | Cenários |
| --- | --- |
| RNF-FIA-04 | ST-WEB-TR-04 |
| RNF-SEG-12 | ST-WEB-TR-03 |
| RNF-USA-01 | ST-WEB-TR-02 |
| RNF-USA-02 | ST-WEB-F001-03, ST-WEB-F001-04, ST-WEB-F001-13, ST-WEB-F001-15, ST-WEB-F001-17, ST-WEB-F001-23, ST-WEB-F001-25, ST-WEB-F002-02, ST-WEB-F002-09, ST-WEB-F004-03, ST-WEB-F004-04, ST-WEB-F004-06, ST-WEB-F004-07, ST-WEB-F004-12, ST-WEB-F004-17, ST-WEB-F005-02, ST-WEB-F005-03, ST-WEB-F005-07, ST-WEB-F007-02, ST-WEB-F009-05, ST-WEB-TR-05 |
| RNF-USA-03 | ST-WEB-TR-01 |

RNF-COM-01 verifica-se repetindo todos os cenários `[FP]` nos quatro browsers (secção 4.1).

## 7. Observações para as issues seguintes

1. **RNF-FIA-04.** A I071 apresenta e interpreta as datas na hora de Portugal, seja qual for o fuso do browser (`partida.ts`). ST-WEB-TR-04 verifica a apresentação; as I072 a I076 devem reutilizar estas funções para qualquer data nova.
2. **`GET /rotas/viagens/{id}` e geometria (DEF-BE-054).** A I072 tem de decidir o que o mapa desenha; ST-WEB-F008-01 é atualizado depois dessa decisão.
3. **Identificadores estáveis.** As I072 a I076 devem expor `id` ou `data-testid` nos elementos dos cenários de F003 a F009.
4. **Operações auxiliares.** AUX-03, AUX-04, AUX-06, AUX-07, AUX-08 e AUX-09 precisam de uma API de teste que permita alterar dados e simular falhas. A `AplicacaoReal` dos testes do backend já oferece `NaBaseDeDados` e a falha simulada dos mapas, e a I087 pode partir dela em E1. Em E2, a API corre em contentor e a solução tem de ser definida na I087 e integrada na I095.
5. **Safari.** Os cenários que dependem do CDP correm só em Chrome e não pertencem ao conjunto `[FP]`, pelo que não afetam RNF-COM-01.

## 8. Fluxos principais

| Fluxo principal (glossário) | Cenários `[FP]` |
| --- | --- |
| Registo e confirmação de conta | ST-WEB-F001-02, ST-WEB-F001-07 |
| Início de sessão | ST-WEB-F001-14 |
| Criar viagem | ST-WEB-F002-01 |
| Pesquisar viagens | ST-WEB-F003-01 |
| Pedir participação | ST-WEB-F004-01 |
| Aceitar ou rejeitar um pedido | ST-WEB-F004-11, ST-WEB-F004-13 |
| Enviar mensagem | ST-WEB-F005-01 |
| Consultar notificações | ST-WEB-F006-01 |
| Avaliar | ST-WEB-F007-01 |
| Consultar o histórico | ST-WEB-F009-01 |

## 9. Documentos relacionados

Regulamento da UC (secções 8, 14, 15 e 19); Proposta de Sistema v03; Declaração de Âmbito v03; Regulamento Interno do Grupo; `m4-plano-testes-frontend-web-v01.md`; `m3-especificacao-requisitos-v02.md`; `m3-especificacoes-casos-uso-v02.md`; `m3-documentacao-api-v02.md`; `m3-requirements-traceability-matrix-v02`; `m4-backlog-projeto-v08`.
