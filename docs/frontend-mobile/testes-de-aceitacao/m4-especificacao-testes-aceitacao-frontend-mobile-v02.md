# Especificação de testes de aceitação — frontend mobile

| Campo | Informação |
|---|---|
| Projeto | G15 — Campus Carpool |
| Milestone / sprint | m4 / S08 |
| Issue | I093 |
| Estado | Revista — especificação de aceitação alinhada com a ER v02 e RTM v02 |
| Versão documental | v02 — revisão da I093, 18/11/2026 |
| Versão de implementação analisada | Frontend mobile m4 — implementação I078–I084 |
| Fontes de rastreabilidade | ER v02 (versão interna v05); RTM v02 (m3); Checklist de Aprovação dos Requisitos v02; Plano de Testes Mobile I090 v01 |
| Referência à implementação das notificações | I083 — frontend mobile (GitHub) |

## 1. Objetivo e âmbito

Definir verificações observáveis pelo utilizador para aceitar as funcionalidades entregues do frontend mobile. Abrange F001–F009 e os requisitos transversais do plano I090. As funcionalidades F001–F009 integram a implementação mobile de m4. Os resultados da execução dos cenários são documentados na I100.

## 2. Preparação da sessão

Usar contas de teste confirmadas e distintas de condutor e passageiros, sem dados pessoais reais. Preparar viagens futuras com lugares livres, viagem cheia, viagem concluída, pedidos pendentes/aceites/rejeitados/cancelados e situações de falha controlada de rede/mapas. A preparação dos dados decorre pelo mecanismo autorizado do ambiente de teste. Usar os dados específicos indicados em cada cenário; confirmar o estado inicial antes de cada execução.

Consultar o plano de testes e a especificação de sistema do mesmo frontend para a configuração do ambiente e dados A/B/C referidos nos cenários. A execução usa a versão integrada identificada no respetivo relatório I100, incluindo referência do commit ou artefacto APK. Não usar contas ou serviços de produção para simular falhas.

## 3. Método e critério de aceitação

Os passos abaixo são executados manualmente na interface por um utilizador representativo, com acompanhamento do responsável. Os cenários foram derivados da especificação de sistema existente, preservando os identificadores RF/RNF, pré-condições, dados e resultados esperados. Os identificadores ATM distinguem esta aceitação dos testes de sistema TSM.

Cada cenário só passa se todos os resultados esperados forem observados. Registar Passou, Falhou ou Bloqueado e o motivo; associar defeitos a resultados que falham. Confirmar também se o utilizador compreendeu as mensagens e conseguiu concluir a tarefa sem instruções adicionais não previstas.

## 4. Registo a preencher na execução

Cenário; ambiente e versão; condição inicial; resultado observado; estado; evidência; defeito/observação; responsável. A execução e o relatório de resultados pertencem à I100. A I100 reúne as evidências e os resultados de execução.

## 5. Casos funcionais de teste

### F001 — Registo, confirmação, sessão e perfis

#### ATM-001 — Registo académico válido

- **Funcionalidade / requisitos:** F001; RF-F001-02, RF-F001-05, RF-F001-06.
- **Precondições:** API de teste E1/E2 com serviço de email substituído; email ainda não registado.
- **Dados de teste:** nome válido; conta `condutor+<id>@estg.ipp.pt`; palavra-passe com ≥8 caracteres, letra e dígito.
- **Passos:** Abrir Registo; introduzir os três campos; submeter; consultar estado da conta na API de teste.
- **Resultado esperado (oráculo):** A interface apresenta confirmação da submissão; a conta é criada por confirmar; o email fictício/código fica acessível no coletor de testes; não se entra automaticamente nas viagens.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-002 — Rejeitar domínio de email não académico

- **Funcionalidade / requisitos:** F001; RF-F001-03; RF-F001-02.
- **Precondições:** Ecrã de registo e API disponíveis.
- **Dados de teste:** `teste+<id>@example.com` com restantes campos válidos.
- **Passos:** Preencher; submeter; consultar contagem de contas antes/depois.
- **Resultado esperado (oráculo):** Mensagem explícita sobre domínio não permitido; não cria conta nem autentica.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-003 — Regras da palavra-passe e dados obrigatórios

- **Funcionalidade / requisitos:** F001; RF-F001-05; RF-F001-02.
- **Precondições:** Registo aberto; sem conta prévia.
- **Dados de teste:** Palavra-passe com 7 caracteres, sem algarismo, só algarismos; nome/email vazios.
- **Passos:** Submeter cada variante isoladamente; corrigir campos; repetir com dados válidos.
- **Resultado esperado (oráculo):** Cada variante inválida é recusada com erro acionável; ao corrigir todos os campos o registo é permitido.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-004 — Email já registado

- **Funcionalidade / requisitos:** F001; RF-F001-04.
- **Precondições:** Conta de teste previamente criada.
- **Dados de teste:** Mesmo email da conta anterior, restantes dados válidos.
- **Passos:** Tentar novo registo; consultar estado da primeira conta.
- **Resultado esperado (oráculo):** O duplicado não cria outra conta; a aplicação mostra erro adequado sem alterar os dados existentes.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-005 — Acesso negado a conta por confirmar

- **Funcionalidade / requisitos:** F001; RF-F001-07, RF-F001-10; P-09.
- **Precondições:** Conta criada, mas `EmailConfirmado=false`.
- **Dados de teste:** Credenciais corretas da conta pendente.
- **Passos:** Abrir Login; introduzir credenciais; tentar entrar e aceder às viagens.
- **Resultado esperado (oráculo):** O backend recusa a autenticação; a aplicação explica que falta confirmação; nenhuma área de viagens fica acessível.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-006 — Reenvio da confirmação e intervalo de proteção

- **Funcionalidade / requisitos:** F001; RF-F001-21.
- **Precondições:** Conta pendente e coletor de email de teste preparados.
- **Dados de teste:** Email da conta pendente; repetição dentro do intervalo definido pelo backend.
- **Passos:** Pedir reenvio; guardar novo token; repetir imediatamente; verificar notificação e estado.
- **Resultado esperado (oráculo):** O reenvio permitido produz token utilizável; pedido precoce tem tratamento controlado; a interface não revela indevidamente se um email arbitrário existe.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-007 — Confirmar conta com token válido

- **Funcionalidade / requisitos:** F001; RF-F001-07.
- **Precondições:** Conta não confirmada; token válido capturado em E1/E2.
- **Dados de teste:** `tokenConfirmacao` obtido do serviço fake, sem valor fixo em ficheiros.
- **Passos:** Abrir ligação `campuscarpool://confirmar-conta?token=...` pelo intent Android ou colar token no ecrã; confirmar; iniciar sessão.
- **Resultado esperado (oráculo):** O token é aceite uma vez, conta passa a confirmada e o login torna-se possível.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-008 — Token inválido, expirado ou reutilizado

