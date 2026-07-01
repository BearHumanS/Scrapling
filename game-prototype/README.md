# 🎲 다이스마블 M0 프로토타입 (Godot)

> 도트 부루마블식 RPG의 **코어 루프 검증용 M0 프로토타입**.
> 기획 문서: [`../game-docs`](../game-docs/README.md) · 범위 정의: [prototype-scope](../game-docs/03-technical/prototype-scope.md)

## 목적 (검증 질문)

> **"주사위 → 이동 → 칸 이벤트 → (구매/전투) → 정산"의 한 바퀴가 돌아가는가?"**

완성도가 아니라 **코어 재미 검증**이 목적이다. 회색박스/텍스트 UI.

## 실행 방법

이 클라우드 환경에서는 Godot 에디터 실행/검증이 불가하다. **로컬에서** 실행하라.

1. **Godot 4.3+** 다운로드 (https://godotengine.org).
2. Godot 실행 → **가져오기(Import)** → 이 `game-prototype/` 폴더의 `project.godot` 선택.
3. 상단 ▶(실행) 버튼 또는 `F5`.
4. 화면의 **주사위 굴리기** 버튼으로 플레이. 전투 칸에서 공격/강타/방어 선택.

## 조작

- **주사위 굴리기**: 내 턴에 이동.
- **구매 / 구매 안 함**: 빈 땅 도착 시.
- **공격 / 강타(EN2) / 방어(+EN)**: 전투 중. AI 라이벌 턴은 자동 진행.
- 3바퀴를 먼저 도는 순간 종료 → **자산(골드+보유 땅 가치)** 비교로 승자 판정.

## 확정한 기본값 (M0)

| 항목 | 값 |
|------|-----|
| 엔진/언어 | Godot 4.3 / GDScript |
| 보드 | 16칸(출발1·땅8·전투3·보물2·쉼터2) |
| 플레이어 | 사람 1 + AI 라이벌 1 |
| 클래스 | 전사 고정(HP120/ATK14/DEF10) |
| 적 | 슬라임(HP30) |
| 종료 | 3바퀴 후 자산 비교 |
| RNG | 고정 시드(결정적) |

## 폴더 구조

```
game-prototype/
├── project.godot          # 프로젝트/오토로드 설정
├── autoload/              # EventBus, RNG, GameState(코어 규칙)
├── scenes/               # Main(.tscn/.gd), BoardView, CombatView (UI는 코드 구성)
└── scripts/              # TileData, EnemyData, PlayerState, Balance, AIController, TurnStateMachine
```

> M0에서는 안정성을 위해 Board/Combat 화면을 **코드로 구성**했다(.tscn 최소화).
> M1에서 씬(.tscn)·도트 아트로 분리·교체 예정.

## M0 완료 기준 (DoD) — 로컬 확인용 체크리스트

- [ ] 한 판이 처음~끝까지 끊김 없이 진행된다.
- [ ] 땅 구매·통행료로 골드가 변동한다.
- [ ] 전투 진입→승패→보드 복귀가 동작한다.
- [ ] AI 라이벌과 턴이 교대된다.
- [ ] 3바퀴 후 자산 비교로 승자가 판정된다.

## 다음 단계 (M1)

- 씬/도트 아트/사운드로 1구역 완성, 이벤트·상점·다중 클래스 단계 추가.
- 상세: [roadmap](../game-docs/05-production/roadmap.md), [act1-campaign](../game-docs/02-gdd/act1-campaign.md).

## 알려진 제약

- 텍스트/회색박스 UI(아트 미적용).
- 세이브/로드·현지화·밸런스 튜닝 없음(M0 범위 밖).
- 로컬 Godot에서만 실행/검증 가능.
