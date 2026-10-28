# I029 — Infraestrutura transversal

- ASP.NET Core 10, API com controllers e OpenAPI JSON em desenvolvimento (`/openapi/v1.json`).
- `GET /health` para verificação do processo (não testa SQL Server).
- Tratamento centralizado de exceções inesperadas com ProblemDetails (HTTP 500).
- 400/401/403/404 dependem de validação/autenticação/autorização e endpoints futuros; não são simulados nesta fase.
- Registo de dependências em Application e Infrastructure; ainda sem serviços de negócio.
- `CampusCarpoolDbContext` sem entidades/migrations. SQL Server só é registado quando há connection string.
- Definir `ConnectionStrings__CampusCarpool` por variável de ambiente ou `dotnet user-secrets`, nunca guardar credenciais no Git.
- Logging padrão ASP.NET Core; não registar corpos de pedidos, passwords, tokens ou mensagens.
- OpenAPI em desenvolvimento não inclui interface Swagger UI; fornece documento JSON.
- Sem migrations, scripts, autenticação real, endpoints de negócio ou deploy.
