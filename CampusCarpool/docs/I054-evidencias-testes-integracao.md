# I054 — Evidências dos Testes de Integração

**Projeto:** CampusCarpool
**Data da execução:** 28/10/2026
**Branch:** fix/i054-testes-integracao
**Especificação de referência:** I053

## 1. Objetivo

Implementar e executar testes de integração para validar os
principais fluxos do backend do CampusCarpool, de acordo com
os cenários definidos na I053.

## 2. Ambiente e ferramentas

- .NET 10
- xUnit
- Entity Framework Core
- SQLite em memória
- SQL Server LocalDB para testes de concorrência
- ASP.NET Core e serviços externos simulados
- Coverlet / Cobertura

## 3. Resultado da execução

| Indicador | Resultado |
|---|---|
| Testes executados | 315 |
| Testes aprovados | 315 |
| Testes falhados | 0 |
| Testes ignorados | 0 |
| Cobertura de linhas | 34,00% |
| Cobertura de decisões | 62,86% |

Os 315 testes correspondem à suite completa e incluem diferentes
níveis de teste, não exclusivamente testes de integração.

## 4. Evidências de cenários implementados

| Cenário I053 | Evidência |
|---|---|
| IT-001 — Registo válido | AutenticacaoFluxoIntegrationTests |
| IT-003 — Confirmação válida | AutenticacaoFluxoIntegrationTests |
| IT-004 — Login válido | AutenticacaoFluxoIntegrationTests |
| IT-008 — Criação válida de viagem | CriacaoViagemIntegrationTests |
| IT-023 — Concorrência | ConcorrenciaSqlServerIntegrationTests |
| IT-038 — Notificações push | ParticipacoesGestaoIntegrationTests |
| IT-039 — Dispositivos ativos | DispositivosAtivosIntegrationTests |
| IT-040 — Atomicidade | FalhaTransacaoIntegrationTests |

Existem também testes de integração para avaliações,
histórico e endpoints de dispositivos.

A rastreabilidade completa dos cenários IT-001 a IT-040
deve ser verificada contra a especificação I053.

## 5. Evidência de execução

Comando utilizado:

dotnet test .\tests\CampusCarpool.Tests\CampusCarpool.Tests.csproj --no-restore --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal"

Resultado da execução:

Passed: 315
Failed: 0
Skipped: 0

Relatório gerado:
coverage.cobertura.xml

## 6. Limitações e observações

- A cobertura global inclui código que pode não pertencer
  diretamente à lógica funcional testada.
- Os serviços externos são simulados para garantir a
  repetibilidade dos testes.
- O teste de concorrência utiliza SQL Server LocalDB.
- O teste de atomicidade utiliza uma transação relacional SQLite.
- Uma falha no envio push não interrompe a operação principal,
  mas a implementação atual não regista essa falha em log.
- Foram identificados avisos de segurança NU1902 e NU1903
  em dependências do projeto.
- Os testes de controlador executados diretamente não substituem
  integralmente testes HTTP de ponta a ponta.

## 7. Conclusão

A suite completa foi executada com sucesso, sem testes falhados.
Foram adicionadas evidências para os fluxos de autenticação,
criação de viagens, avaliações, histórico, dispositivos,
concorrência, notificações e atomicidade.

A conformidade integral com os 40 cenários da I053 depende
da validação da matriz de rastreabilidade.

## 8. Rastreabilidade adicional identificada

| Cenario | Classe de teste | Estado |
|---|---|---|
| IT-001, IT-003, IT-004 | AutenticacaoFluxoIntegrationTests | Aprovado |
| IT-008 | CriacaoViagemIntegrationTests | Aprovado |
| IT-023 | ConcorrenciaSqlServerIntegrationTests | Aprovado |
| IT-026 | I054IntegridadeAdicionalIntegrationTests | Aprovado |
| IT-033 | HistoricoHttpIntegrationTests | Aprovado |
| IT-034, IT-035 | AvaliacoesHttpIntegrationTests | Aprovado |
| IT-036 | I054IntegridadeAdicionalIntegrationTests | Aprovado |
| IT-038 | ParticipacoesGestaoIntegrationTests | Aprovado |
| IT-039 | DispositivosAtivosIntegrationTests | Aprovado |
| IT-040 | FalhaTransacaoIntegrationTests | Aprovado |

Os estados indicados referem-se a testes aprovados na
execucao global, nao a conformidade integral com a I053.

Existem outros testes com nomes funcionais que podem
corresponder aos restantes cenarios da especificacao.

A matriz integral requer validacao dos resultados esperados
na I053, incluindo o nivel de teste exigido.
