# ⚔ 다이스마블 M0.5 — 침공 슬라이스 (Godot)

> **개별 보드 + 침공형** 방향의 재미 검증용 슬라이스.
> 설계: [round-tempo-system](../game-docs/02-gdd/round-tempo-system.md) ·
> [invasion-combat](../game-docs/02-gdd/invasion-combat.md) ·
> [proposal-dual-board](../game-docs/02-gdd/proposal-dual-board.md)
>
> 공유보드 M0(부루마블 코어)는 [`../game-prototype`](../game-prototype/README.md) 에 보존.

## 검증 질문 (재미 게이트)

> **"내 보드를 파밍해 전투력을 키우고 → AI 보드를 침공하는" 루프가 재미있는가?
> '빨리 도는 게 늘 좋지는 않다'(초반 취약)가 실제로 느껴지는가?**

## 실행 방법

이 환경에선 Godot 실행 검증 불가 → **로컬**에서:
1. Godot **4.3+** 설치.
2. Import → 이 `game-prototype-m05/` 의 `project.godot`.
3. ▶ 실행(F5).

## 플레이 루프

1. **주사위 굴리기(파밍)**: 매 턴 모든 칸에 자원이 축적(상한까지)되고,
   착지한 칸에서 수거 + 그 칸 개발도↑. 누적 파밍이 곧 **영웅 전투력**.
2. **완주 = 라운드 +1**, **3라운드마다 티어업**(수확량↑). 속도는 **순수 주사위 운**.
3. **라운드 3부터 "AI 보드 침공"** 버튼 활성. AI 보드는 라운드에 따라
   **트랩·수비병·코어HP**가 성장.
4. **침공**: 영웅이 트랩→수비→코어를 순차 돌파(▶진격). 코어 격파 시 **클리어**,
   실패 시 후퇴(절반 회복) 후 재파밍(AI는 계속 강해짐).

## 핵심 긴장 (설계 의도)

- **일찍 침공**: AI가 얇아 코어HP 낮음 → 쉬움. 단 내 영웅도 파밍이 적어 약함.
- **늦게 침공**: 내 전투력↑ 이지만 AI 방어도 두꺼워짐.
- **빨리(운 좋게) 돌면**: 티어는 빨리 오르나 축적이 얇아 **영웅이 약함** →
  섣부른 침공은 실패. → "빠름 = 항상 좋음"이 아님을 체감.

> 화면 상단 힌트: "지금 침공 시 코어HP vs 내 ATK"로 타이밍 판단.

## 폴더 구조

```
game-prototype-m05/
├── project.godot
├── autoload/   EventBus, RNG, GameState(보드·라운드·티어·AI방어)
├── scenes/     Main(.tscn/.gd), BoardView(파밍), InvasionView(침공)
└── scripts/    FarmTile, HeroState, Balance(축적/피해 수식)
```

## 튜닝 포인트 (Balance.gd / GameState.gd)

- `GEN_BASE·K·CAP_BASE`(축적), 영웅 전투력 환산(`_recompute_hero`),
  AI 성장(`ai_*`), AI 라운드 페이스(`turn_count % 5`).

## M0.5 확인 체크리스트 (로컬)

- [ ] 파밍으로 영웅 ATK/HP가 오른다.
- [ ] 완주 시 라운드↑, 3라운드에 티어↑로 수확이 커진다.
- [ ] 라운드 3부터 침공 가능, 침공이 트랩→수비→코어로 진행된다.
- [ ] 너무 일찍(약할 때) 침공하면 실패한다 = 초반 취약 체감.
- [ ] "언제 침공할지" 타이밍 고민이 재미있는가? (핵심 판단)

## 제약

- 회색박스/텍스트 UI, 밸런스 미튜닝, 트랩 심기(공격적 설치)·멀티는 범위 밖.
- 로컬 Godot에서만 실행/검증.
