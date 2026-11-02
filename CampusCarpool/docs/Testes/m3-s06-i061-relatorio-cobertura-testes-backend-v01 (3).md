# Relatório de cobertura dos testes do backend

**Projeto:** CampusCarpool  
**Milestone:** M3  
**Sprint:** 06  
**Issue:** I061 — Elaborar o relatório de cobertura dos testes do backend  
**Versão:** v01
**Data:** 02/11/2026

## 1. Introdução e objetivo

A realização de testes é uma parte importante do desenvolvimento do CampusCarpool, uma vez que permite verificar o comportamento das funcionalidades do backend e detetar problemas antes da sua utilização. No entanto, a existência de testes, por si só, não indica que todas as partes relevantes da aplicação sejam exercitadas. Para complementar a execução dos testes, é utilizada a medição de cobertura de código.

Este relatório apresenta os resultados de cobertura disponíveis para o backend, analisa a distribuição da cobertura pelas diferentes camadas da aplicação e identifica áreas onde o conjunto de testes pode ser reforçado. São considerados dois indicadores: a **cobertura de linhas**, que indica a proporção de linhas de código executadas durante os testes, e a **cobertura de ramos**, que avalia os caminhos percorridos em estruturas de decisão, como condições `if` e `switch`.

A medição apresentada abrange o código instrumentado dos projetos `CampusCarpool.Api`, `CampusCarpool.Application`, `CampusCarpool.Domain` e `CampusCarpool.Infrastructure`. Como os resultados resultam da execução da suite instrumentada, a cobertura não deve ser atribuída exclusivamente aos testes unitários: a execução pode incluir também testes de integração.

## 2. Ambiente e metodologia

O backend foi desenvolvido em **.NET 10** e utiliza **xUnit** para a implementação e execução dos testes automatizados. A cobertura é recolhida através de **XPlat Code Coverage**, com exportação em formato **Cobertura XML**. O ficheiro `coverage.runsettings` define a configuração da recolha, incluindo exclusões de código gerado, como as pastas de migrations e `obj`.

O procedimento consiste em executar a solução com recolha de cobertura ativa, consultar o resultado dos testes e analisar os indicadores globais e por projeto. A combinação destes elementos permite distinguir uma execução com testes aprovados de uma execução com cobertura efetivamente medida.

A recolha pode ser efetuada na raiz da solução com o seguinte comando:

```powershell
dotnet test .\CampusCarpool.slnx -c Release --settings .\coverage.runsettings --collect "XPlat Code Coverage" --logger "trx;LogFileName=I061-s06.trx" --results-directory .\artifacts\I061
```

Desta execução resultam, nomeadamente, dois tipos de evidência:

- **TRX:** regista a execução dos testes, incluindo testes aprovados, falhados e eventualmente ignorados.
- **`coverage.cobertura.xml`:** contém os dados de cobertura, permitindo analisar as linhas e os ramos abrangidos pelos testes, bem como a sua distribuição pelas classes e projetos.

A comparação entre medições deve utilizar, sempre que possível, a mesma configuração de cobertura. Alterações nas exclusões, no código considerado ou no conjunto de testes podem afetar as percentagens e dificultar a comparação direta.

## 3. Resultados de cobertura

Os resultados principais desta secção correspondem à execução final da **Sprint 6, em 02/11/2026**, obtida em `artifacts/I061-entrega-final`, após a submissão das alterações ao Git. O resultado da execução e as métricas foram novamente confirmados. Os dados históricos de 28/10/2026, documentados na I059, são apresentados apenas para comparação.

### 3.1. Cobertura global — Sprint 6

| Indicador | Valor |
|---|---:|
| Linhas analisadas | 2 242 |
| Linhas cobertas | 1 976 |
| Linhas não cobertas | 266 |
| **Cobertura de linhas (atributo `line-rate` do XML)** | **88,13%** |
| Ramos analisados | 817 |
| Ramos cobertos | 678 |
| Ramos não cobertos | 139 |
| **Cobertura de ramos (atributo `branch-rate` do XML)** | **82,98%** |
| Testes executados | 347 |
| Testes aprovados | 347 |
| Testes falhados / ignorados | 0 / 0 |

