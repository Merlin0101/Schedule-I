#!/usr/bin/env bash
# Compila o mod e gera os zips prontos para upload no Thunderstore (dist/).
#   uso: ./pack-thunderstore.sh [NomeDoMod]   (padrao: GrowFaster)
set -euo pipefail

MOD="${1:-GrowFaster}"
ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJ="$ROOT/$MOD"
DIST="$ROOT/dist"
VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJ/$MOD.csproj")"

dotnet build "$PROJ/$MOD.csproj" -c Release

rm -rf "$DIST" && mkdir -p "$DIST"

# pacote <nome-no-thunderstore> <target-framework> <descricao>
pack() {
  local name="$1" tfm="$2" desc="$3"
  local stage="$DIST/stage-$name"
  mkdir -p "$stage/Mods"
  cp "$PROJ/bin/Release/$tfm/$MOD.dll" "$stage/Mods/"
  cp "$PROJ/thunderstore/icon.png" "$PROJ/thunderstore/README.md" "$PROJ/thunderstore/CHANGELOG.md" "$stage/"
  sed -e "s/__NAME__/$name/" -e "s/__VERSION__/$VERSION/" -e "s/__DESCRIPTION__/$desc/" \
    "$PROJ/thunderstore/manifest.template.json" > "$stage/manifest.json"
  (cd "$stage" && zip -qr "$DIST/$name-$VERSION.zip" .)
  rm -rf "$stage"
  echo "Gerado: dist/$name-$VERSION.zip"
}

pack "$MOD"      net6.0 "Plantas crescem mais rapido com multiplicador configuravel. Versao IL2CPP (padrao da Steam)."
pack "${MOD}_Mono" net472 "Plantas crescem mais rapido com multiplicador configuravel. Versao Mono (branch alternate)."
