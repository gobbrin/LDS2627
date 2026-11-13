# Base de dados
O modelo EF Core foi preparado para SQL Server. Gerar uma migration inicial num ambiente com SDK .NET 10:

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
dotnet ef migrations add InitialCreate --project src/CampusCarpool.Infrastructure --startup-project src/CampusCarpool.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project src/CampusCarpool.Infrastructure --startup-project src/CampusCarpool.Api --output database/scripts/initial-idempotent.sql
```

Configurar `ConnectionStrings__CampusCarpool` e `Jwt__Key` antes de executar. Não aplicar migrations automaticamente em produção. O SQL gerado tem de ser revisto antes de aplicação.
