# I044 — Avisos do condutor aos passageiros participantes

Implementação sobre a base da I041 e as regras F005/F006 da Declaração de Âmbito v03.

## Modelo funcional

F005 define o aviso como uma comunicação do condutor aos passageiros participantes. F006 define que os seis tipos de notificação são registados e disponibilizados na aplicação, mas exclui push para avisos.

Consequentemente, a implementação separa:

- `Aviso`: mensagem original associada à viagem e ao condutor;
- `Notificacao`: cópia dirigida a cada passageiro com participação `Aceite`;
- push: não é criado nem enviado para este tipo de notificação.

Avisos e notificações ficam armazenados indefinidamente. A página de notificações consulta os registos pessoais por `CriadaEm` descendente, da mais recente para a mais antiga.

## Endpoints

- `POST /api/v1/viagens/{viagemId}/avisos` — o condutor autenticado cria um aviso e as notificações dos passageiros aceites.
- `GET /api/v1/viagens/{viagemId}/avisos` — o condutor ou um passageiro com participação `Aceite` consulta avisos da viagem.
- `GET /api/v1/notificacoes?pagina=1&tamanhoPagina=20` — consulta paginada das notificações pessoais.
- `PUT /api/v1/notificacoes/{id}/ler` — marca uma notificação própria como lida.
- `PUT /api/v1/notificacoes/ler-todas` — marca todas as notificações próprias como lidas.

Conteúdo vazio ou superior a 100 caracteres é rejeitado. O limite de 100 caracteres é validado no serviço e reforçado no modelo EF. Passageiros pendentes, rejeitados, cancelados e utilizadores sem relação não podem consultar avisos.

O modelo EF foi atualizado com `HasMaxLength(100)` e a migration
`20261019170436_I044_AvisoConteudoMaxLength100` foi gerada. Deve ser aplicada
ao SQL Server juntamente com as restantes migrations.

## Arquitetura

```text
AvisosController -> IAvisoService -> AvisoService -> EF Core
NotificacoesController -> INotificacaoService -> NotificacaoService -> EF Core
```

Os controllers tratam apenas HTTP e identidade. `AvisoService` valida o condutor, cria o aviso, encontra participações `Aceite` e cria as notificações na mesma transação. Isto evita avisos persistidos sem os respetivos destinatários.

A implementação é incremental: os contratos estão na `Application`, mas os serviços concretos continuam na `Infrastructure` porque usam `CampusCarpoolDbContext` e EF Core. Push, SignalR e entrega externa não fazem parte da I044.

## Ficheiros principais

- [AvisosController.cs](../src/CampusCarpool.Api/Controllers/AvisosController.cs)
- [NotificacoesController.cs](../src/CampusCarpool.Api/Controllers/NotificacoesController.cs)
- [IAvisoService.cs](../src/CampusCarpool.Application/Interfaces/IAvisoService.cs)
- [INotificacaoService.cs](../src/CampusCarpool.Application/Interfaces/INotificacaoService.cs)
- [AvisoService.cs](../src/CampusCarpool.Infrastructure/Services/AvisoService.cs)
- [NotificacaoService.cs](../src/CampusCarpool.Infrastructure/Services/NotificacaoService.cs)
- [AvisosIntegrationTests.cs](../tests/CampusCarpool.Tests/Integration/AvisosIntegrationTests.cs)

## Testes

## Testes

Foram adicionados 14 métodos de teste em `AvisosIntegrationTests.cs`, o que resulta em 16 casos de execução (devido à utilização de um método `[Theory]` que corre com 3 variantes de conteúdo inválido).

Cenários cobertos:
1. Condutor cria aviso e notifica apenas passageiros aceites.
2. Outro utilizador não pode criar aviso.
3. Viagem inexistente é rejeitada.
4. Conteúdo inválido é rejeitado (variantes: nula, vazia, apenas espaços).
5. Conteúdo com 100 caracteres é aceite.
6. Conteúdo com 101 caracteres é rejeitado.
7. Passageiro aceite consulta avisos.
8. Passageiro pendente não consulta avisos.
9. Utilizador não relacionado não consulta avisos.
10. Notificações são pessoais e ordenadas.
11. Identidade ausente na consulta de notificações.
12. Paginação de notificações funciona corretamente.
13. Marcar uma notificação própria como lida.
14. Impedir que um utilizador marque a notificação de outro como lida.

Validação focada:
```bash
dotnet test CampusCarpool.slnx --no-restore --filter FullyQualifiedName~AvisosIntegrationTests
```
Resultado: 16 testes aprovados, 0 falhas.

Validação global:
```bash
dotnet test CampusCarpool.slnx --no-restore
```
Resultado: 92 testes aprovados, 0 falhas.

## Critérios da I044

| Critério | Estado |
|---|---|
| Condutor emite avisos para a própria viagem | Cumprido |
| Aviso associado à viagem e ao condutor | Cumprido |
| Passageiros participantes consultam avisos | Cumprido para participações `Aceite` |
| Utilizador não pode emitir em nome de outro condutor | Cumprido |
| Viagens inexistentes e conteúdo inválido tratados | Cumprido |
| Aviso limitado a 100 caracteres | Cumprido |
| Notificações persistidas para passageiros abrangidos | Cumprido |
| Push para avisos | Fora do âmbito, conforme F006 |
| Testes de criação, consulta e autorização | Cumprido |

# Estado

A I044 está funcionalmente implementada:

- avisos associados à viagem e ao condutor;
- autorização de criação e consulta;
- notificações persistidas para passageiros aceites;
- limite de 100 caracteres;
- paginação;
- estado de leitura individual e global;
- migration `20261019170436_I044_AvisoConteudoMaxLength100`;
- sem push para avisos, conforme F006.

Validação:
- 16 testes focados da I044 aprovados (14 métodos);
- 92 testes globais aprovados;
- 0 falhas.

## Melhorias futuras

- testes HTTP end-to-end;
- mover os serviços concretos para `Application` através de repositories;
- melhorar a abstração das transações;
- integração de push apenas para os tipos permitidos pela declaração de âmbito.