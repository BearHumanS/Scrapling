# 다이스 던전 — Core 로직 프로토타입 + 밸런스 시뮬레이터

Unity에 넣기 전 단계의 **엔진 비의존 게임 규칙 구현체**입니다. 03-기술설계의 "Core/Presentation 분리" 원칙에 따라, 이 코드가 그대로 Unity의 `Assets/_Project/Scripts/Core/`가 됩니다 (netstandard2.1 → Unity 호환).

## 구성

```
DiceDungeon.Core/            게임 규칙 (Unity로 그대로 이식)
 ├─ Rng.cs                   시드 기반 결정론 RNG
 ├─ Data/Balance.cs          밸런스 상수 — 단일 진실 공급원
 ├─ Board/Dice.cs            주사위 (더블·럭키세븐·홀짝 부적)
 ├─ Board/Tile.cs            타일 8종 + 점령 상태
 ├─ Board/FloorGenerator.cs  층 생성 (상점·쉼터 보장 + 비율 랜덤)
 ├─ Battle/BattleSimulator.cs 속도 기반 턴제 전투 + 보드 연동(더블 선제, 럭키세븐 크리)
 └─ Run/RunController.cs     런 오케스트레이션 (층→주사위→타일→보스→하강/귀환)

DiceDungeon.Sim/             콘솔 시뮬레이터 (밸런스 QA — 1인 개발의 플레이테스터 대체)
```

## 실행

```bash
dotnet run --project DiceDungeon.Sim              # 1,000런 통계 (사망 분포, 벽 위치)
dotnet run --project DiceDungeon.Sim -- meta      # 메타 성장 단계별 도달 층 곡선
dotnet run --project DiceDungeon.Sim -- selftest  # 코어 로직 자가 검증 (6항목)
```

## 현재 밸런스 상태 (v3, 검증 완료)

| 지표 | 결과 |
|------|------|
| 1층 사망률 | 0% (튜토리얼 보호: 1~2층 몬스터 1마리) |
| 캐릭터 4종 평균 도달 (메타 0) | 도적 5.7 / 기사 6.1 / 성직자 6.4 / 마법사 6.9층 (±10%) |
| 경제 (80런 탐욕 구매 여정) | 평균 6.9층 → 11.8층, 유물 싱크 소진되지 않음 |
| 자가 검증 | selftest 9항목 통과 (결정론·튜토리얼·상태이상·부활·경제 차감) |

v1(1층 사망 16%) → v2(스킬 없는 기본 킷) → v3(캐릭터·스킬 도입 후 재상향) — 이력은 `Balance.cs` 주석 참조.
`prototype-web/index.html`은 **v2 밸런스의 플레이어블 웹 버전** (스킬 없는 코어 루프 검증용 — 폰 브라우저에서 바로 열림).

## Unity 이식 절차 (Phase 1, Day 0)

1. `DiceDungeon.Core/`의 `.cs` 파일을 `Assets/_Project/Scripts/Core/`로 복사
2. 해당 폴더에 asmdef 생성 (이름 `DiceDungeon.Core`, Engine 참조 불필요)
3. Sim의 SelfTest를 Unity Test Runner EditMode 테스트로 이식
4. Presentation은 `RunController`를 직접 쓰지 않고, 각 단계(주사위→이동→타일)를 이벤트로 노출하는 래퍼를 만들어 연출과 동기화 (05-백로그 Week 1~2)
