#!/usr/bin/env bash
# Monta, a partir de uma tag ou commit, a parte técnica da pasta /projeto da entrega
# (Regulamento da UC, §19). Só copia o que está versionado nesse ref.
#
# Uso:   scripts/gerar-projeto.sh <ref> <milestone> <versao> <destino>
# Ex.:   scripts/gerar-projeto.sh m4-entrega m4 01 ../20262027-G15-m4/projeto
#
# Cada componente fica em <pasta-nivel-2>/<milestone>-v<versao>/. Para dar versões
# diferentes a um componente, definir VERSAO_BACKEND, VERSAO_WEB, VERSAO_MOBILE ou
# VERSAO_CICD (por exemplo VERSAO_BACKEND=02).
set -euo pipefail

if [ $# -ne 4 ]; then
  sed -n '2,10p' "$0"; exit 2
fi
REF=$1; M=$2; V=$3; DEST=$4
RAIZ=$(git rev-parse --show-toplevel)
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

git -C "$RAIZ" rev-parse --verify --quiet "$REF^{commit}" >/dev/null || { echo "ref desconhecido: $REF"; exit 1; }
git -C "$RAIZ" archive "$REF" | tar -x -C "$TMP"

T="$DEST/04-artefactos-tecnicos"
Q="$DEST/03-artefactos-de-qualidade-de-produto"

copiar_pasta() { # origem destino
  [ -d "$TMP/$1" ] || { echo "  (sem $1 em $REF)"; return; }
  if [ -e "$2" ]; then echo "já existe, não substituo: $2"; exit 1; fi
  mkdir -p "$2"; cp -a "$TMP/$1/." "$2/"; echo "  $1 -> ${2#"$DEST"/}"
}
copiar_ficheiros() { # origem destino (só os ficheiros do primeiro nível)
  [ -d "$TMP/$1" ] || return 0
  mkdir -p "$2"
  find "$TMP/$1" -maxdepth 1 -type f -exec cp -a {} "$2/" \;
  echo "  $1/* -> ${2#"$DEST"/}"
}

echo "A gerar $DEST a partir de $REF ($(git -C "$RAIZ" rev-parse --short "$REF"))"
copiar_pasta backend                    "$T/04.12-backend/$M-v${VERSAO_BACKEND:-$V}"
copiar_pasta backend/database/scripts   "$T/04.13-scripts-de-base-de-dados/$M-v${VERSAO_BACKEND:-$V}"
copiar_pasta frontend-web               "$T/04.14-frontend-web/$M-v${VERSAO_WEB:-$V}"
copiar_pasta frontend-mobile            "$T/04.15-frontend-mobile/$M-v${VERSAO_MOBILE:-$V}"
copiar_pasta .github/workflows          "$T/04.16-ci-cd/$M-v${VERSAO_CICD:-$V}"

copiar_ficheiros docs/frontend-web/plano-de-testes       "$Q/03.02-frontend-web/03.02.01-plano-de-testes"
copiar_ficheiros docs/frontend-web/testes-de-sistema     "$Q/03.02-frontend-web/03.02.02-testes-de-sistema"
copiar_ficheiros docs/frontend-mobile/plano-de-testes    "$Q/03.03-frontend-mobile/03.03.01-plano-de-testes"
copiar_ficheiros docs/frontend-mobile/testes-de-sistema  "$Q/03.03-frontend-mobile/03.03.02-testes-de-sistema"

echo "Feito. Os documentos de gestão, de processo e os PDF assinados não estão no repositório:"
echo "juntam-se à mão, com o README da milestone (00-indice-e-submissao)."