- **Funcionalidade / requisitos:** F001; RF-F001-08.
- **Precondições:** Contas pendentes isoladas e tokens manipuláveis por AUX-04/AUX-05.
- **Dados de teste:** Token inexistente; token expirado; token consumido.
- **Passos:** Tentar confirmar com cada token; consultar estado da conta; repetir token previamente aceite.
- **Resultado esperado (oráculo):** Nenhuma confirmação indevida; mensagem diferencia, quando aplicável, token inválido, expirado e já usado; não abre sessão.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-009 — Início de sessão válido e credenciais erradas

- **Funcionalidade / requisitos:** F001; RF-F001-10, RF-F001-11.
- **Precondições:** Conta confirmada e utilizador sem sessão.
- **Dados de teste:** Password errada e correta para o mesmo email; email inexistente.
- **Passos:** Tentar password errada; tentar email inexistente; finalmente autenticar corretamente.
- **Resultado esperado (oráculo):** Erros de autenticação não expõem credenciais; credenciais corretas abrem área autenticada com dados da conta.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-010 — Persistência da sessão e logout

- **Funcionalidade / requisitos:** F001; RF-F001-12.
- **Precondições:** Conta confirmada; app instalada; token guardado por AndroidSessionStore.
- **Dados de teste:** Sessão A no dispositivo; endpoint autenticado de controlo.
- **Passos:** Iniciar sessão; fechar/abrir app; verificar persistência; terminar sessão; usar Voltar e repetir pedido com token revogado.
- **Resultado esperado (oráculo):** Sessão válida resiste ao reinício; logout limpa dados locais e a API rejeita token revogado; Voltar não reabre zona privada.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-011 — Consultar e editar perfil próprio e ligações

- **Funcionalidade / requisitos:** F001; RF-F001-14, RF-F001-15, RF-F001-16, RF-F001-20, RF-F001-22.
- **Precondições:** Utilizador confirmado e autenticado.
- **Dados de teste:** Nome/curso/instituição válidos; 5 URLs HTTPS; sexta URL; URL `javascript:...`.
- **Passos:** Alterar dados e guardar; adicionar cinco ligações; tentar sexta e URL inválida; remover uma ligação; reler perfil.
- **Resultado esperado (oráculo):** Dados persistem; limite de ligações respeitado; esquemas não HTTP(S) recusados; remoção refletida no perfil.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-012 — Perfil público de outro utilizador e privacidade

- **Funcionalidade / requisitos:** F001; RF-F001-17, RF-F001-19; RNF-PRI-01 (minimização), RF-F001-17/P-10 (campos públicos) e RNF-SEG-10 (autorização da consulta).
- **Precondições:** Contas A e B confirmadas; A autenticada.
- **Dados de teste:** Identificador de B e de terceiro inexistente.
- **Passos:** Abrir perfil público de B via navegação/deep link; inspecionar informação; tentar perfil inexistente.
- **Resultado esperado (oráculo):** São apresentados só campos públicos, nunca email nem dados privados; identificador inexistente dá estado de erro/sem resultado.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.


### F002 — Gestão das próprias viagens

#### ATM-013 — Lista de viagens própria vazia

- **Funcionalidade / requisitos:** F002; RF-F002-15.
- **Precondições:** Condutor confirmado, sem viagens ativas.
- **Dados de teste:** Fixture sem viagens do condutor.
- **Passos:** Entrar; abrir As minhas viagens; observar lista e opções.
- **Resultado esperado (oráculo):** Estado vazio compreensível e ação Criar viagem disponível; não aparecem viagens de outros utilizadores.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-014 — Criar viagem válida

- **Funcionalidade / requisitos:** F002; RF-F002-01, RF-F002-02, RF-F002-04; RF-F008-01, RF-F008-02.
- **Precondições:** Condutor confirmado; serviço de mapas simulado determinístico.
- **Dados de teste:** Origem Felgueiras, destino Porto, partida `agora+2 dias` em UTC; 3 lugares.
- **Passos:** Abrir Criar viagem; preencher; confirmar; reler na lista e detalhes.
- **Resultado esperado (oráculo):** Cria uma única viagem ativa; origem/destino, hora local, lugares e disponibilidade coincidem com a API; rota/calculado presentes quando mapas disponíveis.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-015 — Impedir criação com campos vazios ou locais iguais

- **Funcionalidade / requisitos:** F002; RF-F002-04; RF-F002-16.
- **Precondições:** Formulário aberto e serviço de mapas controlado.
- **Dados de teste:** Origem vazia; destino vazio; origem=destino; 201 caracteres em cada local.
- **Passos:** Testar cada variante e submeter; verificar API de teste/contagem de viagens.
- **Resultado esperado (oráculo):** Erro específico e nenhum registo novo; os campos válidos permanecem no formulário.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-016 — Recusar data passada e formato temporal inválido

- **Funcionalidade / requisitos:** F002; RF-F002-03, RF-F002-04; RNF-FIA-04.
- **Precondições:** Formulário de viagem aberto; relógio de teste conhecido.
- **Dados de teste:** Ontem; instante atual; data válida transmitida em UTC `Z` pela aplicação.
- **Passos:** Introduzir datas inválidas; confirmar recusa; criar com hora futura e inspecionar payload recebido.
- **Resultado esperado (oráculo):** Datas passadas não são aceites; data futura enviada com UTC `Z`, sem alterações de instante por fuso do emulador.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-017 — Validar limites de lugares

- **Funcionalidade / requisitos:** F002; RF-F002-04, RF-F002-17.
- **Precondições:** Condutor confirmado; criação de viagem.
- **Dados de teste:** 0, 1, 8 e 9 lugares.
- **Passos:** Tentar guardar com cada valor.
- **Resultado esperado (oráculo):** 0 e 9 recusados com erro; 1 e 8 permitidos com dados restantes válidos; sem criação duplicada.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-018 — Apresentar estado e lugares disponíveis

- **Funcionalidade / requisitos:** F002; RF-F002-15; RF-F003-06.
- **Precondições:** Condutor com viagens ativa, indisponível e concluída; pelo menos uma participação aceite.
- **Dados de teste:** Ativa com 3 lugares e 1 ocupado; duas viagens não ativas.
- **Passos:** Abrir As minhas viagens; consultar detalhes; confirmar consistência de lugares e estado.
- **Resultado esperado (oráculo):** Lista de gestão contém as viagens ativas do próprio condutor; lugares disponíveis refletem ocupação; inativas aparecem no histórico adequado.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-019 — Alterar viagem e recalcular rota

