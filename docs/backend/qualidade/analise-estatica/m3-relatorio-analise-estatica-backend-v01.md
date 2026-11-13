# M3 — Relatório de Análise Estática do Backend

**Projeto:** CampusCarpool  
**Issue:** I056 — Configurar análise estática do backend e elaborar relatório  
**Versão:** 01 — preliminar  
**Data:** 28/10/2026  
**Executor:** Gil Gabriel da Cunha Leão  
**Revisor:** Cristiana Gabriela Oliveira Macedo  
**Auditor:** Vítor Samuel Vieira Lopes

## 1. Objetivo e estado

Configurar e documentar a análise estática do backend, identificar problemas e justificar as ações tomadas.

**Estado:** configuração preparada; execução e resultados dos analisadores pendentes. Este documento **não certifica aprovação de quality gate**.

## 2. Ferramenta e configuração

- **Ferramenta:** analisadores Roslyn/.NET SDK 10.
- **Configuração:** `Directory.Build.props` e regras em `.editorconfig`.
- **Verificação adicional:** `dotnet format --verify-no-changes`.
- **Recolha de evidências:** scripts que guardam `restore`, `build` e `format` em logs separados.

## 3. Âmbito e regras

Âmbito: projetos do backend **API, Application, Domain e Infrastructure**, correspondentes a quatro projetos `.csproj` e 122 ficheiros C# de produção observados.

- Segurança e fiabilidade: `warning`.
- Performance: `suggestion`.
- Restantes regras: recomendações do SDK.
- Os avisos não são globalmente promovidos a erros nesta fase.

## 4. Inventário preliminar do código

Na inspeção local do ZIP de 28/10/2026 foram observados **47 ficheiros C# de produção com tamanho zero**. Podem ser *placeholders*: a existência de um ficheiro vazio **não demonstra um erro de análise estática**.

A lista integral encontra-se em `I056-inventario-preliminar.md`.

## 5. Execução e resultados

Não foi possível executar `dotnet restore`, `dotnet build` nem `dotnet format` no ambiente de preparação, porque o executável `dotnet` não estava instalado.

Por isso, ainda **não existem diagnósticos reais, contagens verificadas de avisos ou erros, nem quality gate validado**.

## 6. Problemas, ações e falsos positivos

| Elemento | Estado |
|---|---|
| Problemas confirmados pelos analisadores | Por apurar |
| Ações corretivas executadas no código | Nenhuma |
| Falsos positivos confirmados | Nenhum identificado; análise por executar |

Os ficheiros vazios constituem apenas uma observação estrutural, não uma conclusão de erro.

## 7. Integração futura na pipeline I057

Os scripts `scripts/analisar-backend.ps1` e `scripts/analisar-backend.sh` executam a análise e devolvem um código de saída diferente de zero quando `restore`, `build` ou `format` falham. Podem ser invocados num job futuro da pipeline, sem alterar a configuração global nesta issue.

## 8. Evidências e conclusão

**Evidências fornecidas:** `.editorconfig`, `Directory.Build.props`, scripts de execução e inventário preliminar.

**Evidências pendentes:** logs reais com SDK 10, triagem dos diagnósticos, ações corretivas justificadas, versão final do relatório e Merge Request.

**Critérios de aceitação:** identificação e configuração da ferramenta, âmbito e preparação de integração documentados. Execução, resultados, ações, falsos positivos, validação do relatório final e MR permanecem pendentes.

## 9. Quality gate proposto (a validar pela equipa)

Falhas em `restore`, `build` ou `dotnet format --verify-no-changes` produzem saída diferente de zero. Os avisos Roslyn não são automaticamente tratados como erros; devem ser analisados e documentados. Este mecanismo **não constitui ainda um quality gate global da CI**.
