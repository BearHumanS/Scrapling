# 🎲 다이스 던전 (Dice Dungeon)

브루마블 × 던전 RPG — 주사위로 보드를 돌고, 칸을 점령하고, 층을 내려가는 모바일 로그라이트.

**▶ 플레이 (웹)**: GitHub Pages 배포 후 `https://<계정>.github.io/dice-dungeon/`

## 구성

| 경로 | 내용 |
|------|------|
| `prototype-web/` | **플레이 가능한 웹 게임** (단일 HTML + PWA) — 커밋 시 Pages 자동 배포 |
| `prototype/` | C# Core 게임 규칙 + 밸런스 시뮬레이터 (`dotnet run --project DiceDungeon.Sim`) |
| `unity-integration/` | Unity 6 모바일 빌드용 통합 패키지 (SETUP.md 참조) |
| `game-design/` | 기획 문서 00~09 (엔진 선정 → 시스템 → 심화 → 배포 계획) |

## 핵심 특징

- **심연의 부름**: 보스 게이트는 선택 — 한 바퀴 더 돌수록 보상↑ 위험도 +28%
- RO 스타일 성장: 6스탯 배분 · 2차 전직 8종 · 선행 조건 스킬트리 · 6속성 상성
- 주사위 조작: 홀짝 부적, 주사위 장비 — 운을 통제하는 재미
- 모든 밸런스는 시뮬레이터(1,000런 단위)로 검증 후 반영

## 개발 워크플로

1. 규칙·수치 변경은 `prototype/DiceDungeon.Core/`에서 (단일 진실 공급원)
2. `dotnet run --project prototype/DiceDungeon.Sim -- selftest && ... -- jobs` 로 검증
3. 웹(`prototype-web/`)에 반영 → 커밋 → Pages 자동 배포