- **Funcionalidade / requisitos:** F002; RF-F002-07, RF-F002-08, RF-F002-16, RF-F002-17.
- **Precondições:** Viagem ativa e futura criada pelo próprio condutor; mapas disponíveis.
- **Dados de teste:** Nova hora futura, 4 lugares, origem/destino alternativos válidos.
- **Passos:** Abrir Gerir viagem; alterar dados; guardar; reler lista, detalhes e rota.
- **Resultado esperado (oráculo):** Atualização persistida; disponibilidade recalculada; rota, distância/duração e estado coerentes com dados novos.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-020 — Impedir redução abaixo de lugares ocupados

- **Funcionalidade / requisitos:** F002; RF-F002-08.
- **Precondições:** Viagem ativa do condutor com pelo menos 2 passageiros aceites (AUX-11).
- **Dados de teste:** Reduzir total para 1 lugar.
- **Passos:** Abrir edição; indicar 1; guardar; consultar viagem na API.
- **Resultado esperado (oráculo):** Regra de negócio recusa a redução; dados originais e participações mantidos; erro legível.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-021 — Bloquear alteração por terceiro ou de viagem não editável

- **Funcionalidade / requisitos:** F002; RF-F002-07, RF-F002-09.
- **Precondições:** Viagem de B ativa e viagem concluída do próprio A.
- **Dados de teste:** Conta A autenticada; identificadores das viagens.
- **Passos:** Tentar editar viagem de B por navegação; tentar alterar viagem concluída de A; comparar dados.
- **Resultado esperado (oráculo):** Não surgem ações indevidas ou são rejeitadas pelo servidor; nenhum registo é modificado.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-022 — Indisponibilizar sem eliminar permanentemente

- **Funcionalidade / requisitos:** F002; RF-F002-09, RF-F002-10.
- **Precondições:** Viagem ativa com pedidos e/ou passageiros; condutor autenticado.
- **Dados de teste:** Viagem e participações de teste.
- **Passos:** Acionar Indisponibilizar; cancelar no diálogo; confirmar depois; consultar lista e histórico.
- **Resultado esperado (oráculo):** Cancelar não altera nada; confirmar muda estado, deixa de constar entre viagens ativas e preserva histórico; não há opção de eliminação definitiva.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.


### F003 e F008 — Pesquisa, mapa, rota e custo

#### ATM-023 — Pesquisar viagens por origem e destino

- **Funcionalidade / requisitos:** F003/F008; RF-F003-01, RF-F003-02, RF-F003-05.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; contas confirmadas; várias viagens ativas e futuras em E1/E2.
- **Dados de teste:** Viagens Felgueiras→Porto, Braga→Porto e Felgueiras→Guimarães.
- **Passos:** Abrir Pesquisa; preencher origem/destino; executar pesquisa; comparar resultados com seed.
- **Resultado esperado (oráculo):** Apenas viagens elegíveis e correspondentes aos filtros; ordenação pela partida crescente, sem dados de outras categorias.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-024 — Filtrar disponibilidade e viagens não elegíveis

- **Funcionalidade / requisitos:** F003/F008; RF-F003-01, RF-F003-02, RF-F003-05.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; fixtures com viagem cheia, passada, indisponível e futura com vaga.
- **Dados de teste:** Quatro estados/ocupações explícitos.
- **Passos:** Pesquisar com filtros abrangentes; verificar cada ID da fixture.
- **Resultado esperado (oráculo):** Resultados não expõem viagens cheias, já partidas ou indisponíveis; viagem elegível aparece.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-025 — Pesquisa sem resultados e recuperação após falha

- **Funcionalidade / requisitos:** F003/F008; RF-F003-01; RNF-USA-02.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; API disponível e resposta vazia/falha controlada.
- **Dados de teste:** Filtro inexistente; erro temporário HTTP 503.
- **Passos:** Pesquisar destino não existente; depois simular falha; repetir após restauro.
- **Resultado esperado (oráculo):** Estado vazio distinguível de falha de ligação; mensagem orienta correção/tentar novamente; resultados surgem após recuperação.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-026 — Detalhes, condutor e visibilidade dos passageiros

- **Funcionalidade / requisitos:** F003/F008; RF-F003-03, RF-F003-04, RF-F003-06, RF-F003-07, RF-F003-08.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; viagem pesquisável com passageiro aceite e condutor conhecido.
- **Dados de teste:** Viagem com pontos intermédios privados do passageiro.
- **Passos:** Abrir resultado e detalhes com utilizador externo; depois com condutor ou aceite.
- **Resultado esperado (oráculo):** Dados públicos do condutor e detalhes elegíveis aparecem; pontos de recolha individuais só são visíveis a quem tem permissão.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-027 — Mostrar percurso e pontos autorizados no mapa

- **Funcionalidade / requisitos:** F003/F008; RF-F008-03, RF-F008-04, RF-F008-05, RF-F008-09.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; serviço de mapas fake; geometria disponível se suportada.
- **Dados de teste:** Viagem com 1 ou mais pontos aceites.
- **Passos:** Abrir mapa como condutor/aceite e depois como terceiro; consultar a rota correspondente.
- **Resultado esperado (oráculo):** Condutor/aceite veem percurso autorizado; terceiro vê só origem-destino; caso a API não forneça geometria, exibe estado declarado e regista DEF-BE-054, sem inventar linha.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-028 — Estimativa de custo indicativa

- **Funcionalidade / requisitos:** F003/F008; RF-F008-08, RF-F008-10, RF-F008-11.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; viagem com rota calculada.
- **Dados de teste:** Distância e custo retornados pela API de teste.
- **Passos:** Abrir detalhes, consultar custo por participante; alterar dados relevantes em fixture; repetir.
- **Resultado esperado (oráculo):** Valor é o da API, identificado como estimativa/indicativo; não surge cobrança nem valor fixo desatualizado.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-029 — Mapas indisponíveis e rota por recalcular

- **Funcionalidade / requisitos:** F003/F008; RF-F008-12; RF-F002-17.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; serviço de mapas controlável.
- **Dados de teste:** Falha 502, 503 ou 504 no cálculo ou consulta.
- **Passos:** Desligar stub de mapas; pedir rota/estimativa ou criar viagem; restaurar stub.
- **Resultado esperado (oráculo):** Interface informa indisponibilidade sem congelar; não declara rota ou custo fictício; operação de escrita não fica parcialmente concluída quando a API a recusa.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-030 — Pesquisa e mapa sem localização contínua

