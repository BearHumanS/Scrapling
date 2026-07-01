# 밸런스 설계 (Balance Design)

> **상태:** 🟡 초안 · **버전:** v0.2 · **최종수정일:** 2026-07-01 · **작성자:** 기획팀
>
> v0.2: M0.5 프로토타입 실측 계수(§10) 추가.
>
> 상위: [GDD](GDD.md) · 관련: [economy-progression](economy-progression.md) · [rpg-combat](rpg-combat.md) · [content-tables](content-tables.md) · [skill-trees](skill-trees.md) · [round-tempo-system](round-tempo-system.md) · [invasion-combat](invasion-combat.md)
>
> ⚠️ 모든 계수는 **출발점(시작값)** 이며 플레이테스트로 보정한다. 목적은
> "튜닝 가능한 수식 골격"을 정의하는 것이지 최종 수치 확정이 아니다.

---

## 1. 밸런스 목표 (Design Targets)

| 지표 | 목표(예시) |
|------|-----------|
| 1막 캠페인 클리어 | 40~60분 |
| 로그라이트 1런 | 20~40분 |
| 일반 전투 길이 | 3~6턴 |
| 보스 전투 길이 | 8~15턴 |
| 한 판 내 파워 성장 | 약함→강함 약 5~8배 체감 |
| 첫 사망/패배 시점 | 신규 유저 2~3구역에서 첫 좌절 허용 |

## 2. 캐릭터 성장 곡선

### 2.1 레벨업 필요 경험치
```
needEXP(L) = round( BASE_EXP * L^EXP_POW )
   BASE_EXP = 50,  EXP_POW = 1.5   (시작값)
```
| Lv | 1→2 | 3→4 | 6→7 | 10→11 |
|----|-----|-----|-----|-------|
| needEXP(예) | 50 | 260 | 771 | 1,658 |

### 2.2 스탯 성장(레벨당)
```
stat(L) = base + growth * (L-1)
```
| 클래스 | HP/Lv | ATK/Lv | DEF/Lv |
|--------|-------|--------|--------|
| 전사 | +12 | +2 | +1.5 |
| 마법사 | +7 | +3 | +0.7 |
| 도적 | +9 | +2.2 | +1 |

> 최종 스탯 = 레벨성장 + 장비 + 영지버프 + 스킬 패시브.

## 3. 적 난이도 스케일

### 3.1 진행도 기반 스케일
```
enemyStat = baseStat * (1 + DIFF_K * progress) * tierMul
   progress = (현재 구역 인덱스 + 바퀴 수 보정)
   DIFF_K   = 0.25   (구역당 +25%, 시작값)
   tierMul  : 일반 1.0 / 엘리트 1.8 / 보스 3.0
```
### 3.2 모드 보정
```
finalEnemyStat = enemyStat * modeMul * roguelikeDepthMul
   modeMul       : 쉬움 0.8 / 보통 1.0 / 어려움 1.25
   roguelikeDepthMul = 1 + 0.15 * depth   (로그라이트 깊이당 +15%)
```
> 목표: 플레이어 파워 커브를 적이 살짝 뒤따라오게 → 성장 체감 유지.

## 4. 전투 피해 공식

```
rawDamage   = ATK * skillCoeff
mitigated   = rawDamage * (100 / (100 + DEF))      # 비율 감산(권장)
critical    = mitigated * (isCrit ? CRIT_MULT : 1) # CRIT_MULT = 1.5
finalDamage = round( max(1, critical) )

critChance  = clamp(BASE_CRIT + LUCK * CRIT_PER_LUCK, 0, CRIT_CAP)
   BASE_CRIT = 5%,  CRIT_PER_LUCK = 0.5%p,  CRIT_CAP = 50%
```
- **비율 감산** 채택 이유: 후반 DEF 인플레로 인한 무한 탱킹 방지(가산 대비 안정적).
- 상태이상 피해(화상/중독)는 최대HP 비례 또는 고정값(튜닝).

## 5. 경제 밸런스

### 5.1 통행료 / 영지 수입
```
toll(tile)   = basePrice * TOLL_RATE * buildingMul * setMul
   TOLL_RATE   = 0.2
   buildingMul : 빈땅 1.0 / 막사 1.5 / 요새 2.5 / 성 4.0
   setMul      : 같은 구역 풀세트 1.5
income(turn) = Σ(영지 basePrice * INCOME_RATE * buildingMul)
   INCOME_RATE = 0.05  (턴당)
```
### 5.2 자산 ↔ 전투력 환산 (핵심)
```
assetPower = Σ(영지 가치 * ASSET_TO_STAT) 를 스탯 버프로 분배
   ASSET_TO_STAT = 0.01   # 영지 100골드 가치당 +1 스탯포인트 상당
buff 분배: 건물 종류별로 ATK/DEF/HP/골드획득에 매핑(전사 C분기 등과 연계)
```
> **밸런스 원칙**: "경제 빌드"와 "전투 빌드"가 서로 다른 경로로 비슷한 총
> 전투력에 도달(±15% 이내)하도록 ASSET_TO_STAT와 적 스케일을 함께 조정.

### 5.3 골드 싱크 (인플레 통제)
- 후반 건설·강화·상점 가격을 progress에 비례 상승 → 잉여 골드 흡수.
```
shopPrice = baseItemPrice * (1 + PRICE_DRIFT * progress)
   PRICE_DRIFT = 0.2
```

