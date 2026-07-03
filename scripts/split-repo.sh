#!/usr/bin/env bash
# 다이스 던전을 게임 전용 저장소로 분리한다 (감사 E4).
#
# 사전 준비: GitHub에서 빈 저장소 생성 (예: BearHumanS/dice-dungeon, README 없이)
# 사용법:   bash scripts/split-repo.sh git@github.com:BearHumanS/dice-dungeon.git [출력디렉토리]
# 이후:     새 저장소 Settings → Pages → Source: GitHub Actions (배포 재활성화)
set -euo pipefail

REMOTE="${1:?사용법: split-repo.sh <새 저장소 git URL> [출력디렉토리]}"
SRC="$(cd "$(dirname "$0")/.." && pwd)"
DEST="${2:-$SRC/../dice-dungeon}"

mkdir -p "$DEST"
echo "게임 파일 복사 → $DEST"

# 게임 관련 파일만 (Scrapling 원본 코드 제외)
for path in game-design prototype prototype-web unity-integration scripts; do
  cp -r "$SRC/$path" "$DEST/"
done
mkdir -p "$DEST/.github/workflows"
cp "$SRC/.github/workflows/deploy-pages.yml" "$DEST/.github/workflows/"
cp "$SRC/scripts/dice-dungeon-README.md" "$DEST/README.md"

# 새 저장소용 배포 워크플로는 기본 브랜치 기준으로 정리
sed -i.bak 's/branches: \[main, claude\/mobile-dungeon-rpg-design-f76ndx\]/branches: [main]/' \
  "$DEST/.github/workflows/deploy-pages.yml" && rm -f "$DEST/.github/workflows/deploy-pages.yml.bak"

cat > "$DEST/.gitignore" <<'EOF'
bin/
obj/
*.user
.DS_Store
EOF

cd "$DEST"
git init -b main
git add -A
git commit -m "다이스 던전: 게임 전용 저장소로 분리 (기획·Core·웹·Unity·배포 파이프라인)"
git remote add origin "$REMOTE"
git push -u origin main

echo ""
echo "완료. 남은 1회 작업:"
echo "  1) 새 저장소 Settings → Pages → Source: GitHub Actions"
echo "  2) 공개 URL: https://<계정>.github.io/dice-dungeon/"
