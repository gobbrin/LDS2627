# I041 — Aceitação, rejeição e cancelamento de participações

Implementação da gestão de participações sobre a I038 integrada.

## Verificação
`dotnet restore CampusCarpool.slnx`
`dotnet build CampusCarpool.slnx`
`dotnet test CampusCarpool.slnx`

## Endpoints

- `POST /api/v1/participacoes/{id}/aceitar` — apenas o condutor; pedido pendente, viagem ativa/futura e lugar disponível.
- `POST /api/v1/participacoes/{id}/rejeitar` — apenas o condutor; pedido pendente.
- `POST /api/v1/participacoes/{id}/cancelar` — passageiro ou condutor; pedido pendente ou participação aceite.

Uma participação aceite só pode ser cancelada até à hora de partida. Um pedido pendente pode ser cancelado sem limite temporal. Ao concluir uma viagem, os pedidos pendentes são cancelados.

## Estados e capacidade

`Participacao.Estado` usa `EstadoParticipacao` e é persistido como texto para manter compatibilidade com a base existente.

- pedidos pendentes não ocupam lugares;
- aceitar recalcula a capacidade a partir das participações `Aceite` e atualiza `Viagem.LugaresDisponiveis` numa transação `Serializable`;
- cancelar uma participação aceite antes da partida recalcula os lugares depois da remoção;
- rejeitar e cancelar pedidos pendentes não alteram lugares.

A regra de consistência é:

`LugaresDisponiveis = LugaresOferecidos - número de participações Aceite`

Se o valor persistido não corresponder a esta fórmula, a operação devolve `409 Conflict` com `DadosInconsistentes`. O sistema não usa `Math.Min` ou `Math.Max` para esconder dados inválidos.

## Correspondência com a issue

| Critério da I041 | Estado |
|---|---|
| Apenas utilizadores autorizados alteram pedidos | Cumprido: condutor aceita/rejeita; passageiro ou condutor cancela |
| Aceitar pedidos pendentes válidos | Cumprido |
| Rejeitar pedidos pendentes válidos | Cumprido |
| Cancelar apenas em estados permitidos | Cumprido: `Pendente`/`Aceite` para `Cancelada` |
| Impedir aceitação sem capacidade | Cumprido |
| Manter estados e lugares consistentes | Cumprido no fluxo validado; inconsistências são rejeitadas |
| Respostas de erro adequadas | Cumprido: `401`, `403`, `404` e `409` |
| Testes de autorização, capacidade e transições | Cumprido nos testes focados |

### Transições

```text
Pendente -> Aceite
Pendente -> Rejeitada
Pendente -> Cancelada
Aceite   -> Cancelada, apenas antes da partida
```

Quando a viagem é concluída, os pedidos ainda `Pendente` passam automaticamente a `Cancelada`. Uma participação `Aceite` não pode ser cancelada depois da hora de partida.

## Arquitetura

O controller [GestaoParticipacoesController.cs](../src/CampusCarpool.Api/Controllers/GestaoParticipacoesController.cs) depende de [IParticipacaoService.cs](../src/CampusCarpool.Application/Interfaces/IParticipacaoService.cs) e converte resultados da aplicação em respostas HTTP.

A implementação atual está em [ParticipacaoService.cs](../src/CampusCarpool.Infrastructure/Services/ParticipacaoService.cs) porque ainda usa diretamente `CampusCarpoolDbContext`, EF Core e transações SQL. Esta é precisamente a fronteira incompleta da Clean Architecture: o contrato está em `Application`, mas a regra de negócio ainda conhece detalhes de persistência em `Infrastructure`.

Esta é uma refatoração incremental, não uma Clean Architecture completa. A fronteira já está preparada para uma evolução futura: repositories e uma abstração de transação poderiam permitir mover as regras para `Application` sem o controller voltar a conhecer a base de dados.

O email segue a mesma ideia. O contrato está em [IServicoEmail.cs](../src/CampusCarpool.Application/Interfaces/IServicoEmail.cs) e o detalhe Brevo em [ServicoEmail.cs](../src/CampusCarpool.Infrastructure/Integrations/ServicoEmail.cs):

```text
AutenticacaoController -> IServicoEmail -> ServicoEmail/Brevo
```

Os testes usam um fake de `IServicoEmail`, por isso não dependem de Internet ou Brevo.

## O que significa “está feita”

Para os critérios funcionais propostos, a issue está implementada: os endpoints existem, as transições são validadas, a autorização e a capacidade são verificadas, e existem testes focados.

Não significa que todo o trabalho de produção esteja concluído. Permanecem explicitamente fora desta entrega a concorrência validada num SQL Server real, testes HTTP end-to-end, notificações e a migração completa das regras para Application.

## Testes

Foram adicionados 11 testes focados em `ParticipacoesGestaoIntegrationTests.cs`:

1. Aceitação válida, alteração do estado e redução de lugar.
2. Rejeição válida sem alteração de lugares.
3. Cancelamento de pedido pendente depois da partida.
4. Cancelamento de participação aceite antes da partida.
5. Rejeição de cancelamento aceite depois da partida.
6. Aceitação sem capacidade.
7. Capacidade inconsistente sem correção silenciosa.
8. Utilizador não autorizado.
9. Passageiro impedido de aceitar ou rejeitar.
10. Transições inválidas.
11. Cancelamento automático de pendentes ao concluir a viagem.

O número `11` é o filtro da classe de testes da I041. O número `76` é o total de testes de todo o projeto, incluindo I038, autenticação, viagens, pesquisa, persistência e outras funcionalidades.

Validação executada:

`dotnet test CampusCarpool.slnx --no-restore` — 76 testes aprovados.

Para repetir os testes apenas da I041, executar na pasta que contém a solução:

```bash
dotnet test CampusCarpool.slnx --no-restore --filter FullyQualifiedName~ParticipacoesGestaoIntegrationTests
```

## Avisos de dependências

Os avisos `NU1902` e `NU1903` referem-se a dependências transitivas:

- `Microsoft.OpenApi 2.0.0` — vulnerabilidade de severidade alta;
- `System.Security.Cryptography.Pkcs 9.0.11` — vulnerabilidades de severidade moderada e alta.

Não impedem a compilação nem os testes, mas devem ser tratados numa tarefa de manutenção de dependências. Não foram atualizadas nesta issue para evitar alterações de versão não relacionadas à gestão de participações.

Os avisos são de segurança de dependências, não erros de código. A correção deve ser feita com atualização controlada, nova execução de `dotnet restore`, `dotnet build` e `dotnet test`, e revisão de compatibilidade.

## Observações

As transações `Serializable` dependem de provider relacional; SQL Server é o provider alvo. A concorrência deve ainda ser validada num ambiente SQL Server real. Notificações não fazem parte desta entrega; deverão ser publicadas depois de a alteração transacional ser confirmada.

## Estado da issue

A implementação cumpre os critérios funcionais propostos e tem testes automatizados focados. Para declarar a issue totalmente pronta para produção, ainda faltam validação de concorrência real com SQL Server, testes HTTP end-to-end e integração futura de notificações. Esses pontos são limitações de validação e evolução, não falhas dos cenários funcionais atualmente cobertos.
