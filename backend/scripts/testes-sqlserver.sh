#!/usr/bin/env bash
# Corre a suite completa de testes em SQL Server real, como o passo «Test on SQL Server» da CI.
# Uso, a partir de qualquer pasta:  bash backend/scripts/testes-sqlserver.sh [argumentos extra do dotnet test]
# Exemplo:  bash backend/scripts/testes-sqlserver.sh --filter "FullyQualifiedName~GeometriaRota"
#
# Passos: cria o .env se faltar, sobe o contentor (se ainda não estiver de pé), espera pelo
# healthcheck do sqlcmd, exporta as variáveis dos testes e corre dotnet test.
# Os testes rápidos em SQLite continuam a ser o "dotnet test" normal, sem este script.
set -euo pipefail

# Pasta backend/ (o script está em backend/scripts/).
PASTA_BACKEND="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PASTA_BACKEND"

COMPOSE="docker compose -f docker-compose.sqlserver.yml"

# 1. Criar o .env com uma password aleatória local, se ainda não existir (nunca vai para o git).
if [ ! -f .env ]; then
  # Hex aleatório mais "Aa1!" para cumprir as regras de complexidade do SQL Server (como na CI).
  echo "SQLSERVER_SENHA=$(openssl rand -hex 16)Aa1!" > .env
  echo "SQLSERVER_PORTA=1433" >> .env
  echo "Criado backend/.env com uma password local aleatória."
fi

# 2. Ler a password e a porta do .env.
set -a
. ./.env
set +a
PORTA="${SQLSERVER_PORTA:-1433}"

# 3. Subir o contentor e esperar que fique "healthy" (o compose não recria se já estiver de pé).
echo "A arrancar o SQL Server (porta $PORTA) e a esperar pelo sqlcmd..."
$COMPOSE up -d --wait

# 4. Variáveis lidas pelos testes (BaseDeDadosDeTeste e SqlServerFactAttribute).
export CAMPUSCARPOOL_TESTES_SQLSERVER="Server=localhost,$PORTA;User Id=sa;Password=$SQLSERVER_SENHA;TrustServerCertificate=True;"
export CAMPUSCARPOOL_TESTES_BD=SqlServer

# 5. Correr a suite inteira em SQL Server; o < /dev/null evita que o dotnet fique à espera de input.
echo "A correr os testes em SQL Server..."
dotnet test CampusCarpool.slnx "$@" < /dev/null
