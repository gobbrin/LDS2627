# Campus Carpool — backend (.NET 10)

Projetos: Api, Application, Domain, Infrastructure e Tests, na solução `CampusCarpool.slnx`. Documentação em [`docs/backend/`](../docs/backend/README.md): o [guia do código](../docs/backend/arquitetura/GUIA-DO-CODIGO.md) explica as camadas e o [manual I064](../docs/backend/operacao/I064-MANUAL-INSTALACAO-BACKEND.md) a instalação e a configuração. O estado resumido está em [`IMPLEMENTACAO-STATUS.md`](../docs/backend/IMPLEMENTACAO-STATUS.md).

## Configuração local

1. Configurar uma instância SQL Server acessível (LocalDB, contentor ou remota).
2. Definir `ConnectionStrings:DefaultConnection` em User Secrets ou na variável `ConnectionStrings__DefaultConnection`.
3. Definir `Jwt:Key` (variável `Jwt__Key`) com uma chave aleatória de pelo menos 32 bytes, **fora do Git**.
4. Aplicar as migrations de `src/CampusCarpool.Infrastructure/Migrations` à parte (`database/scripts/README.md`); a API nunca as aplica ao arrancar.
5. Executar `dotnet build CampusCarpool.slnx` e `dotnet test CampusCarpool.slnx`.
6. Executar `dotnet run --project src/CampusCarpool.Api` (fica em `http://localhost:5100`).

Sem `Jwt:Key` ou sem `ConnectionStrings:DefaultConnection`, a API recusa arrancar, de propósito. As integrações são opcionais: OpenRouteService (`Maps:ApiKey`), HERE Routing para a linha do mapa (`Here:ApiKey`), Brevo (`Brevo:*`, `Frontend:ConfirmUrl`) e FCM (`Firebase:*`); o manual I064 diz o que acontece sem cada uma.

Não enviar segredos para o repositório: é público.

## Testes em SQL Server local (Docker)

O `dotnet test` normal usa SQLite em memória e continua a ser o caminho rápido. Para correr a mesma suite em SQL Server real, como o passo «Test on SQL Server» da CI, sem fazer commit:

```bash
bash scripts/testes-sqlserver.sh                                         # suite toda em SQL Server
bash scripts/testes-sqlserver.sh --filter "FullyQualifiedName~Geometria" # só alguns testes
bash scripts/parar-sqlserver.sh                                          # pára (mantém os dados)
bash scripts/parar-sqlserver.sh --limpar                                 # pára e apaga o volume
```

- Requer Docker utilizável sem `sudo` (utilizador no grupo `docker`). Na primeira vez descarrega a imagem `mcr.microsoft.com/mssql/server:2022-latest` (cerca de 1,5 GB), a mesma da CI.
- O contentor está em `docker-compose.sqlserver.yml`, com healthcheck por `sqlcmd` e um volume para os dados.
- A password do `sa` fica em `backend/.env`, ignorado pelo git; o script cria-o com uma password aleatória se não existir (modelo em `.env.example`). Mudar `SQLSERVER_PORTA` no `.env` se a 1433 estiver ocupada.
- O script exporta `CAMPUSCARPOOL_TESTES_SQLSERVER` e `CAMPUSCARPOOL_TESTES_BD=SqlServer` só para essa execução; os argumentos extra seguem para o `dotnet test`.
