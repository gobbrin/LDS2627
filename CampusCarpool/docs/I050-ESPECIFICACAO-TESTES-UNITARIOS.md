# I050 — Especificação dos testes unitários do backend

**Projeto:** CampusCarpool · **Framework:** .NET 10 / xUnit  · **Data:** 28/10/2026

> Documento de especificação para revisão. A associação dos grupos F001–F009 abaixo é uma organização operacional inferida dos controllers e das issues I051/I052; deve ser confrontada com a especificação oficial de requisitos antes de aprovação. Não constitui evidência de execução.

## 1. Objetivo

Especificar casos de teste unitário para F001–F009, incluindo comportamento normal, limites, entradas inválidas, erros de dependências e autorização. Distinguir testes existentes de casos planeados e fornecer rastreabilidade para implementação nas issues I051 e I052.

## 2. Critérios e estratégia

- Isolar a unidade sob teste (serviço, validador, regra de domínio ou controller com dependências substituídas).
- Usar xUnit `[Fact]` para casos isolados e `[Theory]`/`[InlineData]` para fronteiras e equivalências.
- Substituir base de dados, relógio, serviços de email, mapas, notificações e autenticação por *fakes*, *stubs* ou *mocks*, conforme o contrato real.
- Verificar resultado, código de erro e efeitos colaterais: gravação, notificações, alteração de capacidade e ausência de escrita quando a operação é rejeitada.
- Não confundir testes unitários com os testes de integração especificados na I053 e implementados na I054.
- Não afirmar que os cenários passaram sem `dotnet test` e evidência recolhida.

## 3. Inventário real de testes unitários no ZIP de código recebido

| Ficheiro | Casos `[Fact]`/`[Theory]` declarados | Observação |
|---|---:|---|
| `AutenticacaoServiceTests.cs` | 0 | Ficheiro vazio; sem testes |
| `CalculadoraDeCustoAdditionalTests.cs` | 3 | Testes definidos; execução não verificada |
| `CalculadoraDeCustoTests.cs` | 2 | Testes definidos; execução não verificada |
| `EmailSenderTests.cs` | 4 | Testes definidos; execução não verificada |
| `I051ValidacoesControllersTests.cs` | 7 | Testes definidos; execução não verificada |
| `InfrastructureStructureTests.cs` | 1 | Testes definidos; execução não verificada |
| `MapasServiceTests.cs` | 9 | Testes definidos; execução não verificada |
| `OrdenadorPontosRotaTests.cs` | 4 | Testes definidos; execução não verificada |
| `StarterTests.cs` | 1 | Testes definidos; execução não verificada |
| `ViagemServiceTests.cs` | 0 | Ficheiro vazio; sem testes |

**Limite do inventário:** corresponde apenas ao código-fonte extraído do ZIP de 28/10/2026. Os ZIPs incrementais das I051 e I052 entregues separadamente não foram considerados como integrados no projeto de origem. O número de casos parametrizados executados pode exceder o número de métodos `[Theory]`.

## 4. Matriz de especificação (I050)

**Legenda:** `Planeado` = caso especificado sem correspondência unitária confirmada; `Parcial` = existe um teste relacionado no código-base, mas não é evidência de cobertura completa. **Pré-condição comum:** dependências externas isoladas e dados sintéticos válidos, salvo indicação contrária.

