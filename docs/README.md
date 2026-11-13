# Documentação

Uma pasta por componente. A documentação de gestão e de processo (backlog, atas, regulamentos, plano de qualidade assinado) fica nas pastas de entrega do grupo, fora do repositório.

| Pasta | Conteúdo |
|---|---|
| [`backend/`](backend/README.md) | Guia do código, entregas, testes (especificações, relatórios, coleções Postman), operação (pipeline, instalação, release) e qualidade (análise estática, plano de qualidade e DoR de m3) |
| [`frontend-web/`](frontend-web/) | `plano-de-testes/` (I085), `testes-de-sistema/` (I086) e `evidencias/` das issues, com capturas |
| [`frontend-mobile/`](frontend-mobile/) | `plano-de-testes/` (I090), `testes-de-sistema/` (I091) e `evidencias/` das issues: relatórios JUnit e JaCoCo, Lint, capturas e `VALIDACAO.md` da I077 |

Regras:

- Documentos novos entram na pasta do componente e na subpasta temática. Nada solto na raiz de `docs/`.
- Os `.pdf` são exportações dos `.md` homónimos; a fonte é o `.md`.
- As pastas `qualidade/revisoes/`, `qualidade/auditorias/` e `qualidade/divida-tecnica/` desta pasta são locais (`.gitignore`).

## Mudança de pastas (I111, 13/11/2026)

Até ao Sprint 07 o repositório seguia duas convenções: o backend em `CampusCarpool/` e os frontends na estrutura da entrega (`projeto/…/m4-v01`). Passou a ter uma pasta por componente na raiz. Os documentos anteriores a esta data podem citar os caminhos antigos:

| Antes | Agora |
|---|---|
| `CampusCarpool/` | `backend/` |
| `CampusCarpool/docs/` | `docs/backend/` |
| `projeto/04-artefactos-tecnicos/04.14-frontend-web/m4-v01/` | `frontend-web/` |
| `projeto/04-artefactos-tecnicos/04.15-frontend-mobile/m4-v01/` | `frontend-mobile/` |
| `…/m4-v01/docs/` (evidências e capturas) | `docs/frontend-<web\|mobile>/evidencias/` |
| `…/04.15-frontend-mobile/m4-v01/evidencias/` e `VALIDACAO.md` | `docs/frontend-mobile/evidencias/` |
| `projeto/03-…/03.02-frontend-web/03.02.0x-…` | `docs/frontend-web/plano-de-testes/`, `testes-de-sistema/` |
| `projeto/03-…/03.03-frontend-mobile/03.03.0x-…` | `docs/frontend-mobile/plano-de-testes/`, `testes-de-sistema/` |
| `G15_I031_Plano_Testes_Backend_S03.docx` (raiz) | `docs/backend/testes/m3-s03-i031-plano-testes-backend.docx` |
| `m3-s03-i062-…-v01.docx` (raiz) | `docs/backend/operacao/` |
| `docs/testes/I059_relatorio_testes_unitarios.*` | `docs/backend/testes/I059-relatorio-testes-unitarios.*` |
| `docs/testes/I055/CampusCarpool — I055*.json` (oficiais) | `docs/backend/testes/I055/I055-postman-{collection,environment}.json` |
| `docs/testes/I055/I055_CampusCarpool_Postman_*.json` (obsoletos) | `docs/backend/testes/I055/arquivo/` |

A entrega continua a seguir a hierarquia oficial: `scripts/gerar-projeto.sh` copia cada componente para a pasta `/projeto` correspondente.
