# Campus Carpool — .NET 10 — Base de desenvolvimento

Projetos: Api, Application, Domain, Infrastructure e Tests. Consulte `docs/IMPLEMENTACAO-STATUS.md` para limitações.

## Configuração local (Visual Studio / .NET 10)

1. Configurar uma instância SQL Server acessível.
2. Definir a connection string `ConnectionStrings__CampusCarpool` por variável de ambiente ou User Secrets.
3. Definir `Jwt__Key` com chave aleatória de pelo menos 32 bytes **fora do Git** (por exemplo, User Secrets).
4. Gerar e aplicar migrations apenas depois de rever o modelo e o script (`database/scripts/README.md`).
5. Executar `dotnet restore CampusCarpool.slnx`, `dotnet build CampusCarpool.slnx`, `dotnet test CampusCarpool.slnx`.
6. Executar `dotnet run --project src/CampusCarpool.Api`.

Sem configuração de JWT, a aplicação falha ao arrancar intencionalmente para evitar configuração insegura. Sem base de dados configurada, endpoints que usam DbContext não funcionam.

Não enviar segredos para o GitLab. Verificar `docs/IMPLEMENTACAO-STATUS.md` antes de apresentar qualquer funcionalidade como concluída.
