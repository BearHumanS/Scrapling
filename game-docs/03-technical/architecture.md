# 아키텍처 (Architecture)

> **상태:** 🟡 초안 · **버전:** v0.1 · **최종수정일:** 2026-06-30 · **작성자:** 개발팀
>
> 관련: [tech-stack](tech-stack.md) · [multiplayer-plan](multiplayer-plan.md) · [board-system](../02-gdd/board-system.md)

---

## 1. 설계 원칙

1. **데이터 주도(Data-driven)**: 칸·적·아이템·이벤트를 `Resource`로 외부화.
2. **로직과 표현 분리**: 게임 규칙(시뮬레이션)과 연출/애니메이션을 분리.
3. **결정적 코어(Deterministic core)**: 동일 입력→동일 결과. 멀티/리플레이/세이브
   안정성의 기반(난수는 시드 관리).
4. **상태 기계 기반 턴 진행**: 명확한 단계 전환으로 버그·동기화 리스크 감소.

## 2. 씬/노드 계층 (개략)

```
Main (AutoLoad 부트스트랩)
├── GameManager (게임 상태머신·턴 진행)        [싱글톤/AutoLoad]
├── BoardScene
│   ├── Board (TileMap/칸 노드)
│   ├── Tokens (플레이어 말)
│   └── BoardUI (HUD·주사위·자산 표시)
├── CombatScene (전투 진입 시 로드)
│   ├── Units (아군/적)
│   └── CombatUI (스킬바·턴 순서)
├── MetaScene (메타 강화·해금·도감)
└── UILayer (메뉴·팝업·이벤트 다이얼로그)
```

- **AutoLoad 싱글톤**(예): `GameState`(저장 상태), `EventBus`(시그널 허브),
  `AudioManager`, `Localization`, `RNG`(시드 난수).

## 3. 게임 상태 기계 (Turn State Machine)

```
[TurnStart] → [RollDice] → [Move] → [ResolveTile]
   → (Buy/Build | Combat | Shop | Event | Trap | ...)
→ [ActionPhase] → [TurnEnd] → (다음 플레이어 | 라운드 종료 체크)
→ [GameOver] (승/패 조건 충족 시)
```

- 각 상태는 진입/처리/종료가 명확. 전투는 별도 하위 상태기계(Combat FSM).
- 상태 전환은 `EventBus` 시그널로 UI/연출과 느슨하게 결합.

## 4. 데이터 모델 (Resource 기반)

| 리소스 | 내용 |
|--------|------|
| `BoardData` | 맵 메타, `TileData` 배열 |
| `TileData` | 칸 종류·파라미터(가격/적ID/이벤트ID/보상) |
| `EnemyData` / `EncounterData` | 적 스탯·패턴, 전투 구성 |
| `ItemData` / `EquipmentData` | 아이템·장비·희귀도·효과 |
| `SkillData` | 스킬 효과·비용·대상 |
| `ClassData` | 클래스 기본 스탯·스킬셋 |
| `EventData` / `QuestData` | 텍스트 이벤트·퀘스트·분기 |

> 런타임 상태(소유 영지, 인벤토리, 진행도)는 별도 **세이브 상태 객체**로 관리.

## 5. 세이브 / 로드

- **대상**: 현재 런 상태(보드·플레이어·인벤토리·진행), 메타 진행(해금·강화),
  설정(옵션·키매핑).
- **방식**: `Resource`/JSON 직렬화. **임시파일 쓰기 → 검증 → 원자적 교체**로
  손상 방지. 버전 필드로 마이그레이션 대응.
- **자동/수동 저장**: 턴 경계 자동 저장 + 수동 슬롯.

## 6. 난수(RNG) 관리

- 시드 기반 RNG 싱글톤. 런 시작 시 시드 기록 → 리플레이/디버그/세이브 일관성.
- 멀티 대비: 동기화가 필요한 난수는 권위 측(서버/호스트)에서 생성·전파.

## 7. 멀티플레이 대비 설계

- 싱글에서도 **입력→상태 변경**을 "명령(Command)" 형태로 처리하면 멀티 전환 용이.
- AI도 플레이어와 동일 인터페이스로 명령을 생성(로직 통일).
- 상세: [multiplayer-plan](multiplayer-plan.md).

## 8. 폴더 구조 (제안)

```
project/
├── autoload/        # 싱글톤
├── scenes/          # board, combat, meta, ui
├── scripts/         # 게임 로직(상태머신·시스템)
├── data/            # .tres/csv (칸·적·아이템·스킬·이벤트)
├── assets/          # art, audio, fonts
├── ui/              # 테마·컨트롤
└── tests/           # 단위 테스트
```

## 9. 결정 필요 (Open Questions)

- [ ] 명령(Command) 패턴 도입 범위(처음부터 vs 멀티 단계에서)
- [ ] 세이브 포맷(Resource 바이너리 vs JSON) 및 암호화/검증 수준
- [ ] AutoLoad 싱글톤 구성 최종안
- [ ] 데이터 정의 포맷(.tres vs CSV/JSON) 표준
