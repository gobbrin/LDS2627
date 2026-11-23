#!/usr/bin/env bash
# Pára o SQL Server local dos testes (docker-compose.sqlserver.yml).
# Uso:  bash backend/scripts/parar-sqlserver.sh            # pára e remove o contentor; mantém o volume de dados
#       bash backend/scripts/parar-sqlserver.sh --limpar   # também apaga o volume (começa do zero na próxima vez)
# O ficheiro backend/.env (password local) não é apagado.
set -euo pipefail

# Pasta backend/ (o script está em backend/scripts/).
cd "$(dirname "$0")/.."

# Sem .env o compose recusa ler o ficheiro (a password é obrigatória); um valor qualquer serve para parar.
export SQLSERVER_SENHA="${SQLSERVER_SENHA:-so-para-parar}"
if [ -f .env ]; then
  set -a
  . ./.env
  set +a
fi

if [ "${1:-}" = "--limpar" ]; then
  docker compose -f docker-compose.sqlserver.yml down -v
  echo "SQL Server parado e volume de dados apagado."
else
  docker compose -f docker-compose.sqlserver.yml down
  echo "SQL Server parado (dados mantidos no volume)."
fi