| ID | Grupo funcional | Cenário / entrada | Resultado esperado | Estado |
|---|---|---|---|---|
| UT-001 | F001 | registo válido com email académico | Operação aceite e estado/resultado coerente com a regra de negócio | Parcial |
| UT-002 | F001 | rejeição de email não académico | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-003 | F001 | rejeição de nome vazio | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-004 | F001 | rejeição de palavra-passe curta | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-005 | F001 | rejeição de registo duplicado | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-006 | F001 | emissão e validade do token de confirmação | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-007 | F001 | confirmação com token válido | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-008 | F001 | confirmação com token vazio ou inválido | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-009 | F001 | reenviar confirmação sem divulgar existência de conta | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-010 | F001 | login válido com conta confirmada | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-011 | F001 | login com credenciais inválidas | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-012 | F001 | login com conta não confirmada | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-013 | F002 | criação válida e associação ao condutor | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-014 | F002 | rejeição de utilizador não autenticado | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Parcial |
| UT-015 | F002 | origem e destino iguais | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-016 | F002 | lugares inválidos | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-017 | F002 | data de partida passada | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-018 | F002 | consulta de viagem existente | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-019 | F002 | consulta de viagem inexistente | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-020 | F002 | edição por condutor | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-021 | F002 | edição por terceiro não autorizado | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-022 | F002 | indisponibilização pelo condutor | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-023 | F002 | conclusão de viagem elegível | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-024 | F002 | transição de estado inválida | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-025 | F003 | pesquisa sem filtros | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-026 | F003 | filtro de origem | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-027 | F003 | filtro de destino | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-028 | F003 | filtros combinados | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-029 | F003 | filtro de data e hora | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-030 | F003 | paginação válida | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-031 | F003 | página zero | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Parcial |
| UT-032 | F003 | tamanho zero | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Parcial |
| UT-033 | F003 | tamanho superior ao limite | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Parcial |
| UT-034 | F003 | ordenação e consistência dos resultados | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-035 | F004 | criação de pedido válido | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-036 | F004 | viagem inexistente | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-037 | F004 | pedido duplicado | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-038 | F004 | condutor pedir lugar na própria viagem | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-039 | F004 | pontos de recolha/destino inválidos | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-040 | F004 | pedido para viagem indisponível | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-041 | F004 | consulta de pedidos do utilizador | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-042 | F004 | autorização de acesso a pedido | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-043 | F005 | aceitação de pedido pendente | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-044 | F005 | rejeição de pedido pendente | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-045 | F005 | impedir aceitação por terceiro | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-046 | F005 | impedir aceitação sem lugares | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-047 | F005 | impedir aceitação repetida | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-048 | F005 | cancelamento de pedido pendente | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-049 | F005 | cancelamento de participação aceite | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-050 | F005 | reposição de lugares após cancelamento | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-051 | F005 | notificação do resultado ao passageiro | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-052 | F006 | geocodificação válida | Operação aceite e estado/resultado coerente com a regra de negócio | Parcial |
| UT-053 | F006 | geocodificação sem resultados | Operação aceite e estado/resultado coerente com a regra de negócio | Parcial |
| UT-054 | F006 | falha do serviço externo de mapas | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Parcial |
| UT-055 | F006 | cálculo de distância e duração | Operação aceite e estado/resultado coerente com a regra de negócio | Parcial |
| UT-056 | F006 | rejeição de origem vazia | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-057 | F006 | rejeição de destino vazio | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-058 | F006 | simulação com pontos válidos | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-059 | F006 | simulação com pontos iguais | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-060 | F006 | otimização da ordem de recolhas | Operação aceite e estado/resultado coerente com a regra de negócio | Parcial |
| UT-061 | F006 | cálculo de custo e arredondamento | Operação aceite e estado/resultado coerente com a regra de negócio | Parcial |
| UT-062 | F006 | tratamento de chave API em falta | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-063 | F007 | criação de notificação | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-064 | F007 | consulta de notificações do próprio utilizador | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-065 | F007 | impedir leitura de notificações alheias | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-066 | F007 | marcar notificação como lida | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-067 | F007 | paginação de notificações | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-068 | F007 | envio de mensagem válida | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-069 | F007 | rejeição de mensagem vazia | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-070 | F007 | rejeição de mensagem demasiado longa | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-071 | F007 | impedir envio para si próprio | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-072 | F007 | impedir acesso a conversas de terceiros | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-073 | F008 | consulta de perfil autenticado | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-074 | F008 | rejeição de consulta sem autenticação | Resultado filtrado e dados autorizados; sem exposição indevida | Parcial |
| UT-075 | F008 | atualização válida de nome/curso/instituição | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-076 | F008 | rejeição de nome vazio | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-077 | F008 | rejeição de atualização sem autenticação | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Parcial |
| UT-078 | F008 | não exposição de dados sensíveis | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-079 | F009 | criação de aviso válido | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-080 | F009 | validação de filtros de aviso | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-081 | F009 | consulta de avisos do utilizador | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-082 | F009 | impedir acesso a avisos alheios | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-083 | F009 | histórico de viagens concluídas | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-084 | F009 | histórico sem resultados | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |
| UT-085 | F009 | avaliação válida após viagem concluída | Operação aceite e estado/resultado coerente com a regra de negócio | Planeado |
| UT-086 | F009 | rejeição de classificação fora de 1 a 5 | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-087 | F009 | rejeição de autoavaliação | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-088 | F009 | impedir avaliação duplicada | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-089 | F009 | impedir avaliação sem participação | Operação recusada ou erro tratado, sem efeitos colaterais indevidos | Planeado |
| UT-090 | F009 | média e consulta de avaliações sem expor avaliador | Resultado filtrado e dados autorizados; sem exposição indevida | Planeado |

**Total especificado:** 90 cenários. As associações assinaladas como `Parcial` são apenas indícios de testes relacionados, não equivalência demonstrada entre caso e método.

## 5. Rastreabilidade para I051 e I052

| Issue de implementação | Grupos a implementar | Resultado a entregar |
|---|---|---|
| I051 | F001, F002, F003 e F008 | Testes unitários de autenticação, viagens, pesquisa e perfil |
| I052 | F004, F005, F006, F007 e F009 | Testes unitários de pedidos, participações, mapas/rotas, comunicação e funcionalidades adicionais |

A matriz inclui casos associados a controllers e a serviços efetivamente presentes no código, mas os ficheiros `AutenticacaoService.cs`, `ViagemService.cs`, `AvaliacoesController.cs` e `HistoricoController.cs` estão vazios na versão analisada. **Não é possível considerar testes de serviços vazios como implementados.** As funcionalidades correspondentes devem ser confirmadas no código mais recente antes de implementar os casos planeados.

## 6. Critérios de aceitação da I050

- [x] Inventário dos testes unitários existentes no código fornecido.
- [x] Casos positivos, negativos e de fronteira especificados para todos os grupos F001–F009.
- [x] Identificadores estáveis UT-001 em diante e resultado esperado por caso.
- [x] Separação entre existente/parcial e planeado.
- [x] Encaminhamento dos grupos para I051/I052.
- [ ] Validar o mapeamento com os requisitos RF e casos de uso oficiais.
- [ ] Rever e aprovar a matriz pela equipa.
- [ ] Associar cada UT ao método de teste concreto após implementação.

## 7. Execução e evidências (fora do âmbito documental)

A implementação e a execução pertencem às I051/I052. Com o projeto atualizado, executar:

```bash
dotnet test tests/CampusCarpool.Tests/CampusCarpool.Tests.csproj --filter FullyQualifiedName~Unit
```

Guardar relatório de execução, falhas, versão do commit e evidência de cobertura. O presente documento não certifica aprovação ou execução.