- **Funcionalidade / requisitos:** F003/F008; RF-F003-01; RNF-PRI-02.
- **Precondições:** Funcionalidade de pesquisa e mapa I080 disponível na versão integrada; permissões inicialmente não concedidas.
- **Dados de teste:** Filtros de localidades introduzidas pelo utilizador.
- **Passos:** Pesquisar e consultar mapa sem permissão GPS; verificar pedidos de permissão e comportamento em segundo plano.
- **Resultado esperado (oráculo):** É possível usar filtros manuais; não há recolha/seguimento contínuo da localização nem exigência de permissão permanente.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.


### F004 — Pedidos de participação e simulação

#### ATM-031 — Simular impacto de aceitar um pedido

- **Funcionalidade / requisitos:** F004; RF-F004-09; RF-F008-06.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; condutor com pedido pendente e rota atual.
- **Dados de teste:** Pedido válido com recolha e destino; resposta determinística de mapas.
- **Passos:** Abrir pedido pendente; simular; guardar estado inicial; consultar estado final sem aceitar.
- **Resultado esperado (oráculo):** Apresenta impacto na rota/duração/custo conforme API; pedido mantém-se pendente e lugares não mudam.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-032 — Simulação sem permissão ou serviço de mapas

- **Funcionalidade / requisitos:** F004; RF-F004-09; RF-F008-12.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; pedido pendente de B; mapas com falha controlada.
- **Dados de teste:** Conta A não condutora; código 503/504 do stub.
- **Passos:** Simular pedido de terceiro; repetir como condutor com mapas indisponíveis.
- **Resultado esperado (oráculo):** Recusa sem dados alheios quando não autorizado; erro de mapas claro e sem aceitação implícita.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-033 — Submeter pedido de participação válido

- **Funcionalidade / requisitos:** F004; RF-F004-01, RF-F004-02, RF-F004-13.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; passageiro confirmado distinto do condutor, viagem ativa com vaga.
- **Dados de teste:** Viagem de B; pontos opcionais de recolha/destino.
- **Passos:** Abrir detalhes; pedir participação; confirmar; consultar pedido como passageiro/condutor.
- **Resultado esperado (oráculo):** Pedido fica Pendente, não ocupa lugar até aceitação; condutor pode consultá-lo; evento de notificação preparado.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-034 — Impedir pedido duplicado e viagem inválida

- **Funcionalidade / requisitos:** F004; RF-F004-05, RF-F004-06, RF-F004-13, RF-F004-18, RF-F004-20, RF-F004-21.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; passageiro com pedido já pendente.
- **Dados de teste:** Mesma viagem; viagem cheia/indisponível.
- **Passos:** Tentar novo pedido na mesma viagem; repetir em viagem sem elegibilidade; introduzir pontos vazios, iguais e maiores que 200 caracteres.
- **Resultado esperado (oráculo):** Recusa com causa; não duplica pedido, rejeita pontos inválidos e não altera os lugares existentes.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-035 — Condutor não pede lugar na própria viagem

- **Funcionalidade / requisitos:** F004; RF-F004-05.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; condutor autenticado com viagem ativa.
- **Dados de teste:** ID da própria viagem.
- **Passos:** Abrir a própria viagem e tentar solicitar participação.
- **Resultado esperado (oráculo):** A ação não aparece ou é recusada com mensagem; não é criado pedido.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-036 — Condutor consulta e aceita pedido pendente

- **Funcionalidade / requisitos:** F004; RF-F004-08, RF-F004-10, RF-F004-11.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; condutor com pedido pendente e 2 lugares livres.
- **Dados de teste:** Pedido de passageiro diferente; rota de mapas disponível.
- **Passos:** Abrir pedidos pendentes; aceitar; reler pedidos e lugares.
- **Resultado esperado (oráculo):** Pedido passa a aceite, sai dos pendentes e ocupa 1 lugar; informação de disponibilidade e rota atualizada.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-037 — Rejeitar pedido pendente sem ocupar lugar

- **Funcionalidade / requisitos:** F004; RF-F004-12.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; condutor e pedido pendente.
- **Dados de teste:** Pedido válido.
- **Passos:** Abrir pedido; selecionar rejeição; confirmar; consultar dados.
- **Resultado esperado (oráculo):** Estado Rejeitado; vagas inalteradas; outro utilizador não consegue rejeitar.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-038 — Passageiro cancela pedido pendente ou aceite

- **Funcionalidade / requisitos:** F004; RF-F004-15, RF-F004-16, RF-F004-17.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; pedido pendente e outro aceite antes da partida.
- **Dados de teste:** Duas viagens distintas para isolar variantes.
- **Passos:** Cancelar pendente; cancelar aceite em outra viagem; verificar disponibilidade.
- **Resultado esperado (oráculo):** Cancelamento refletido; pedido aceite libertou o lugar; cancelamento após partida é recusado pela regra da API.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-039 — Não aceitar pedido com viagem lotada

- **Funcionalidade / requisitos:** F004; RF-F004-11.
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; viagem sem vagas e pedido pendente existente.
- **Dados de teste:** Total de 1 lugar previamente ocupado por outro passageiro.
- **Passos:** Tentar aceitar pedido adicional.
- **Resultado esperado (oráculo):** Recusa com motivo; não sobreocupa o veículo nem transforma pedido em Aceite.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-040 — Dupla decisão concorrente e atualização de estado

- **Funcionalidade / requisitos:** F004; RF-F004-10, RF-F004-12; RNF-FIA-01 (consistência em decisões concorrentes e capacidade).
- **Precondições:** Funcionalidade de participações I081 disponível na versão integrada; um pedido pendente acessível em duas sessões do condutor.
- **Dados de teste:** Duas solicitações de aceitação/rejeição quase simultâneas.
- **Passos:** Executar decisões concorrentes; atualizar ecrãs.
- **Resultado esperado (oráculo):** Estado final é único e consistente; segunda ação encontra conflito ou estado atualizado sem criar duas participações.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.


### F005 — Mensagens e avisos

#### ATM-041 — Enviar mensagem a utilizador confirmado

- **Funcionalidade / requisitos:** F005; RF-F005-01.
- **Precondições:** Funcionalidade de mensagens e avisos I082 disponível na versão integrada; A e B confirmados.
- **Dados de teste:** Mensagem curta em PT-PT.
- **Passos:** Abrir conversa de B; escrever; enviar; reabrir conversa em A e B.
- **Resultado esperado (oráculo):** Uma mensagem aparece com remetente, conteúdo e ordem corretos; persistida na API.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-042 — Recusar mensagem inválida ou para si próprio

