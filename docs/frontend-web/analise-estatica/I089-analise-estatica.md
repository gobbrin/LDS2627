# I089 — Relatório de análise estática do frontend web

**Projeto:** G15 — Campus Carpool  
**Milestone/Sprint:** M4 / S09  
**Issue:** I089 — Configurar a análise estática do frontend web e elaborar o relatório  
**Componente:** `frontend-web`  
**Versão documental:** v01  
**Versão efetivamente testada:** versão mais recente do projeto Campus Carpool  
**Branch:** `ci/i089-analise-estatica-web`

## 1. Objetivo e alinhamento documental

Documentar a configuração e execução local de ESLint, Prettier e compilação Angular para a análise estática do frontend web. A documentação de testes de sistema web e a validação do sistema completo não são substituídas por este relatório.

Estão previstas regras ESLint para TypeScript/Angular e acessibilidade dos templates, Prettier para formatação e verificação da compilação via Angular.

## 2. Configuração e ambiente

| Elemento | Configuração / evidência |
|---|---|
| ESLint | `npm.cmd run lint` executa `eslint src` |
| Prettier | `npm.cmd run format:check` executa `prettier --check .` |
| Build Angular | `npm.cmd run build` executa `ng build` |
| Testes de apoio | `npm.cmd test` executa `ng test --watch=false --coverage` com Karma e ChromeHeadless |
| Instalação | `npm.cmd ci` instalou 611 pacotes, auditou 612 |
| Node/npm efetivos | Node `v26.13.1`, npm `11.20.0` |
| Versões declaradas no projeto | Node `>=24.15.0 <25`, npm `>=11 <13` |

**Compatibilidade:** a instalação emitiu `npm warn EBADENGINE`, porque Node 26 está fora do intervalo declarado. Os comandos registados executaram com sucesso, mas recomenda-se repetir a validação em Node 24 compatível para comparar com o ambiente previsto. O `npm ci` reportou ainda **9 vulnerabilidades moderadas**; esta indicação não é um erro do ESLint nem prova exploração efetiva.

## 3. Comandos e resultados observados  

| Comando | Resultado | 
|---|---|
| `npm.cmd ci` | Instalação terminada, com avisos de engine, dependências e auditoria | 
| `npm.cmd run format:check` | **Conforme:** `All matched files use Prettier code style!` | 
| `npm.cmd run lint` | **Conforme:** ESLint terminou sem erros no registo | 
| `npm.cmd test` | **244/244 testes aprovados** em ChromeHeadless | 
| `npm.cmd run build` | **Conforme:** `Application bundle generation complete` | 

## 4. Cobertura de testes web (evidência complementar)

| Métrica | Valor | Contadores |
|---|---:|---:|
| Statements | **84,37%** | 2214 / 2624 |
| Branches | **82,71%** | 665 / 804 |
| Functions | **81,44%** | 338 / 415 |
| Lines | **89,53%** | 1498 / 1673 |

São métricas da suite local Angular/Karma, não resultados de testes instrumentados Android nem evidência de execução da API real.

## 5. Rastreabilidade, achados e limites

| Aspeto | Avaliação fundamentada | 
|---|---|
| Configuração e identificação | ESLint, Prettier e Angular descritos |
| Formatação e lint | Aprovados localmente |
| Compilação web | Aprovada localmente |
| Testes e cobertura | 244 aprovados; 89,53% de linhas |
| Ambiente Node | Node 26 fora da faixa declarada | 
| Dependências | `npm ci` indicou 9 vulnerabilidades moderadas | 


O sucesso do lint não prova por si só segurança, autorização, funcionalidade, acessibilidade completa ou inexistência de defeitos. A revisão de formulários, cancelamentos, mensagens, ligações em tempo real e erros de mapa permanece um controlo complementar e específico.

## 6. Conclusão técnica

**Análise estática web local: CONFORME** para os comandos efetivamente executados (`format:check`, `lint`, `build`). A suite de testes integrou **244 aprovações e zero falhas**. A documentação está consistente com essas evidências.