**Nota de precisão:** os valores percentuais acima reproduzem os atributos de taxa lidos do XML Cobertura e apresentados pelo PowerShell a duas casas decimais. O cálculo diretamente pelas contagens inteiras fornece 88,14% (1 976/2 242) e 82,99% (678/817). Esta diferença de 0,01 ponto percentual indica que a taxa serializada e as contagens não coincidem exatamente na precisão apresentada; deve conservar-se o valor tal como reportado pelo ficheiro e não misturar as duas convenções sem explicação. A reprodução completa requer guardar o XML original.

A taxa de linhas é superior à de ramos, pelo que continuam a existir alternativas de decisão não exercitadas. **266 linhas** e **139 ramos** permanecem não cobertos nas contagens reportadas.

### 3.2. Cobertura por projeto — Sprint 6

| Projeto | Cobertura de linhas | Cobertura de ramos |
|---|---:|---:|
| `CampusCarpool.Api` | 88,41% | 82,17% |
| `CampusCarpool.Application` | 87,97% | 81,66% |
| `CampusCarpool.Domain` | 88,63% | 100,00% |
| `CampusCarpool.Infrastructure` | 87,42% | 90,54% |

Os valores por projeto foram lidos dos atributos `line-rate` e `branch-rate` de `packages/package` na recolha anterior `I061-validado`. Os valores globais foram reconfirmados no XML da execução final; a tabela por projeto deverá ser confrontada com esse XML antes da aprovação documental.

**CampusCarpool.Api.** Os resultados de 88,41% de linhas e 82,17% de ramos sugerem dar continuidade à verificação de entradas inválidas e respostas de erro dos endpoints.

**CampusCarpool.Application.** Os resultados de 87,97% de linhas e 81,66% de ramos tornam esta camada a principal prioridade para rever alternativas de validação, permissões e transições de estado, pois tem a menor cobertura de ramos nesta recolha.

**CampusCarpool.Domain.** A cobertura de linhas é de 88,63% e a de ramos é de 100,00% **para o código incluído na medição**. Isto não garante, por si só, cobertura de todos os comportamentos possíveis.

**CampusCarpool.Infrastructure.** A cobertura de linhas é de 87,42% e a de ramos de 90,54%; podem ser considerados cenários adicionais de falha de persistência e integrações.

### 3.3. Comparação com a medição histórica da I059

| Indicador | I059 — 28/10/2026 | I061 — 02/11/2026 |
|---|---:|---:|
| Linhas analisadas | 1 737 | 2 242 |
| Linhas cobertas | 1 467 | 1 976 |
| Cobertura de linhas reportada | 84,45% | 88,13% |
| Ramos analisados | 705 | 817 |
| Ramos cobertos | 563 | 678 |
| Cobertura de ramos reportada | 79,85% | 82,98% |

As taxas reportadas são superiores na Sprint 6 em **3,68 pontos percentuais para linhas** e **3,13 pontos percentuais para ramos**. Contudo, o universo medido cresceu de 1 737 para 2 242 linhas e de 705 para 817 ramos. Não é possível atribuir automaticamente a diferença a novos testes, nem afirmar equivalência metodológica completa entre as duas medições sem confirmar o âmbito, as exclusões e os ficheiros originais da I059. Os valores da I059 foram transcritos da documentação histórica, não recalculados a partir do XML original.

## 4. Interpretação e oportunidades de melhoria

Os resultados permitem identificar algumas prioridades para reforçar a qualidade dos testes:

1. **Dar prioridade aos ramos não cobertos da camada Application.** Os 81,66% de cobertura de ramos sugerem que há alternativas de execução a explorar. Devem ser analisados casos com dados inválidos, permissões insuficientes, estados incompatíveis e situações em que as operações não podem ser concluídas.
2. **Complementar os cenários de sucesso com cenários de erro.** Testar apenas o percurso normal de uma funcionalidade não permite verificar todos os resultados possíveis. Nos endpoints e serviços, importa considerar também pedidos incompletos, recursos inexistentes e situações de conflito.
3. **Analisar as classes com menor cobertura.** A análise do ficheiro Cobertura XML permite localizar métodos e decisões não exercitados, de forma a criar testes dirigidos às lacunas concretas, em vez de aumentar apenas o número total de testes.
4. **Manter a recolha integrada na validação do backend.** Repetir a medição após alterações relevantes permite acompanhar a evolução da cobertura e detetar reduções que justifiquem novos cenários de teste.