- **Funcionalidade / requisitos:** F005; RF-F005-02.
- **Precondições:** Funcionalidade de mensagens e avisos I082 disponível na versão integrada; A confirmado.
- **Dados de teste:** Texto vazio, só espaços, 1001 caracteres; destinatário=A.
- **Passos:** Tentar enviar cada variante.
- **Resultado esperado (oráculo):** Nenhuma mensagem inválida é enviada; cada caso apresenta causa; texto anterior não é perdido sem necessidade.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-043 — Consultar conversas próprias em ordem cronológica

- **Funcionalidade / requisitos:** F005; RF-F005-03, RF-F005-04, RF-F005-08, RF-F005-09.
- **Precondições:** Funcionalidade de mensagens e avisos I082 disponível na versão integrada; A com conversas com B e C, D externo.
- **Dados de teste:** Mensagens em instantes distintos e de remetentes distintos.
- **Passos:** Abrir lista de conversas; abrir conversa com B; como D tentar consultar dados alheios.
- **Resultado esperado (oráculo):** Só conversas próprias surgem; ordenação de conversas por última mensagem e de mensagens por cronologia; D não recebe conteúdo de A.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-044 — Criar aviso do condutor para participantes

- **Funcionalidade / requisitos:** F005; RF-F005-05, RF-F005-10.
- **Precondições:** Funcionalidade de mensagens e avisos I082 disponível na versão integrada; viagem ativa com passageiros aceites; condutor confirmado.
- **Dados de teste:** Aviso válido e aviso com 101 caracteres.
- **Passos:** Publicar aviso válido; tentar aviso acima do limite.
- **Resultado esperado (oráculo):** Aviso válido persiste e gera destinatários esperados; o maior que 100 caracteres é recusado com erro claro.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-045 — Consultar avisos e impedir publicação sem autorização

- **Funcionalidade / requisitos:** F005; RF-F005-06, RF-F005-07.
- **Precondições:** Funcionalidade de mensagens e avisos I082 disponível na versão integrada; condutor, passageiro aceite e terceiro.
- **Dados de teste:** Viagem com aviso e 1 passageiro aceite.
- **Passos:** Consultar avisos como aceite; tentar publicar como passageiro e como terceiro.
- **Resultado esperado (oráculo):** Participante autorizado consulta; só condutor publica; terceiro não obtém conteúdo privado.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-046 — Estado vazio, carregamento e falha de mensagens

- **Funcionalidade / requisitos:** F005; RF-F005-03; RNF-USA-02.
- **Precondições:** Funcionalidade de mensagens e avisos I082 disponível na versão integrada; conversa sem mensagens e API com falha controlável.
- **Dados de teste:** Lista vazia e 503 temporário.
- **Passos:** Abrir conversa vazia; cortar resposta; restabelecer e repetir.
- **Resultado esperado (oráculo):** Estado vazio não é erro; falha tem mensagem recuperável; reconsulta mantém mensagens existentes sem duplicação.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-047 — Texto de mensagens e avisos não interpretado como HTML

- **Funcionalidade / requisitos:** F005; RF-F005-01, RF-F005-05; RNF-SEG-12.
- **Precondições:** Funcionalidade de mensagens e avisos I082 disponível na versão integrada; mensagens e avisos com payloads X1–X4 previamente gravados.
- **Dados de teste:** Payloads da secção 6 do I090.
- **Passos:** Abrir conversa e avisos de outro utilizador; inspecionar apresentação.
- **Resultado esperado (oráculo):** Payloads aparecem literalmente como texto, sem executar scripts, imagens nem formatação HTML.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.


### F006 — Notificações, tempo real e push

#### ATM-048 — Listagem de notificações e estado de leitura

- **Funcionalidade / requisitos:** F006; RF-F006-07.
- **Precondições:** Funcionalidade de notificações I083 disponível na versão integrada; A recebeu notificações de pedido e alteração.
- **Dados de teste:** Duas notificações com datas e estados distintos.
- **Passos:** Abrir Notificações; ordenar; abrir detalhe; voltar.
- **Resultado esperado (oráculo):** Só notificações de A; recentes primeiro; estado lida/não lida coerente com a API.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-049 — Marcar notificação como lida

- **Funcionalidade / requisitos:** F006; RF-F006-19 (marcar uma notificação como lida); RF-F006-07 (consulta da lista).
- **Precondições:** Funcionalidade de notificações I083 disponível na versão integrada; notificação de A por ler.
- **Dados de teste:** ID de notificação própria.
- **Passos:** Abrir e marcar como lida; recarregar ecrã.
- **Resultado esperado (oráculo):** Estado persiste; outras notificações conservam estado; outro utilizador não altera esta notificação.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-050 — Marcar todas como lidas

- **Funcionalidade / requisitos:** F006; RF-F006-19 (marcar todas como lidas); RF-F006-07 (consulta da lista).
- **Precondições:** Funcionalidade de notificações I083 disponível na versão integrada; A com 3 notificações por ler.
- **Dados de teste:** IDs e estados iniciais.
- **Passos:** Acionar Marcar todas como lidas; recarregar.
- **Resultado esperado (oráculo):** As de A ficam lidas; notificações de B não mudam.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-051 — Atualização em tempo real do pedido de participação

- **Funcionalidade / requisitos:** F006; RF-F006-13, RF-F006-15.
- **Precondições:** Funcionalidade de notificações I083 disponível na versão integrada; condutor online com app aberta; passageiro noutro cliente.
- **Dados de teste:** Novo pedido de participação na viagem do condutor.
- **Passos:** Abrir lista de pedidos/notificações; criar pedido no outro cliente; observar atualização sem atualizar manualmente.
- **Resultado esperado (oráculo):** Evento é apresentado uma vez e corresponde ao pedido confirmado, respeitando os requisitos RF-F006-13 e RF-F006-15.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-052 — Registar token de dispositivo para push

- **Funcionalidade / requisitos:** F006; RF-F006-10.
- **Precondições:** Funcionalidade de notificações I083 disponível na versão integrada; conta autenticada; execução em E1/E2 com serviço FCM substituído e em dispositivo Android com Firebase/FCM real.
- **Dados de teste:** Em E1/E2, token fictício único por dispositivo de teste; na execução real, token FCM real obtido no dispositivo, nunca incluído no documento, no repositório ou em logs publicados.
- **Passos:** E1/E2: autorizar notificações quando necessário, registar o token fictício e verificar na store de teste a associação à conta/dispositivo. Execução FCM real: obter token FCM com a aplicação, registar através da API e confirmar a associação por evidência técnica protegida; não divulgar o valor do token.
- **Resultado esperado (oráculo):** Em E1/E2 confirma-se exclusivamente o contrato e a persistência do registo simulado, não a entrega por Firebase. Na execução real Firebase/FCM, confirmar o registo de token real e associação à conta/dispositivo, sem exposição do token. Os registos de execução simulada e real são individualizados na I100.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-053 — Receção de push e abertura do ecrã correto

