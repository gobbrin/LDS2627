# Base de dados

O esquema é o das migrations EF Core em `src/CampusCarpool.Infrastructure/Migrations` (17, de `InitialCreate` a `I112_ModeloDadosV07`). Esta pasta não guarda scripts SQL: gera-se um quando for preciso, a partir das migrations.

Comandos a partir de `backend/`, com `ConnectionStrings__DefaultConnection` e `Jwt__Key` definidos (para gerar um script, chegam valores fictícios):

```bash
dotnet tool install --global dotnet-ef --version 10.0.*
dotnet ef database update --project src/CampusCarpool.Infrastructure --startup-project src/CampusCarpool.Api
dotnet ef migrations script --idempotent --project src/CampusCarpool.Infrastructure --startup-project src/CampusCarpool.Api --output database/scripts/campuscarpool-idempotente.sql
```

Regras:

- As migrations nunca se aplicam ao arrancar a API. Em produção, rever o SQL gerado antes de o aplicar.
- Uma alteração ao `OnModelCreating` exige uma migration nova (`dotnet ef migrations add <Nome> …`); confirmar com `dotnet ef migrations has-pending-model-changes`.
- Armadilhas do SQL Server (colunas usadas por um `CHECK` ou por um índice filtrado, preenchimento de dados dentro de `EXEC`): guia do código, [§5 «Migrations»](../../../docs/backend/arquitetura/GUIA-DO-CODIGO.md#migrations).
- O esquema segue o modelo de dados v07 desde `I112_ModeloDadosV07` (BE-33): `CHECK`, tamanhos e índices; os `CHECK` acrescentados a uma BD com dados entram `WITH NOCHECK` (valem para as gravações novas). O que ficou de fora da v07 está no guia, §5 «Migrations».
- A CI aplica as migrations num SQL Server real e corre lá a suite de testes; localmente, sem SQL Server, os testes usam SQLite com o esquema do modelo.
