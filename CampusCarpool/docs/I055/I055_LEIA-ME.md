# I055 — Testes de Integração da API CampusCarpool (Postman)

## Objetivo

Documentar e executar testes de integração à API do projeto **CampusCarpool**, usando o Postman para verificar endpoints, autenticação, regras de negócio, respostas HTTP e tratamento de erros.


## Ambiente de execução

- API: ASP.NET Core / .NET 10, executada localmente.
- Endereço de desenvolvimento: `http://localhost:5100`.
- Ferramenta: Postman (collection e environment próprios da I055).
- Dados: base de dados de desenvolvimento com contas académicas confirmadas e recursos de teste.
- Integração de mapas: serviços HeiGIT/OpenRouteService.

### Iniciar a API

Na raiz do repositório:

```powershell
dotnet run --project .\src\CampusCarpool.Api\CampusCarpool.Api.csproj
```

Confirmar que o processo está em execução antes de enviar pedidos pelo Postman.

## Preparação no Postman

1. Importar a collection da I055 e o ficheiro de environment de teste, se necessário.
2. Selecionar o environment apropriado e confirmar `baseUrl = http://localhost:5100`.
3. Configurar **localmente** as passwords das duas contas académicas de teste já confirmadas, sem as incluir na exportação.
4. Verificar os valores de `origem`, `destino` e uma `partidaFutura` que ainda seja futura na data da execução.
5. Executar primeiro os logins, para obter os tokens JWT. Os pedidos seguintes devem usar o token do papel correto (condutor ou passageiro).
6. Utilizar identificadores reais de viagens, pedidos, participações, notificações e utilizadores. Não inventar IDs para testes positivos.

### Variáveis relevantes

| Variável | Finalidade |
| --- | --- |
| `baseUrl` | Endereço local da API |
| `emailCondutor`, `passwordCondutor` | Login do condutor de teste |
| `emailPassageiro`, `passwordPassageiro` | Login do passageiro de teste |
| `condutorToken`, `passageiroToken` | JWTs obtidos após login |
| `origem`, `destino`, `partidaFutura` | Dados para criação de viagens |
| `viagemId`, `pedidoId`, `participacaoId` | Recursos existentes na base de dados |
| `notificacaoId`, `destinatarioId` | Notificação e destinatário reais |
| `email`, `password`, `confirmToken` | Cenários manuais de registo e confirmação; exigem dados válidos e, no caso do token, não reutilizados |

**Segurança:** não publicar passwords, JWTs, tokens de confirmação, chaves de mapas, chaves de email ou outros segredos no Git. Rever o environment antes de o exportar.

## Execução recomendada

### Runner automático

Executar **apenas a pasta `00 — RUNNER AUTOMÁTICO (19 pedidos)`**:

- 1 iteração;
- intervalo inicial entre pedidos: 2000 ms;
- environment com credenciais locais válidas;
- backend e serviço de mapas disponíveis.

Esta pasta cria recursos de teste e usa os respetivos IDs nos pedidos seguintes.

**Evidência observada numa execução:** **24 testes aprovados, 0 falhados, 0 ignorados e 0 erros**.

### Cenários manuais

Os restantes pedidos devem ser executados com pré-condições específicas, e não indiscriminadamente numa única sequência. Exemplos:

- Registo: conta académica ainda não existente; a confirmação requer token válido enviado por email.
- Simulação: viagem **ativa e futura** com pedido de participação associado.
- Rejeição/aceitação: participação em estado **pendente**; repetir transições pode devolver `409 Conflict`.
- Conclusão: viagem no estado elegível e com data de partida apropriada.
- Mensagens: destinatário real, diferente do utilizador autenticado.
- Notificações: ID existente e pertencente ao utilizador autenticado.
- Avaliações: viagem concluída e participantes elegíveis, incluindo cenários de duplicação e alteração.
- Testes negativos: os códigos `400`, `401`, `403`, `404` e `409` podem ser **resultados corretos** quando são precisamente os esperados pela asserção.

**Atenção às dependências:** executar pedidos de indisponibilização, conclusão, rejeição ou cancelamento pode invalidar dados que outros testes ainda precisam de utilizar. Para testes destrutivos ou não repetíveis, preparar recursos independentes.

## Resultados observados

| Execução | Passed | Failed | Skipped | Observações |
| --- | ---: | ---: | ---: | --- |
| Pasta Runner Automático | 24 | 0 | 0 | Fluxo automático validado numa execução local |
| Collection completa (último resultado comunicado) | 105 | 20 | Não indicado nesse resultado | Execução conjunta com cenários manuais e dependências de estado |

O número de **asserções** pode ser superior ao número de pedidos HTTP, pois um pedido pode conter vários testes.

**Interpretação:** os 20 testes falhados na execução completa **não devem ser declarados aprovados**. Alguns foram posteriormente verificados individualmente com dados adequados, mas isso não demonstra que a execução integral seja repetível nem exclui problemas adicionais. A aprovação da issue depende dos critérios de aceitação aplicáveis e da revisão das evidências.

### Verificações manuais confirmadas durante a análise

- Cálculo de rota: `200 OK` após correção da integração externa de mapas.
- Criação de viagem e pedido associado: `201 Created` em execuções individuais.
- Simulação de percurso com viagem ativa e pedido correspondente: `200 OK`.
- Rejeição de participação pendente: `204 No Content`.
- Paginação inválida de notificações (`pagina=0&tamanhoPagina=101`): `400 Bad Request`.
- Marcar notificação como lida: `204 No Content`.
- Enviar mensagem entre utilizadores distintos: `201 Created`.
- Consultar conversa entre esses utilizadores: `200 OK`.

Estes resultados são observações pontuais. Não substituem relatórios exportados de execução.

## Correção da integração de mapas

Durante a execução foram observados `502 Bad Gateway` nos endpoints de rotas e criação de viagem. Os logs da API mostraram respostas `403` ao antigo endpoint `https://api.openrouteservice.org/geocode/search`.

Foi feita a adaptação de construção dos URLs para os endpoints HeiGIT, incluindo:

- geocodificação: `https://api.heigit.org/pelias/v1/search`;
- cálculo de rota: `https://api.heigit.org/openrouteservice/v2/directions/driving-car`.

Após a alteração e reinício da API, o pedido de cálculo de rota respondeu `200 OK`.

**Revisão antes do commit:** confirmar que os caminhos e o fallback de `Maps:BaseUrl` estão coerentes, que as alterações não quebram testes de integração existentes e que **`Maps:ApiKey` não foi incluída no repositório**.

## Limitações e pontos pendentes

1. A collection completa inclui cenários dependentes de estados distintos ou irreversíveis e não está demonstrada como repetível numa só execução.
2. Registo, reenvio e confirmação de email necessitam de conta de teste e token válidos; o token não é gerado automaticamente a partir da resposta de registo.
3. Cenários de avaliações necessitam de viagens concluídas e participações aceites específicas.
4. Uma simulação individual devolveu um desvio de percurso aparentemente elevado para Porto → Lousada → Felgueiras. Merece análise funcional separada; o `200 OK` apenas demonstra que houve resposta, não que o trajeto seja geograficamente adequado.
5. Antes de assinalar a I055 como aprovada, confrontar estas evidências com os seus critérios de aceitação e, quando necessário, anexar as capturas e resultados de execução.