- **Funcionalidade / requisitos:** F006; RF-F006-11, RF-F006-13, RF-F006-01, RF-F006-12, RF-F006-16, RF-F006-17; RI-03; P-24.
- **Precondições:** Funcionalidade de notificações I083 disponível na versão integrada; contas distintas de condutor e passageiro; aplicação destinatária em segundo plano. E1/E2 têm serviço de push substituído. Na execução real utiliza-se Firebase/FCM configurado, dispositivo Android compatível e permissões concedidas.
- **Dados de teste:** Três eventos elegíveis: Pedido recebido (para o condutor), Pedido aceite e Pedido rejeitado (para o passageiro). Controlos negativos: Nova mensagem, Alteração da viagem e Aviso não originam push (RF-F006-12/16/17).
- **Passos:** E1/E2 (simulação): produzir cada evento, verificar o encaminhamento para o serviço substituído e injetar a mensagem no Android; tocar e observar a navegação. FCM real: produzir o mesmo evento na API, verificar a entrega efetiva pela infraestrutura Firebase/FCM e tocar na notificação do sistema, sem injeção artificial. Repetir para os três tipos e controlos negativos; distinguir evidência simulada de evidência real.
- **Resultado esperado (oráculo):** Os três tipos elegíveis originam push Android para o destinatário correto, dentro de 30 segundos (P-24), e o toque na notificação abre a aplicação na `MainActivity`. **Pedido recebido:** o condutor acede a **As minhas viagens → viagem correspondente → Pedidos pendentes**, onde consulta o pedido. **Pedido aceite:** o passageiro consulta **Histórico → Pedidos efetuados → pedido correspondente**, com estado **Aceite**, e os detalhes da viagem. **Pedido rejeitado:** o passageiro consulta **Histórico → Pedidos efetuados → pedido correspondente**, com estado **Rejeitado**. A consulta respeita a sessão do destinatário e as permissões de acesso. Os eventos Nova mensagem, Alteração da viagem e Aviso permanecem na lista interna da aplicação e não originam push. A verificação E1/E2 comprova o fluxo com serviço substituído; a execução Firebase/FCM real comprova a entrega ao dispositivo Android.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-054 — Permissão de notificações negada e logout do dispositivo

- **Funcionalidade / requisitos:** F006; RF-F006-18; RNF-USA-02.
- **Precondições:** Funcionalidade de notificações I083 disponível na versão integrada; Android com permissões controláveis.
- **Dados de teste:** Permissão negada; token registado anteriormente; sessão A.
- **Passos:** Negar permissão de push; testar uso normal; terminar sessão; enviar evento posterior.
- **Resultado esperado (oráculo):** Fluxos de app permanecem acessíveis sem push; após logout o token do dispositivo é desassociado e não recebe pushes de A.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.


### F007 e F009 — Avaliações e histórico

#### ATM-055 — Avaliar após viagem concluída e impedir avaliações indevidas

- **Funcionalidade / requisitos:** F007; RF-F007-01, RF-F007-02, RF-F007-03, RF-F007-04, RF-F007-05, RF-F007-07, RF-F007-08, RF-F007-09.
- **Precondições:** Funcionalidade de avaliações e histórico I084 disponível na versão integrada; viagem concluída e condutor/passageiro aceites.
- **Dados de teste:** Valores 1,5 e 0,6; tentativa de autoavaliação e repetição.
- **Passos:** Avaliar outro participante com 1–5; tentar 0/6 e autoavaliação; repetir par; editar classificação permitida; consultar lista, total e média no perfil público.
- **Resultado esperado (oráculo):** Avaliação legítima persiste; escala 1–5; inválidas/duplicada recusadas; edição atualiza valor sem segunda avaliação do par; a consulta apresenta média e total sem revelar autor das avaliações.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-056 — Histórico próprio, avaliações e estados das viagens

- **Funcionalidade / requisitos:** F009; RF-F009-01, RF-F009-02, RF-F009-03, RF-F009-04, RF-F009-05, RF-F009-06, RF-F009-07, RF-F009-08.
- **Precondições:** Funcionalidade de avaliações e histórico I084 disponível na versão integrada; A com viagem como condutor, outra como passageiro, notificação e aviso; B sem relação.
- **Dados de teste:** Datas diferentes, estados ativa/concluída/indisponível.
- **Passos:** Abrir Histórico de A; consultar categorias e ordenação; autenticar B e comparar.
- **Resultado esperado (oráculo):** A vê apenas registos seus e respetivos estados, ordenados pela data de partida; B não consulta o histórico de A; guardar decisão D-029 se alterar o universo exibido.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.


## 6. Requisitos não funcionais e transversais

#### ATM-057 — HTTPS em produção e contrato comum de API

- **Funcionalidade / requisitos:** RNF; RNF-SEG-01; RNF-COM-03.
- **Precondições:** APK debug E1 e variante release configurada para ambiente público; web e mobile conhecidos.
- **Dados de teste:** Endpoint `api/v1/` controlado e URLs em build.
- **Passos:** Inspecionar `BuildConfig` e configuração de rede; comunicar com E1 debug; tentar HTTP em release; comparar rotas da API com web.
- **Resultado esperado (oráculo):** HTTP local autorizado apenas no debug nos hosts definidos; release não permite HTTP livre; ambos os clientes usam contrato API v1 e rotas compatíveis.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-058 — Ausência de credenciais e segredos no APK e código

- **Funcionalidade / requisitos:** RNF; RNF-SEG-08.
- **Precondições:** APK release/debug gerado; repositório e configurações acessíveis.
- **Dados de teste:** Lista de padrões de segredos, nomes de variáveis e entradas do manifesto.
- **Passos:** Pesquisar por chaves, passwords e tokens reais nos ficheiros rastreados; inspecionar strings e artefacto APK; verificar `google-services.json`.
- **Resultado esperado (oráculo):** Nenhum segredo real hardcoded em código, APK ou logs; valores de teste não se confundem com credenciais de produção; achados são registados.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-059 — Renderização segura de conteúdo do utilizador e URLs

