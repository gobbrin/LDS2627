# CampusCarpool — Aumentar a cobertura de testes

## Situação inicial (medição fornecida)
- Cobertura global: **24,96% das linhas** (1310/5247).
- **58,63% dos ramos** (533/909).
- A cobertura inclui milhares de linhas de *migrations* e código gerado.
- `CampusCarpool.Infrastructure` apresenta apenas 4,31% na medição original.

## O que foi acrescentado
- 8 testes para `MapasService.CalcularComParagensAsync` e `CalcularMatrizAsync`.
- Configuração `coverage.runsettings` para excluir exclusivamente ficheiros de migrations e `obj` gerados automaticamente.

A exclusão de código gerado é uma decisão de medição, **não representa testes novos nem melhoria efetiva da cobertura desse código**. A meta de 80% deve ser avaliada sobre o âmbito acordado pela equipa/docente. Se for exigida cobertura bruta, não uses exclusões.

## Aplicar
1. Copiar o ficheiro de testes para `tests/CampusCarpool.Tests/Unit/`.
2. Copiar `coverage.runsettings` para a raiz `CampusCarpool/`.
3. Na raiz, executar:

```powershell
dotnet test .\CampusCarpool.slnx -c Release --settings .\coverage.runsettings --collect "XPlat Code Coverage" --results-directory .\artifacts\cobertura-atualizada --logger "trx;LogFilePrefix=cobertura-atualizada"
```

4. Verificar o ficheiro `coverage.cobertura.xml` gerado. Exemplo:

```powershell
$xml = Get-ChildItem .\artifacts\cobertura-atualizada -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime -Descending | Select-Object -First 1
[xml]$c = Get-Content $xml.FullName
"Linhas: {0:P2}; Ramos: {1:P2}" -f [double]::Parse($c.coverage.'line-rate',[cultureinfo]::InvariantCulture), [double]::Parse($c.coverage.'branch-rate',[cultureinfo]::InvariantCulture)
```

## Limitações
Não foi possível executar `dotnet test` neste ambiente (SDK .NET não instalado). Os testes são propostos com base no código-fonte enviado e **não estão validados**. Não é possível garantir 80% sem nova execução. Para atingir 80% no código não gerado, provavelmente serão necessários testes adicionais de `Infrastructure` (repositórios, serviços e integrações), além de caminhos ainda não cobertos na API.