A cobertura constitui um indicador útil, mas **não equivale à qualidade total dos testes**. Uma percentagem elevada não demonstra, isoladamente, que as asserções são adequadas, que todos os requisitos estão contemplados ou que o sistema se comporta corretamente em produção. Por isso, a análise de cobertura deve ser acompanhada dos resultados dos testes e da verificação dos requisitos funcionais.

## 5. Evidência da execução da Sprint 6

| Elemento | Registo |
|---|---|
| Data da execução final | 02/11/2026 |
| Commit (`git rev-parse HEAD`) | `0d2351ae12cc9fed211dbfbd0374a6f74e46b6b6` |
| Estado da árvore Git | Limpa — `git status --short` sem resultados |
| Ficheiro de resultados | `artifacts/I061-entrega-final/I061-s06-final.trx` |
| Ficheiro de cobertura | `artifacts/I061-entrega-final/**/coverage.cobertura.xml` |
| Testes executados / aprovados / falhados / ignorados | 347 / 347 / 0 / 0 |
| Cobertura de linhas / ramos — atributos XML | 88,13% / 82,98% |
| Linhas cobertas / válidas | 1 976 / 2 242 |
| Ramos cobertos / válidos | 678 / 817 |
| Avisos da compilação | 291 (não constituem falhas dos testes) |

A execução final terminou com sucesso, tendo o xUnit apresentado **347 testes aprovados, zero falhados e zero ignorados**. Os contadores lidos do ficheiro TRX confirmaram `total=347`, `executed=347`, `passed=347`, `failed=0`. O ficheiro Cobertura XML da mesma recolha apresenta `line-rate=0.8813`, `branch-rate=0.8298000000000001`, `lines-valid=2242`, `lines-covered=1976`, `branches-valid=817` e `branches-covered=678`.

**Rastreabilidade:** após a execução, `git rev-parse HEAD` devolveu o commit identificado acima e `git status --short` não apresentou alterações. Deste modo, a versão de código local ficou identificada e sem alterações não submetidas no momento da verificação. Para confirmação documental independente, o TRX e o XML originais da pasta `I061-entrega-final` devem acompanhar a entrega da issue; os resultados aqui transcritos não substituem esses anexos.

**Precisão das percentagens:** preservam-se os valores serializados nos atributos do XML (88,13% e 82,98%); os quocientes das contagens, quando arredondados a duas casas, produzem 88,14% e 82,99%. Esta diferença de 0,01 ponto percentual está documentada e deve ser avaliada juntamente com o XML original.

## 6. Conclusão

Na execução instrumentada disponível da Sprint 6, **347 de 347 testes passaram**. O relatório Cobertura indica **88,13% de cobertura de linhas** e **82,98% de ramos**; os dados foram obtidos do ficheiro XML e não extrapolados dos números históricos.

A `CampusCarpool.Application` apresenta a cobertura de ramos mais baixa (81,66%) e constitui uma área prioritária para testes adicionais. Face à documentação histórica da I059, as taxas reportadas são superiores, mas as alterações no universo de código medido impedem concluir que a diferença se deve apenas ao reforço da suite de testes.

A execução foi repetida após o commit `0d2351ae12cc9fed211dbfbd0374a6f74e46b6b6`, com a árvore de trabalho limpa e 347 testes aprovados. Para a aprovação documental, devem ser anexados à issue os ficheiros TRX e Cobertura XML da execução final, permitindo verificar independentemente os resultados e a cobertura.

## 7. Referências técnicas

- Relatório I059 — valores históricos de testes e cobertura, medição de 28/10/2026 (não verificados nesta análise contra o XML original).
- `artifacts/I061-entrega-final/I061-s06-final.trx` — contadores da execução de 347 testes.
- `artifacts/I061-entrega-final/**/coverage.cobertura.xml` — taxas e contagens da recolha da Sprint 6.
- `coverage.runsettings` — configuração da recolha de cobertura.
- `CampusCarpool.slnx` — solução .NET do backend.
- xUnit e XPlat Code Coverage — execução de testes e recolha de métricas.
