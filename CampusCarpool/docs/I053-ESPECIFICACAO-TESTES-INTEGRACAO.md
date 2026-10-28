# I053 — Especificação dos testes de integração do backend

**Projeto:** CampusCarpool  
**Âmbito:** API ASP.NET Core / .NET 10, camada de aplicação, persistência e integrações externas  
**Base de análise:** código-fonte do ZIP CampusCarpool de 28-10-2026  
**Estado:** especificação proposta; execução e evidências pertencem à I054

## 1. Objetivo e critérios de aceitação

Validar a comunicação entre endpoints HTTP, autenticação, serviços, repositórios e persistência, incluindo cenários de sucesso, falha, autorização e integridade dos dados. Cada caso deverá ter um identificador estável, dados de entrada, resultado esperado e evidência de execução. Não confundir testes unitários isolados com testes de integração.

A I053 considera-se documentalmente concluída quando a equipa revê e aprova esta matriz, confirma os contratos HTTP e as regras de negócio e atribui os casos à I054. A aprovação e os resultados de execução não são presumidos.

## 2. Ambiente e estratégia

- Executar `dotnet test` no projeto `tests/CampusCarpool.Tests`, com xUnit e a configuração de testes já existente.
- Para testar endpoints, utilizar uma aplicação de teste ASP.NET Core (por exemplo `WebApplicationFactory`), quando compatível com a arquitetura atual.
- Para persistência, usar base de dados isolada por execução, com migrações/esquema apropriados. Não substituir SQLite/PostgreSQL por EF Core InMemory em casos que verifiquem constraints, transações ou comportamento SQL.
- Para Firebase, email e serviços de mapas, usar substitutos controlados nos testes de integração locais; executar testes reais de fornecedor separadamente, com credenciais de teste, quando necessário.
- Criar utilizadores, viagens e pedidos por teste ou fixture; eliminar os dados no fim. Evitar dependência da ordem dos testes.
- Asserções devem validar código HTTP, payload, alterações persistidas e ausência de alterações em casos rejeitados.
- Os códigos HTTP concretos e os formatos de erro devem ser confirmados com os controllers/DTOs antes da implementação da I054. Não assumir `404`/`409`/`422` quando o contrato efetivo devolver outro código.

## 3. Dados de teste

**U1:** condutor autenticado; **U2:** passageiro autenticado; **U3:** terceiro sem relação com a viagem.  
**V1:** viagem publicada de U1 com lugares; **V2:** viagem sem lugares; **P1:** pedido pendente de U2 para V1.  
Usar IDs únicos, emails sintéticos, tokens de teste, timestamps controlados e dados geográficos válidos. Nenhuma credencial de produção deve entrar no repositório.

## 4. Matriz de casos de integração

| ID | Área / fluxo | Preparação e ação | Resultado esperado |
|---|---|---|---|
| IT-001 | Autenticação — registo | Registar U1 com dados válidos | Utilizador persistido; resposta conforme contrato; estado de confirmação coerente |
| IT-002 | Autenticação — duplicado | Registar email já utilizado | Pedido rejeitado; sem segundo utilizador |
| IT-003 | Autenticação — confirmação | Registar, obter código controlado e confirmar | Conta confirmada; confirmação persistida |
| IT-004 | Autenticação — login | Autenticar utilizador válido | Token válido e acesso a endpoint protegido |
| IT-005 | Autenticação — credenciais inválidas | Login com palavra-passe errada | Acesso recusado; sem emissão de sessão válida |
| IT-006 | Autorização | Chamar endpoint protegido sem token e com token inválido | Acesso recusado conforme política da API |
| IT-007 | Perfil | Consultar e atualizar `utilizadores/me` como U1 | Apenas perfil U1 alterado; leitura reflete atualização |
| IT-008 | Viagens — criação | U1 cria viagem válida | Viagem persistida e associada a U1 |
| IT-009 | Viagens — validação | Criar viagem com dados inválidos | Erro de validação; nenhuma viagem criada |
| IT-010 | Viagens — detalhe | Consultar viagem existente e ID inexistente | Dados corretos; ausência tratada pelo contrato |
| IT-011 | Viagens — edição | U1 edita V1 | Dados persistidos e visíveis em consulta posterior |
| IT-012 | Viagens — autorização | U2 tenta alterar V1 | Operação recusada; V1 inalterada |
| IT-013 | Viagens — estados | Indisponibilizar e concluir viagens elegíveis | Transições e restrições respeitadas; estado persistido |
| IT-014 | Pesquisa | Pesquisar viagens com filtros válidos | Apenas resultados compatíveis; paginação consistente |
| IT-015 | Pesquisa — limites | Consultar paginação/filtros inválidos | Validação conforme contrato; sem erro 500 inesperado |
| IT-016 | Pedidos | U2 solicita participação em V1 | Pedido associado a U2/V1 e persistido |
| IT-017 | Pedidos — duplicado | U2 repete pedido não permitido | Sem duplicação; erro de negócio conforme contrato |
| IT-018 | Pedidos — lotação | Pedir lugar em V2 sem capacidade | Rejeição consistente; capacidade inalterada |
| IT-019 | Participações — aceitar | U1 aceita P1 | Estado aceite; lugares atualizados uma única vez |
| IT-020 | Participações — rejeitar | U1 rejeita pedido pendente | Estado rejeitado; capacidade coerente |
| IT-021 | Participações — cancelar | U2 cancela pedido elegível | Estado e lugares consistentes com a regra aplicável |
| IT-022 | Participações — permissão | U3 tenta aceitar/rejeitar P1 | Recusado; pedido e lugares inalterados |
| IT-023 | Participações — concorrência | Duas aceitações disputam último lugar | Nunca excede capacidade; transação consistente |
| IT-024 | Notificações | Criar/aceitar/rejeitar pedido | Notificações internas para destinatário correto e tipo correto |
| IT-025 | Notificações — leitura | Marcar uma/todas como lidas | Estado persistido; outros utilizadores não afetados |
| IT-026 | Mensagens | U1 envia mensagem válida a U2 e consulta conversa | Mensagem persistida; conversa acessível aos participantes autorizados |
| IT-027 | Mensagens — segurança | U3 tenta aceder a conversa alheia | Acesso indevido recusado; dados não divulgados |
| IT-028 | Mensagens — validação | Enviar mensagem vazia/inválida | Rejeitada sem persistência |
| IT-029 | Rotas | Calcular rota com serviço de mapas substituído | Resposta coerente com serviço controlado; dados processados corretamente |
| IT-030 | Rotas — indisponibilidade | Serviço de mapas devolve erro/timeout | Falha tratada conforme contrato; sem dados inconsistentes |
| IT-031 | Simulações | Simular viagem existente com parâmetros válidos | Cálculo/resposta consistente com regras de negócio |
| IT-032 | Avisos | Criar e consultar avisos autorizados | Persistência e visibilidade de acordo com permissões |
| IT-033 | Histórico | Consultar histórico do utilizador autenticado | Apenas dados pessoais autorizados; ordem/filtros corretos |
| IT-034 | Avaliações | Submeter avaliação elegível | Avaliação persistida e ligada às entidades corretas |
| IT-035 | Avaliações — restrições | Avaliação duplicada/sem elegibilidade | Rejeição sem duplicação; integridade mantida |
| IT-036 | Integridade referencial | Eliminar/alterar entidades relacionadas em base de teste | FKs e regras de cascata/restrição respeitadas |
| IT-037 | Push Android — registo | Registar token de dispositivo autenticado, se I047 integrada | Token ligado ao utilizador; duplicados tratados |
| IT-038 | Push Android — evento | Aceitar/rejeitar pedido com emissor push substituído | Emissão correta para destinatário; pedido não depende de sucesso do push |
| IT-039 | Push Android — desativação | Desativar token e gerar novo evento | Token desativado não recebe nova tentativa de envio |
| IT-040 | Erros e isolamento | Forçar falha de repositório numa operação composta | Erro controlado; sem persistência parcial indevida |