- **Funcionalidade / requisitos:** RNF; RNF-SEG-12.
- **Precondições:** App integrada com perfil, viagens, mensagens e avisos; conteúdo de teste pré-gravado na API.
- **Dados de teste:** X1 `<script>alert(1)</script>`; X2 `<img src=x onerror=alert(1)>`; X3 `"><svg onload=alert(1)>`; X4 `<b>negrito</b>`; X5 `javascript:alert(1)`.
- **Passos:** Guardar X1–X4 em campos de texto suportados e visualizar por segundo utilizador; tentar adicionar X5 como URL externa.
- **Resultado esperado (oráculo):** Etiquetas aparecem como texto sem execução/renderização HTML; X5 é recusado; a aplicação não introduz `WebView` inseguro.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-060 — Rate limiting de autenticação e restantes rotas

- **Funcionalidade / requisitos:** RNF; RNF-SEG-13 (confirmado na ER v02, §4.1; P-29 não define os limiares de pedidos).
- **Precondições:** Ambiente isolado só para este cenário; sem partilhar janela de limitador com outros testes.
- **Dados de teste:** 11 chamadas em 1 minuto para cada rota limitada a 10/minuto/IP (login, registo e reenvio); 101 chamadas em 1 minuto para uma rota geral (100/minuto por utilizador autenticado ou IP sem sessão). Conforme I090 §6, RTM v02 e decisão G19; não partilhar janela entre variantes.
- **Passos:** Em ambiente isolado, executar 10 pedidos dentro da janela da rota de autenticação e o 11.º na mesma origem IP; repetir separadamente para registo/reenvio. Numa rota geral, executar 100 pedidos na janela por utilizador autenticado (ou por IP sem sessão) e o 101.º. Confirmar HTTP 429 ao exceder; após renovação da janela testar novamente pedido legítimo. Não desativar o limitador.
- **Resultado esperado (oráculo):** Até ao limite da janela não há bloqueio por rate limiting; o 11.º pedido nas rotas de autenticação e o 101.º na rota geral recebem HTTP 429, com indicação de tentar mais tarde na UI; depois da janela o pedido legítimo volta a ser admitido. Qualquer 4xx/5xx por outra regra funcional é distinguido de 429. Não se exige nem permite desativar o limitador.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-061 — Não solicitar nem conservar localização contínua

- **Funcionalidade / requisitos:** RNF; RNF-PRI-02.
- **Precondições:** App instalada e permissões do sistema repostas.
- **Dados de teste:** Dispositivo sem localização concedida.
- **Passos:** Abrir pesquisa, mapa e segundo plano; examinar permissões e serviços em execução.
- **Resultado esperado (oráculo):** Não solicita localização permanente/contínua nem mantém recolha em segundo plano; filtros de localidade funcionam sem GPS contínuo.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-062 — Português europeu e mensagens de erro úteis

- **Funcionalidade / requisitos:** RNF; RNF-USA-01; RNF-USA-02.
- **Precondições:** Versão integrada das funcionalidades I077 a I084 identificada; app configurada em pt-PT.
- **Dados de teste:** Erros 400, 401, 403, 409, 429, 503 e estado vazio.
- **Passos:** Percorrer fluxos principais e provocar erros; recolher capturas; voltar após correção.
- **Resultado esperado (oráculo):** Terminologia e mensagens em PT-PT, sem textos técnicos crus; identificam causa e ação de recuperação; navegação permanece utilizável.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-063 — Robustez perante perda de rede, rotação e restauro

- **Funcionalidade / requisitos:** RNF; RNF-USA-02; RNF-FIA-04 (data da viagem).
- **Precondições:** Android 10+; formulário F002 preenchido; API disponível e depois desligada.
- **Dados de teste:** Formulário parcialmente preenchido; modo de avião; rotação 90°.
- **Passos:** Preencher viagem; rodar; desligar rede; tentar guardar e carregar lista; restabelecer rede; repetir.
- **Resultado esperado (oráculo):** Dados de edição não desaparecem sem aviso; não cria viagem duplicada; falha de rede é recuperável e distingue-se de validação; hora preservada.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-064 — Hora de Portugal e mudança de fuso/DST

- **Funcionalidade / requisitos:** RNF; RNF-FIA-04.
- **Precondições:** Viagens de inverno e verão sem ambiguidades temporais; emulador com fusos configuráveis.
- **Dados de teste:** Partidas `2026-12-15T14:00:00Z` e `2027-07-15T13:00:00Z`, ambas 14:00 em Portugal continental; fusos Europe/Lisbon e Europe/Berlin.
- **Passos:** Abrir viagens em dois fusos; criar/editar hora visível; inspecionar instante UTC recebido.
- **Resultado esperado (oráculo):** A interface apresenta 14:00 em Portugal continental em ambos os casos e nos dois fusos do dispositivo; API recebe UTC Z sem deslocações.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-065 — Compatibilidade Android 10 e versão mais recente disponível

- **Funcionalidade / requisitos:** RNF; RNF-COM-02.
- **Precondições:** Emulador API 29 e emulador Android mais recente estável disponível; APK alinhado com commit.
- **Dados de teste:** Conjunto dos fluxos críticos ATM-001, 007, 009, 014, 023, 033, 041, 048, 055, 056.
- **Passos:** Executar o subconjunto crítico em ambos os Android; registar fingerprint/modelo/API e logs.
- **Resultado esperado (oráculo):** Todos os fluxos incluídos na versão candidata funcionam sem crash ou incompatibilidade; qualquer falha ou impossibilidade real de execução é registada explicitamente na I100, não convertida automaticamente em aprovação.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.

#### ATM-066 — Quality gates e bloqueio da integração na pipeline

- **Funcionalidade / requisitos:** RNF; RNF-MAN-02; RNF-MAN-03.
- **Precondições:** Quality gates I094/I095 definidos; workflow do frontend mobile a apontar para o commit de teste identificado.
- **Dados de teste:** Commit válido e alteração de teste controlada que deve falhar.
- **Passos:** Executar `:core:check`, `:app:verificarCoberturaApp`, `:app:lintDebug`, ktlint, build e sistema; introduzir falha de teste numa branch de demonstração; observar CI.
- **Resultado esperado (oráculo):** Commit válido satisfaz gates documentados; falha bloqueia integração e fica registada; limiar de cobertura por módulo é ≥80%; restauro deixa pipeline verde.
- **Verificação de aceitação:** observar manualmente os passos e resultados. O executor prepara os dados e as falhas controladas; o utilizador valida o resultado. Registar Passou/Falhou/Bloqueado na I100.



## 7. Matriz de rastreabilidade da especificação

A matriz associa **eventos de negócio** aos cenários que os provocam e **comportamentos verificáveis** aos ATM que os observam. As correspondências abaixo foram cruzadas com a RTM v02 e o plano I090. A RTM v02 é uma fotografia de m3 e não prova, por si, que a implementação mobile m4 foi testada ou aprovada.

