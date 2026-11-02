# I059 — Relatório de testes unitários do backend

**Projeto:** CampusCarpool  
**Issue:** I059 — Testes unitários  
**Data do relatório:** 28/10/2026  
**Estado da meta de cobertura:** Atingida (métrica global de linhas)  
**Fonte da evidência:** `coverage.cobertura(10).xml`, gerado pela execução de testes com recolha de cobertura.

## 1. Objetivo

Documentar a evidência de cobertura dos testes do backend CampusCarpool e confirmar se foi atingida a meta global de **pelo menos 80% de cobertura de linhas**. O presente relatório refere-se à atividade de testes unitários da I059, mas o ficheiro Cobertura agrega a execução do conjunto de testes e **não separa o contributo dos testes unitários do contributo dos testes de integração**.

## 2. Ambiente e procedimento de recolha

- Plataforma de desenvolvimento: .NET 10 / ASP.NET Core Web API.
- Framework de testes utilizada no projeto: xUnit.
- Formato de cobertura: Cobertura XML (`XPlat Code Coverage`).
- Configuração de seleção de ficheiros: `coverage.runsettings`.

Comando de referência, a partir da pasta que contém `CampusCarpool.slnx`:

```powershell
dotnet test .\CampusCarpool.slnx -c Release --settings .\coverage.runsettings --collect "XPlat Code Coverage" --results-directory .\artifacts\cobertura-atualizada
```

**Nota:** o XML comprova os valores da recolha de cobertura, mas não inclui por si só a listagem de testes aprovados/falhados; para comprovar individualmente o sucesso de todos os testes, devem ser anexados os resultados do `dotnet test` (por exemplo, log ou TRX).

## 3. Resultados globais registados

| Indicador | Resultado |
|---|---:|
| Linhas analisadas | 1 737 |
| Linhas cobertas | 1 467 |
| Linhas não cobertas | 270 |
| **Cobertura de linhas** | **84,45%** |
| Ramos analisados | 705 |
| Ramos cobertos | 563 |
| Cobertura de ramos | 79,85% |
| Meta estabelecida para linhas | ≥ 80% |
| **Meta global de linhas** | **Atingida** |

A cobertura global excede a meta em **4,45 pontos percentuais**. A cobertura de ramos fica ligeiramente abaixo dos 80%; não deve ser confundida com a métrica de linhas.

## 4. Cobertura por projeto (inclui testes unitários e de integração)

| Projeto | Cobertura de linhas | Cobertura de ramos |
|---|---:|---:|
| CampusCarpool.Api | 82,42% | 80,41% |
| CampusCarpool.Application | 85,99% | 71,59% |
| CampusCarpool.Domain | 100,00% | 100,00% |
| CampusCarpool.Infrastructure | 90,51% | 88,23% |

A análise reporta cobertura nos serviços e na lógica de domínio/aplicação abrangidos pelos testes executados. **Não é possível atribuir estes valores exclusivamente aos testes unitários** com o XML fornecido.

## 5. Evolução da cobertura

| Recolha | Linhas cobertas / válidas | Cobertura de linhas |
|---|---|---:|
| `coverage.cobertura(9).xml` | 1 383 / 1 737 | 79,62% |
| **`coverage.cobertura(10).xml`** | **1 467 / 1 737** | **84,45%** |

Foram cobertas **mais 84 linhas**, sem alteração do total de linhas analisadas.

## 6. Conclusão e evidência de fecho

**A meta global de cobertura de linhas ≥ 80% está cumprida**, de acordo com `coverage.cobertura(10).xml` (84,45%). O relatório é utilizável como evidência quantitativa da I059, em conjunto com o relatório da I060.

**Evidências recomendadas para anexar à issue:**

1. Ficheiro `coverage.cobertura(10).xml`.
2. Resultado da execução de `dotnet test` com contagem de testes aprovados/falhados e sem erros.
3. Referência ao commit/merge request dos testes unitários, para rastreabilidade.

**Limitação da evidência:** o ficheiro de cobertura não comprova isoladamente a aprovação de todos os testes nem identifica qual a percentagem produzida só pelos testes unitários. O fecho integral da I059 deve ainda observar os restantes critérios de aceitação específicos da issue, se existirem.
