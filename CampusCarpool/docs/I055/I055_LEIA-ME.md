# I055 — Testes de integração da API CampusCarpool (Postman)

**Projeto:** G15 — CampusCarpool
**Milestone:** m3 · **Sprint:** 05 · **Issue:** I055
**Branch:** `feature/i055-testes-integracao-postman`
**Atualização documental:** 29/10/2026

## 1. Objetivo

A I055 disponibiliza uma collection Postman para verificar os endpoints da API CampusCarpool, incluindo cenários positivos, negativos e operações que modificam o estado dos recursos. A execução está dividida entre uma sequência automática independente e cenários manuais que requerem pré-condições específicas.

## 2. Artefactos e versão oficial

A **collection oficial da I055** é `CampusCarpool — I055.postman_collection.json`, com **113 pedidos HTTP**. Deve ser utilizada em conjunto com `CampusCarpool — I055 — environment.postman_environment.json` e com este README.

| Ficheiro em `CampusCarpool/docs/I055/` | Identificação | Utilização |
| --- | --- | --- |
| `CampusCarpool — I055.postman_collection.json` | Collection atual — **113 pedidos** | **Versão oficial** |
| `CampusCarpool — I055 — environment.postman_environment.json` | Variáveis e dados de teste associados | **Environment oficial** |
| `I055_LEIA-ME.md` | Instruções, resultados e limitações | **Documentação oficial** |
| `I055_CampusCarpool_Postman_Collection.json` | Collection anterior — **91 pedidos** | **Obsoleta; não utilizar** |
| `I055_CampusCarpool_Postman_Environment.json` | Environment da versão anterior | **Obsoleto; não utilizar** |

Os ficheiros de 91 pedidos e respetivo environment permanecem identificados no histórico/estrutura da branch analisada, mas **não integram a versão de execução definida para esta revisão**. A contagem de 91 corresponde a uma collection anterior; não é um resultado do Runner da collection atual.

**Esclarecimento das contagens:**

| Número | Significado |
| ---: | --- |
| **113** | Pedidos HTTP existentes na collection oficial |
| **94** | Pedidos fora da pasta do Runner automático; incluem cenários de execução manual |
| **19** | Pedidos HTTP na pasta preparada para o Runner automático |
| **24** | Asserções Postman aprovadas na execução dessa pasta automática |
| **91** | Pedidos HTTP da collection antiga, não oficial |

Uma única chamada HTTP pode conter várias asserções; por isso, o número de pedidos executados não coincide necessariamente com o número de testes apresentados pelo Postman.

### Localização no repositório e na documentação do projeto

Os ficheiros técnicos da I055 são versionados no GitHub em `CampusCarpool/docs/I055/`.

A localização `03-artefactos-de-qualidade-de-produto/03.01-backend/03.01.05-postman/` pertence à **estrutura documental oficial da milestone**, que é distinta do repositório GitHub e inclui também atas, revisões e auditorias. A organização dos artefactos nessa pasta integra a consolidação documental da milestone pelo grupo. A localização no GitHub não pretende reproduzir essa estrutura externa.

## 3. Preparação do ambiente

**Requisitos:** Postman, backend CampusCarpool a executar localmente, base de dados de desenvolvimento operacional e dados/contas de teste apropriados.

Iniciar a API na pasta `CampusCarpool`:

```powershell
dotnet run --project .\src\CampusCarpool.Api\CampusCarpool.Api.csproj
```

No Postman:

1. Importar a **collection oficial de 113 pedidos** e o **environment oficial** da tabela anterior.
2. Selecionar o environment e confirmar `baseUrl = http://localhost:5100`.
3. Confirmar que as contas de teste do condutor e do passageiro estão disponíveis e autenticadas; renovar `condutorToken` e `passageiroToken` quando necessário.
4. Antes de cada cenário manual, verificar os identificadores dos recursos e o respetivo estado atual. `partidaFutura` deve corresponder a uma data posterior à execução.
5. Quando um teste alterar ou terminar o estado de um recurso, preparar um recurso novo ou repor o estado necessário antes de executar um cenário que dependa da situação anterior.