| Requisito / fluxo | Casos ATM relevantes | Fonte de correspondência | Situação documental |
| --- | --- | --- | --- |
| F001 — RF-F001-02–08, 10–12, 14–17, 19–22 | 001–012 | RTM v02; I078 | IDs confrontados com RTM v02; ER v02 por confronto final |
| F002 — RF-F002-01–10, 15–17 | 013–022 | RTM v02; I079 | IDs confrontados; rever cobertura fina na ER v02 |
| F003 — RF-F003-01–08 | 023–027, 030 | RTM v02; I080 | IDs confrontados |
| F008 — RF-F008-01–06, 08–12; RF-F008-07 (mapa) | 014, 019, 027–032 | RTM v02; I080/I081 | Acrescentada referência explícita ao mapa RF-F008-07 |
| F004 — RF-F004-01–02, 04–21 (exceto RF-F004-03 descontinuado) | 031–040 | RTM v02; I081 | RF-F004-03 descontinuado; restantes IDs confrontados |
| F005 — RF-F005-01–02, 04–10 (RF-F005-03 descontinuado) | 041–047 | RTM v02; I082 | RF-F005-03 descontinuado; correspondências confrontadas |
| F006 — registo dos eventos RF-F006-01–06, 13–14 | 033, 036–038, 041, 044, 048, 051 | RTM v02; I081–I083 | Eventos geradores separados das verificações de receção; ver §7.2 |
| F006 — consulta RF-F006-07 e marcação de leitura RF-F006-19 | 048–050 | ER v02, §3.6; I083 | RF-F006-19 confirma marcar uma ou todas; RF-F006-08 é uma lacuna histórica |
| F006 — tempo real RF-F006-09, 15 | 051; 036–037 | RTM v02; I083 | RF-F006-09 é atualização de estado em tempo real do passageiro; RF-F006-19 é marcação de leitura |
| F006 — push RF-F006-10–12, 16–18; RI-03 | 052–054 | RTM v02; I083 | Tipos permitidos/vedados e simulado/real separados |
| F007 — RF-F007-01–05, 07–11 | 055 | RTM v02; I084 | Rastreado a ER v02 e RTM v02 |
| F009 — RF-F009-01–08 | 018, 022, 056 | RTM v02; I084 | Rastreado a ER v02 e RTM v02 |
| RNF-SEG-01, RNF-COM-03 | 057 | I090 §2; RTM v02 | Identificados |
| RNF-SEG-08 | 058 | I090 §2; RTM v02 | Identificado |
| RNF-SEG-12 | 047, 059 | I090 §§2, 6; RTM v02 | Identificado |
| RNF-SEG-13 (10/min autenticação; 100/min rotas gerais) | 060 | I090 §6; RTM v02; Checklist v02 | ID e limiares confirmados nestas fontes |
| RNF-FIA-01 (concorrência/capacidade) | 039, 040 | RTM v02; Checklist v02 | Identificado |
| RNF-PRI-01 (minimização de dados) | 012 | RTM v02; Checklist v02 | Referência complementar à privacidade do perfil |
| RNF-PRI-02 | 030, 061 | I090 §2; RTM v02 | Identificado |
| RNF-USA-01/02 | 025, 046, 054, 062, 063 | I090 §2; RTM v02 | Identificados |
| RNF-FIA-04 | 016, 063, 064 | I090 §2; RTM v02 | Identificado |
| RNF-COM-02 | 065 | I090 §2; RTM v02 | Identificado |
| RNF-MAN-02/03 | 066 | I090 §§2, 9; RTM v02 | Identificados |

### 7.1. Validação da rastreabilidade

A RTM v02 considerada é o documento de m3 com atualização em 06/11/2026. A Checklist v02 documenta as decisões de revisão que foram incorporadas na ER v02. A ER v02, submetida em m3, confirma os identificadores, os requisitos descontinuados, o âmbito Android e os critérios de aceitação usados nesta especificação. Os ATM-049 e ATM-050 ficam rastreados a RF-F006-19; a lacuna RF-F006-08 não é reutilizada. O cenário ATM-053 reflete a abertura da aplicação pela I083 e identifica os percursos de consulta associados a cada tipo de pedido. A atualização da RTM em m4 é tratada na I104.

### 7.2. F006 — Eventos que originam notificações e comprovação da receção

| Evento provocado no sistema | Requisito de registo interno | ATM que provoca/verifica o evento | Comprovação de receção e canal |
| --- | --- | --- | --- |
| Condutor recebe novo pedido | RF-F006-13 | ATM-033 | ATM-048 (lista interna), ATM-051 (tempo real), ATM-053 (push Pedido recebido) |
| Pedido é aceite ou rejeitado | RF-F006-01 | ATM-036/037 | ATM-048 (lista interna), ATM-051 (atualização em tempo real do passageiro, RF-F006-09), ATM-053 (push Pedido aceite/rejeitado) |
| Passageiro cancela participação aceite | RF-F006-02 | ATM-038 | ATM-048 (notificação interna); **não** ATM-053 |
| Condutor indisponibiliza viagem | RF-F006-03 | ATM-022 | ATM-048 (notificação interna); **não** ATM-053 |
| Condutor altera dados da viagem | RF-F006-04 | ATM-019 | ATM-048 (notificação interna); **não** ATM-053 |
| Condutor envia aviso | RF-F006-05 | ATM-044 | ATM-048 (notificação interna); sem push (RF-F006-17) |
| Utilizador envia mensagem | RF-F006-06 | ATM-041 | ATM-048 (notificação interna); sem push (RF-F006-12) |
| Outro passageiro entra/sai da viagem | RF-F006-14 | ATM-036/038 | ATM-048 (notificação interna); sem push por alteração da viagem (RF-F006-16) |

ATM-052 verifica o registo do dispositivo/token, não a receção do push. ATM-053 é o cenário específico para entrega e navegação de push. ATM-048 verifica a lista interna; ATM-051 verifica atualizações na aplicação ativa. A execução FCM real verifica a entrega ao Android; a execução simulada verifica o contrato com o serviço substituído. As evidências são registadas na I100.

## 8. Referências e manutenção documental

Esta especificação abrange as funcionalidades F001–F009 da implementação mobile de m4. A rastreabilidade segue a Especificação de Requisitos v02 (versão interna v05), a RTM v02 de m3, a Checklist de Aprovação dos Requisitos v02 e o Plano de Testes Mobile I090. A implementação de notificações e push corresponde à I083, com Firebase/FCM real funcional e testado. Os cenários ATM-052 e ATM-053 distinguem os procedimentos com serviço substituído em E1/E2 da execução real Firebase/FCM. O histórico de execução e as evidências de aceitação são mantidos na I100. A versão documental da presente especificação é v02.
