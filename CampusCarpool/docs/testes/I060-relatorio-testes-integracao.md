# I060 — Registo dos resultados dos testes de integração do backend

**Projeto:** CampusCarpool  
**Milestone:** M3  
**Sprint:** 06  
**Issue:** I060 — Registar os resultados dos testes de integração do backend  
**Data da atualização:** 02/11/2026  
**Estado dos testes:** Aprovados (conforme confirmação do executor)  
**Estado da meta de cobertura global de linhas:** Cumprida

## 1. Objetivo

Registar os resultados comunicados da execução dos testes de integração do backend e apresentar **apenas a medição de cobertura atual**. A cobertura corresponde à suite instrumentada global, podendo incluir testes unitários e de integração; não representa uma percentagem exclusiva dos testes de integração.

## 2. Ambiente e metodologia

A solução utiliza **.NET 10**, testes automatizados com **xUnit** e recolha de cobertura pelo **XPlat Code Coverage**, em formato Cobertura XML. A medição apresentada abrange `CampusCarpool.Api`, `CampusCarpool.Application`, `CampusCarpool.Domain` e `CampusCarpool.Infrastructure`.

Comando de referência para recolha:

```powershell
dotnet test .\CampusCarpool.slnx -c Release --settings .\coverage.runsettings --collect "XPlat Code Coverage" --results-directory .\artifacts\cobertura-atualizada
```

A fonte dos valores apresentados é o conteúdo XML Cobertura fornecido pelo executor, com os atributos globais `line-rate="0.8458"` e `branch-rate="0.7994"`.

## 3. Cobertura atual — resultados globais

| Indicador | Resultado |
|---|---:|
| Linhas analisadas | 1 739 |
| Linhas cobertas | 1 471 |
| Linhas não cobertas | 268 |
| **Cobertura global de linhas** | **84,58%** |
| Ramos analisados | 693 |
| Ramos cobertos | 554 |
| Ramos não cobertos | 139 |
| **Cobertura global de ramos** | **79,94%** |
| Meta mínima de cobertura de linhas | 80% |
| **Estado da meta** | **Cumprida** |

## 4. Cobertura atual por projeto

| Projeto | Cobertura de linhas | Cobertura de ramos |
|---|---:|---:|
| `CampusCarpool.Api` | 82,43% | 80,41% |
| `CampusCarpool.Application` | 86,89% | 73,17% |
| `CampusCarpool.Domain` | 100,00% | 100,00% |
| `CampusCarpool.Infrastructure` | 90,51% | 88,23% |

Os valores apresentados são taxas reportadas pelo XML Cobertura. A cobertura de ramos da camada `Application` é inferior à das restantes camadas e constitui uma oportunidade de reforço dos cenários de decisão.

## 5. Registo dos resultados dos testes de integração

O executor confirmou que **os testes passaram**. A tabela organiza os grupos de ficheiros de testes incluídos na entrega da I060. A confirmação global de sucesso não permite extrair, por si só, a quantidade de testes executados em cada grupo.

| Grupo de cenários / ficheiro | Resultado comunicado | N.º de testes |
|---|---|---|
| `CoberturaMensagensCenariosAdicionaisTests.cs` — cenários adicionais de mensagens | Aprovados, conforme confirmação do executor | Não discriminado |
| `CoberturaRepositoriosMensagensTests.cs` — integração do repositório de mensagens | Aprovados, conforme confirmação do executor | Não discriminado |
| `RepositorioParticipacoesCoberturaTests.cs` — integração do repositório de participações | Aprovados, conforme confirmação do executor | Não discriminado |
| **Resultado global comunicado** | **Todos os testes passaram** | **Não indicado** |

**Evidência de execução:** o resultado positivo foi confirmado pelo executor; não foram fornecidos nesta atualização os contadores individuais nem o ficheiro TRX/log. O XML Cobertura regista execução de código, mas não os resultados aprovados/falhados de cada teste.

## 6. Conclusão

Segundo a confirmação do executor, **os testes de integração executados foram aprovados**. A medição de cobertura atual da suite do backend apresenta **84,58% de linhas** e **79,94% de ramos**, cumprindo a meta de, pelo menos, **80% de cobertura global de linhas**.

Para rastreabilidade na revisão da I060, devem ser anexados à Issue o XML Cobertura original e, caso solicitado pelo revisor, o TRX/log da execução e a referência ao commit ou Pull Request da versão testada. Estes elementos não são substituídos pelo presente resumo.