As variáveis de apoio incluem `baseUrl`, `emailCondutor`, `emailPassageiro`, `condutorToken`, `passageiroToken`, `viagemId`, `pedidoId`, `participacaoId`, `notificacaoId`, `origem`, `destino` e `partidaFutura`, além das restantes variáveis presentes no environment. Os valores concretos são dados de teste do ambiente de desenvolvimento, não dados de produção.

## 4. Execução automática

Na collection oficial, executar **apenas** a pasta `00 — RUNNER AUTOMÁTICO (19 pedidos) — EXECUTAR ESTA PASTA`.

**Condições utilizadas na execução comunicada:** uma iteração; intervalo de 2000 ms entre pedidos; backend local iniciado; environment correspondente selecionado.

| Resultado da pasta automática | Valor |
| --- | ---: |
| Pedidos HTTP na pasta | **19** |
| Asserções aprovadas | **24** |
| Asserções falhadas | **0** |
| Asserções ignoradas | **0** |
| Erros reportados | **0** |

Estes valores correspondem à **execução local comunicada durante a realização da I055**. Não representam a execução sequencial de todos os 113 pedidos. O ZIP analisado não inclui um relatório exportado ou captura autenticável do Runner; por isso, o resultado é apresentado como registo da execução comunicada, e não como evidência externa anexada.

## 5. Execução dos cenários manuais

Os restantes **94 pedidos** abrangem cenários que podem alterar o estado da mesma entidade ou exigir condições mutuamente incompatíveis. Não estão todos preparados para execução sequencial, numa única passagem, sem reposição dos dados de teste.

| Grupo de cenários | Pré-condição a verificar |
| --- | --- |
| Autenticação e confirmação | Conta no estado esperado; credenciais e token de confirmação válidos quando aplicável |
| Perfil e permissões | Utilizador autenticado e papel adequado ao endpoint |
| Viagens e pesquisa | Viagem existente no estado apropriado; data futura quando exigida; filtros coerentes |
| Pedidos e participações | Viagem elegível; pedido pendente para operações de aceitação/rejeição |
| Cancelamento, conclusão e simulação | Viagem e participação no estado que autoriza a operação |
| Notificações, mensagens e avisos | Recursos existentes, associados ao utilizador correto e com as permissões necessárias |
| Avaliações | Viagem e participantes elegíveis, sem conflito com avaliações já efetuadas |
| Histórico pessoal | Autenticação válida no caso positivo; ausência de autenticação no cenário negativo |
| Cenários negativos | Entrada ou contexto deliberadamente inválido; códigos `400`, `401`, `403`, `404` ou `409` podem ser o resultado esperado |

**Exemplo de dependência:** uma operação anula uma viagem; um cenário posterior tenta pedir participação nessa mesma viagem, exigindo que continue ativa. Sem preparar outra viagem, a segunda execução deixa de cumprir a pré-condição. A resposta obtida nesse contexto não permite concluir, isoladamente, que exista um defeito no endpoint.

Para verificar manualmente cada cenário, deve confirmar-se o estado inicial, executar o pedido com os dados adequados, comparar o código HTTP e o corpo com o resultado esperado e conservar o registo da execução. A aprovação individual comunicada durante os testes não substitui a apresentação dessas evidências, quando exigidas na revisão.

## 6. Resultados e análise da execução integral

Foram comunicados os seguintes resultados:

| Modalidade | Asserções aprovadas | Asserções falhadas | Interpretação |
| --- | ---: | ---: | --- |
| Runner da pasta automática (19 pedidos) | **24** | **0** | Sequência concebida para execução conjunta |
| Runner de toda a collection (113 pedidos) | **105** | **20** | Execução conjunta de cenários automáticos e manuais sem reposição sistemática dos estados |

