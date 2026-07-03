# Unity 프로젝트 셋업 가이드 (Day 0 — 30분)

이 폴더는 로컬 Unity 프로젝트에 그대로 넣는 통합 패키지입니다. **씬을 코드로 자동 구성**하므로 프리팹/인스펙터 배선이 필요 없습니다 — 빈 씬에 스크립트 하나 붙이고 Play.

## 0. Unity 설치 (처음이라면)

1. https://unity.com/download 에서 **Unity Hub** 다운로드·설치
2. Unity Hub 실행 → 우측 상단에서 Unity 계정 로그인 (무료 Personal 라이선스 자동 활성화)
3. **Installs → Install Editor → Unity 6 LTS** (6000.x, LTS 표시된 것) 선택
4. 모듈 선택 (여기서 체크 안 하면 나중에 Hub에서 추가 가능):
   - ✅ **Android Build Support** (하위 항목 OpenJDK, Android SDK & NDK Tools 포함 전부)
   - ✅ **iOS Build Support** — 단, iOS **빌드 제출**은 macOS + Xcode에서만 가능.
     Windows 개발이라면: 개발·테스트는 Android로 진행하고, iOS 빌드는 Mac을 빌릴 때 몰아서 처리
5. 설치 완료까지 15~40분 (약 8GB) — 그동안 아래 1~2단계의 저장소 파일을 준비

## 1. 프로젝트 생성

1. Unity Hub → New Project → **2D (URP)** 템플릿, Unity 6 LTS
2. 프로젝트 이름: `DiceDungeon`
3. File → Build Settings → Android/iOS 전환은 나중에 (에디터에서 먼저 개발)
4. Edit → Project Settings → Player → Resolution: **Portrait 고정**

## 2. 파일 복사

```
이 저장소                                  → Unity 프로젝트
─────────────────────────────────────────────────────────
prototype/DiceDungeon.Core/**/*.cs        → Assets/_Project/Scripts/Core/
  (csproj, bin, obj 제외 — sync-core.sh 참고)
unity-integration/Assets/_Project/Scripts/Core/DiceDungeon.Core.asmdef
                                          → Assets/_Project/Scripts/Core/
unity-integration/Assets/_Project/Scripts/Game/*
                                          → Assets/_Project/Scripts/Game/
```

macOS/Linux라면 저장소 루트에서: `bash unity-integration/sync-core.sh <Unity프로젝트경로>`

## 3. 패키지 설치 (Window → Package Manager)

| 패키지 | 용도 | 시점 |
|--------|------|------|
| 2D Animation | AI 아트 본 리깅 (06 문서) | Phase 2 |
| PSD Importer | 파츠 분리 PSB 임포트 | Phase 2 |

Phase 1 스캐폴드는 추가 패키지 없이 동작합니다 (UGUI 내장만 사용, DOTween도 아직 불필요).

## 4. 실행

1. 새 씬 생성 (File → New Scene, Basic 2D)
2. 빈 GameObject 생성 → `GameBootstrap` 컴포넌트 추가
3. **Play** — 마을(캐릭터 선택/훈련) → 런(보드/주사위/전투) → 결산까지 전체 루프가 즉시 돌아갑니다

모든 그래픽은 임시(단색 사각형 + 유니코드 아이콘)입니다. Phase 1의 목적은 루프 검증이므로 이 상태로 재미를 먼저 확인하세요 (05-백로그 Week 6).

## 5. 구조

```
Assets/_Project/Scripts/
├─ Core/                  ← prototype에서 복사 (엔진 비의존 게임 규칙, 수정 금지)
│  └─ DiceDungeon.Core.asmdef
└─ Game/                  ← Unity 전용 (Presentation)
   ├─ DiceDungeon.Game.asmdef   (Core 참조)
   ├─ GameBootstrap.cs    씬 전체를 코드로 구성 (Canvas, 보드, HUD, 팝업)
   ├─ GameManager.cs      런 상태머신: 주사위→이동→타일→보스→하강/귀환
   ├─ BoardView.cs        24칸 사각 트랙 렌더링 + 말 이동 코루틴
   ├─ UiFactory.cs        UGUI 코드 생성 헬퍼 (패널/버튼/텍스트/바)
   └─ SaveService.cs      PlayerProfile JSON 저장 (persistentDataPath)
```

## 6. Core 수정 워크플로

밸런스·규칙 변경은 **이 저장소의 prototype/에서 먼저** 하고 시뮬레이터로 검증 → `sync-core.sh`로 Unity에 복사. Unity 쪽 Core를 직접 고치면 시뮬레이션과 어긋납니다.

## 7. 다음 단계 (05-백로그 매핑)

- Week 3: BattleSimulator 자동 결과를 수동 입력(공격/방어/포션 버튼)으로 교체
- Week 5: 주사위 3D 연출, 홀짝 부적, DOTween 도입
- Phase 2: 2D Animation 리깅 파이프라인 (06 문서)