**Nota de âmbito:** IT-037 a IT-039 são condicionais à incorporação efetiva da I047 na versão que será testada; não constam necessariamente do ZIP base de 28-10-2026.

## 5. Rastreabilidade com os testes já presentes

Na versão analisada existem ficheiros em `tests/CampusCarpool.Tests/Integration/`, entre outros:

- `ApiIntegrationTests.cs`, `EndpointsAdditionalTests.cs`, `PersistenciaIntegrationTests.cs` e `IntegridadeRelacionalAdditionalTests.cs`;
- `PedidosParticipacaoIntegrationTests.cs`, `ParticipacoesGestaoIntegrationTests.cs` e `RotasParticipacoesIntegrationTests.cs`;
- `MensagensControllerTests.cs`, `MensagensNotificacoesIntegrationTests.cs` e `ViagensNotificacoesIntegrationTests.cs`;
- `AvisosIntegrationTests.cs`, `SimulacoesIntegrationTests.cs`, `ViagensEstimativaIntegrationTests.cs` e `EstimativaCustoTests.cs`.

Estes nomes indicam áreas de cobertura potencial, **não demonstram** que todos os cenários acima já estão implementados ou a passar. Na I054, mapear cada método `[Fact]`/`[Theory]` para o respetivo IT-ID, assinalando `Existente`, `A adaptar` ou `Novo` após inspeção do comportamento real.

## 6. Critérios de execução para I054

1. Confirmar routes, DTOs, códigos HTTP e regras efetivas antes de codificar cada caso.
2. Implementar fixtures reproduzíveis e isolamento de dados.
3. Cobrir caminhos positivos, negativos, autorização, persistência e concorrência.
4. Executar `dotnet test` e guardar resultados com data, commit, ambiente, total de testes, aprovados, falhados e ignorados.
5. Registar defeitos com passos de reprodução, expectativa, resultado real e evidência.
6. Não declarar cobertura integral nem aprovação sem evidências de execução.

## 7. Modelo de registo de evidências

| IT-ID | Método de teste | Estado | Resultado esperado | Resultado observado | Evidência / log | Commit |
|---|---|---|---|---|---|---|
| IT-001 | A preencher na I054 | Não executado | Conforme matriz | — | — | — |

**Estados sugeridos:** Não implementado, Implementado/não executado, Aprovado, Falhado, Bloqueado.

## 8. Comando de referência

```bash
dotnet test tests/CampusCarpool.Tests/CampusCarpool.Tests.csproj --filter FullyQualifiedName~Integration --logger "trx;LogFileName=I054-integration.trx"
```

O filtro por `Integration` depende dos nomes e namespaces usados nos testes. A I053 não exige a execução deste comando; o registo de resultados é trabalho da I054.
