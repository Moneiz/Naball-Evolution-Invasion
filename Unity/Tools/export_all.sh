#!/bin/sh
# Régénère les données Unity d'un niveau depuis le .blend original.
# Usage : sh Unity/Tools/export_all.sh [scène]   (Blender 2.8+ doit être dans le PATH)
set -e
SCENE="${1:-Ger_FieldSwamp}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
WORK="$(mktemp -d)"
python3 "$ROOT/Unity/Tools/extract_logic.py" "$ROOT/Assets/MAP_DATA.scenes" "$WORK"
blender -b "$ROOT/Assets/MAP_DATA.scenes" --python "$ROOT/Unity/Tools/export_level.py" -- \
    "$SCENE" "$WORK/logic/$(echo "$SCENE" | tr ' ' '_').json" "$WORK/out"
python3 "$ROOT/Unity/Tools/build_unity_data.py" "$WORK" "$WORK/out" "$SCENE"
echo "Données brutes (logic bricks, scripts intégrés) : $WORK"