## 6. 보상 곡선

```
goldReward(enemy)  = enemy.baseGold * (1 + 0.2*progress) * tierMul
expReward(enemy)   = enemy.baseExp  * (1 + 0.2*progress) * tierMul
dropChance(rarity) = 가중 테이블, LUCK 보정 + 연속 꽝 보호(pity)
```
| 희귀도 | 기본 드랍 가중(예) |
|--------|-------------------|
| 일반 | 60 |
| 고급 | 25 |
| 희귀 | 10 |
| 영웅 | 4 |
| 전설 | 1 |

- **Pity(보호)**: N회 연속 고급 이상 미획득 시 다음 드랍 등급 보장.

## 7. 메타 성장 밸런스

```
metaReward(run) = DEPTH_BASE*depth + BOSS_BONUS*bossKills + ASSET_BONUS*finalAssets
metaCost(node)  = baseCost * COST_GROWTH^tier   (COST_GROWTH = 1.6)
```
- **인플레 통제**: 상시 스탯 증가형은 완만하게, **해금형 위주**로 구성.
- 캠페인·로그라이트 공용 누적([content-tables](content-tables.md) §8).

## 8. 튜닝 워크플로우

1. 위 계수를 **데이터(CSV/Resource)** 로 외부화 → 코드 수정 없이 조정.
2. 스프레드시트에 곡선/시뮬레이션 모델 구성(레벨·적·경제 동시 검토).
3. 자동 시뮬(봇 플레이)로 1차 검증 → 사람 플레이테스트로 체감 보정.
4. 지표(클리어 시간·승률·사망 시점·골드 잔량) 로깅 → 이상치 추적.
5. 마일스톤마다 재튜닝([roadmap](../05-production/roadmap.md)).

## 9. 밸런스 파라미터 요약표 (시작값)

| 파라미터 | 시작값 | 영향 |
|----------|--------|------|
| BASE_EXP / EXP_POW | 50 / 1.5 | 레벨업 속도 |
| DIFF_K | 0.25 | 적 난이도 상승률 |
| TOLL_RATE / INCOME_RATE | 0.2 / 0.05 | 경제 회전 |
| ASSET_TO_STAT | 0.01 | 자산→전투력 |
| BASE_CRIT / CRIT_MULT / CRIT_CAP | 5% / 1.5 / 50% | 치명타 |
| PRICE_DRIFT | 0.2 | 골드 싱크 |
| COST_GROWTH | 1.6 | 메타 비용 곡선 |

## 10. M0.5 프로토타입 실측 계수 (구현 반영)

개별 보드 침공 슬라이스([`game-prototype-m05`](../../game-prototype-m05/README.md))에
현재 적용된 **시작 계수**. 로컬 플레이테스트로 조정한다.
관련 설계: [round-tempo-system](round-tempo-system.md), [invasion-combat](invasion-combat.md).

### 파밍 축적 (Balance.gd)
| 상수 | 값 | 의미 |
|------|----|------|
| `GEN_BASE` | 5.0 | 칸 축적 기본량/턴 |
| `K` | 0.5 | 개발도 가중(축적·수확) |
| `CAP_BASE` | 20.0 | 칸 축적 상한 기본 |
```
gen/턴 = GEN_BASE × tier × (1 + K × dev)
cap    = CAP_BASE × tier
착지: harvest = store(정수), store→0, dev+1
```

### 라운드/티어 (GameState.gd)
- 완주 1회 = 라운드 +1, `tier = 1 + round/3` (3라운드마다 +1).
- 라운드 3부터 침공 가능. 이동은 순수 주사위(1~6), **속도=운**.

### 골드 사용 (전략 배분)
| 행동 | 비용 | 효과 |
|------|------|------|
| 훈련 | 30G | 영웅 ATK +3, HP +15 |
| 사보타주 | 40G | AI 코어 HP -60(누적) |

### AI 보드 성장
| 항목 | 공식 |
|------|------|
| AI 라운드 | 플레이어 턴 5회마다 +1 |
| AI 티어 | `1 + ai_round/3` |
| 코어 HP | `max(20, 80×티어 + 15×라운드 − 사보누적)` |
| 수비병 | HP `40×티어`, ATK `8×티어` |
| 트랩 | 피해 `15×티어`, 개수 `min(라운드, 3)` |

### 전투(침공)
- 피해 = §4 비율 감산 공식(적 방어 5 고정, 영웅 DEF 8 기본).
- 침공: 트랩(피해)→수비병(전투)→코어(전투) 순차. 영웅 HP 0 = 실패(후퇴 시 절반 회복).

> **튜닝 관찰 포인트**: 조기 침공 성공률, 훈련↔사보타주 선택 빈도, "빠름"이
> 실제로 중립~불리인지(초반 취약 발현). 위 계수를 스프레드시트로 시뮬 후 보정.

## 11. 결정 필요 (Open Questions)

- [ ] 피해 공식: 비율 감산 vs 가산 감산 최종 채택
- [ ] ASSET_TO_STAT 등 핵심 계수 1차 플레이테스트 값
- [ ] 자동 시뮬레이션(봇) 도입 여부·범위
- [ ] 난이도 모드 수·로그라이트 깊이 상한
- [ ] Pity(드랍 보호) 임계값
