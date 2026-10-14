#!/usr/bin/env bash
# Compila mods e gera os zips prontos para upload no Thunderstore (dist/).
#   uso: ./pack-thunderstore.sh [NomeDoMod ...]   (padrao: todos os mods)
#
# Cada mod tem em thunderstore/:
#   packages.tsv            -> uma linha por pacote: nome<TAB>target-framework<TAB>descricao
#   manifest.template.json  -> manifest com __NAME__, __VERSION__ e __DESCRIPTION__
#   icon.png (256x256), README.md, CHANGELOG.md
#   *_icon.png (opcional)   -> imagens extras copiadas para Mods/ (ex: icone do app do celular)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
DIST="$ROOT/dist"

if [ "$#" -gt 0 ]; then
  MODS=("$@")
else
  MODS=()
  for f in "$ROOT"/*/thunderstore/packages.tsv; do MODS+=("$(basename "$(dirname "$(dirname "$f")")")"); done
fi

mkdir -p "$DIST"

for MOD in "${MODS[@]}"; do
  PROJ="$ROOT/$MOD"
  TS="$PROJ/thunderstore"
  VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$PROJ/$MOD.csproj")"

  dotnet build "$PROJ/$MOD.csproj" -c Release

  while IFS=$'\t' read -r name tfm desc; do
    [ -z "$name" ] && continue
    if [ "${#desc}" -gt 250 ]; then echo "Descricao de $name passa de 250 caracteres" >&2; exit 1; fi

    stage="$DIST/stage-$name"
    rm -rf "$stage" && mkdir -p "$stage/Mods"
    cp "$PROJ/bin/Release/$tfm/$MOD.dll" "$stage/Mods/"
    for extra in "$TS"/*_icon.png; do [ -e "$extra" ] && cp "$extra" "$stage/Mods/"; done
    cp "$TS/icon.png" "$TS/README.md" "$TS/CHANGELOG.md" "$stage/"
    sed -e "s/__NAME__/$name/" -e "s/__VERSION__/$VERSION/" -e "s/__DESCRIPTION__/$desc/" \
      "$TS/manifest.template.json" > "$stage/manifest.json"

    rm -f "$DIST/$name-$VERSION.zip"
    (cd "$stage" && zip -qr "$DIST/$name-$VERSION.zip" .)
    rm -rf "$stage"
    echo "Gerado: dist/$name-$VERSION.zip"
  done < "$TS/packages.tsv"
done