**A execução integral não é equivalente à execução individual dos cenários manuais nas respetivas pré-condições.** Uma mesma viagem, participação ou pedido pode ser alterado por um teste e deixar de satisfazer as condições de outro. A existência de 20 asserções falhadas na execução integral **não demonstra, por si só, 20 falhas funcionais da API**.

Durante o trabalho da I055, os cenários manuais foram testados individualmente com os dados adequados, segundo os resultados comunicados. No entanto, **o ZIP da branch não contém a exportação detalhada das 20 asserções falhadas**, com pedido, valor esperado, valor obtido e estado dos recursos no instante da execução. Assim, não é possível atribuir documentalmente uma causa específica a cada falha ou considerar a análise individual solicitada como concluída.

Para documentar cada falha, a evidência necessária é: **pedido e asserção; resultado esperado; resultado obtido; estado/pré-condição no momento da execução; causa verificada; resultado da execução individual, quando disponível**. Esta classificação deve basear-se nos resultados guardados do Postman, não numa causa presumida.

Resultados individuais comunicados durante o desenvolvimento incluem: cálculo de rota `200 OK` após correção da integração; criação de recursos `201 Created` em cenários apropriados; simulação `200 OK`; rejeição de participação `204 No Content`; paginação inválida `400 Bad Request` como resultado negativo esperado; marcação de notificação como lida `204 No Content`; envio de mensagem `201 Created`; e consulta de conversa `200 OK`. São registos comunicados, não um anexo de capturas ou exportações.

## 7. Cobertura e alterações verificadas

A estrutura da collection oficial abrange autenticação, perfil, mapas/rotas, viagens, pesquisa, simulação, pedidos, participações, notificações, mensagens, avisos, avaliações, histórico pessoal, push Android, segurança e cenários negativos. Contém ainda os **dois pedidos relativos ao histórico pessoal**, para acesso autenticado e não autenticado.

A presença dos pedidos foi verificada na collection; **a sua existência não equivale a evidência de execução bem-sucedida**. A versão de referência documental é:

- **Branch:** `feature/i055-testes-integracao-postman`.
- **Collection:** `CampusCarpool — I055.postman_collection.json` — 113 pedidos.
- **Environment:** `CampusCarpool — I055 — environment.postman_environment.json`.
- **README:** `I055_LEIA-ME.md` — revisão documental de 29/10/2026.

Não foi possível associar os resultados comunicados a um identificador de commit e a um relatório datado de execução preservados no ZIP. Esses identificadores não são apresentados como confirmados.

### Integração de mapas

Durante os testes, os pedidos relacionados com rotas apresentaram um erro `502 Bad Gateway`, associado à resposta `403` do endpoint externo anteriormente utilizado. O backend foi adaptado para a infraestrutura HeiGIT/OpenRouteService, designadamente geocodificação (`https://api.heigit.org/pelias/v1/search`) e direções (`https://api.heigit.org/openrouteservice/v2/directions/driving-car`). Após a alteração e reinício da API, foi comunicado um resultado `200 OK` no cálculo de rota.

## 8. Conclusão e limitações documentadas

A entrega atual da I055 está identificada pela **collection oficial de 113 pedidos**, pelo environment correspondente e por este README. A versão anterior de 91 pedidos é identificada como obsoleta. A pasta automática, com **19 pedidos e 24 asserções aprovadas**, distingue-se dos cenários manuais dependentes de pré-condições.

A execução integral comunicada registou **105 asserções aprovadas e 20 falhadas**. A incompatibilidade de estados entre cenários explica tecnicamente a necessidade de execução manual independente, mas a atribuição individual das 20 falhas permanece **por comprovar com registos da execução integral**. Também não estão anexadas ao ZIP evidências exportadas do Runner ou identificadores completos de versão da execução.

Os artefactos técnicos estão organizados em `CampusCarpool/docs/I055/` no GitHub; a colocação na pasta documental oficial é uma operação distinta de consolidação da milestone. Estas limitações são expressamente registadas para permitir uma revisão objetiva, sem alterar os resultados comunicados ou atribuir verificações não demonstradas.
