#!/usr/bin/env bash
# Core 로직을 Unity 프로젝트로 동기화한다.
# 사용법: bash unity-integration/sync-core.sh /path/to/UnityProject
set -euo pipefail

UNITY_PROJECT="${1:?사용법: sync-core.sh <Unity프로젝트경로>}"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DEST="$UNITY_PROJECT/Assets/_Project/Scripts"

mkdir -p "$DEST/Core" "$DEST/Game"

# Core: .cs만 복사 (csproj/bin/obj 제외)
rsync -av --delete \
  --include='*/' --include='*.cs' --exclude='*' \
  "$REPO_ROOT/prototype/DiceDungeon.Core/" "$DEST/Core/"

# asmdef + Game 스크립트
cp "$REPO_ROOT/unity-integration/Assets/_Project/Scripts/Core/DiceDungeon.Core.asmdef" "$DEST/Core/"
cp "$REPO_ROOT/unity-integration/Assets/_Project/Scripts/Game/"* "$DEST/Game/"

echo "동기화 완료: $DEST"
