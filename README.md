# Campus Carpool (G15, LDS 2026/27)

Plataforma de boleias para a comunidade académica: backend .NET, frontend web em Angular e app Android em Kotlin.

## Estrutura

| Pasta | Conteúdo | Pipeline |
|---|---|---|
| [`backend/`](backend/) | API .NET 10 em camadas (`src/`), testes xUnit (`tests/`), scripts de base de dados e de testes | [`backend-ci.yml`](.github/workflows/backend-ci.yml) |
| [`frontend-web/`](frontend-web/) | Angular 22 e TypeScript | [`frontend-web-ci.yml`](.github/workflows/frontend-web-ci.yml) |
| [`frontend-mobile/`](frontend-mobile/) | Kotlin e Jetpack Compose, Android 10 ou superior (módulos `core` e `app`) | [`frontend-mobile-ci.yml`](.github/workflows/frontend-mobile-ci.yml) |
| [`docs/`](docs/README.md) | Documentação técnica, planos e especificações de testes e evidências, por componente | — |
| [`scripts/`](scripts/) | Ferramentas do repositório, como o `gerar-projeto.sh`, que monta a pasta `/projeto` da entrega | — |

Cada pipeline só corre quando muda a pasta do seu componente.

## Começar

| Componente | Comandos | Guia |
|---|---|---|
| Backend | `cd backend && dotnet build CampusCarpool.slnx && dotnet test CampusCarpool.slnx` | [Manual de instalação (I064)](docs/backend/operacao/I064-MANUAL-INSTALACAO-BACKEND.md) |
| Web | `cd frontend-web && npm ci && npm start` | [README](frontend-web/README.md) |
| Mobile | `cd frontend-mobile && ./gradlew :core:check :app:assembleDebug` | [README](frontend-mobile/README.md) |

A API não arranca sem `Jwt:Key` nem `ConnectionStrings:DefaultConnection` (User Secrets ou variáveis de ambiente). Não se versionam segredos.

## Convenções

- Português europeu em nomes, documentos e commits.
- Branches `tipo/i<número>-descricao`; commits `tipo(Ixxx): descrição`.
- A `develop` só recebe alterações por PR com duas aprovações.
- A entrega oficial (`/projeto`, Regulamento da UC §19) gera-se a partir de uma tag com `scripts/gerar-projeto.sh`.
