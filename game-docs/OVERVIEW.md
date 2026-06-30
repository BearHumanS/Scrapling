# 📋 프로젝트 한눈 요약 (Overview) — 순서대로 정리

> **상태:** 🟢 정리 · **버전:** v0.2 기준 · **최종수정일:** 2026-06-30 · **작성자:** 기획팀
>
> 전체 문서를 **00 → 06 순서**로 압축한 요약본. 상세는 각 문서 링크 참조.
> 문서 인덱스/읽는 순서는 [README](README.md).

---

## 0. 프로젝트 한 줄

> **도트 부루마블식 RPG** — 주사위로 보드를 돌며 땅을 사고, 칸마다 전투·모험으로
> 영웅을 키우는 PC(Steam) 게임.

### ✅ 확정 사항 (Decisions)
| 항목 | 결정 |
|------|------|
| 엔진 | **Godot 4.x** |
| 플레이 | **단계적** — 싱글(AI) 우선 → 온라인 멀티 후속 |
| 메인 구조 | **하이브리드** — 캠페인 + 로그라이트(공용 메타 성장) |
| RPG 결합 | **종합형** — 전투 + 스토리 + 육성 |
| 톤앤매너 | **혼합** — 명랑(기본) + 긴장(보스·위기) |
| 출시 | **얼리 액세스 → 1.0**, 본격 상업 출시 |
| 클래스(1차) | **전사 / 마법사 / 도적·상인** 3종 |

---

## 1. 문서별 핵심 요약 (순서대로)

### 00 · 비전
- **[concept-brief](00-vision/concept-brief.md)** — 피치, USP 5가지(보드=던전, 자산=전투력,
  로그라이트 메타성장, 낮은 진입장벽, 도트+1인~멀티), 레퍼런스, 가제 후보(다이스마블 등).

### 01 · PRD
- **[PRD](01-prd/PRD.md)** — 비전·KPI(위시리스트 1만+, 리뷰 "매우 긍정적"),
  범위(In/Out), MoSCoW, NFR, 릴리스 단계(프로토→슬라이스→MVP/EA→1.0).

### 02 · 게임 디자인 (핵심, 11개)
- **[GDD](02-gdd/GDD.md)** — 디자인 기둥 4, 코어/게임/메타 루프, 모드, 승패.
- **[board-system](02-gdd/board-system.md)** — 칸 11종, 주사위·이동, 땅·건물·통행료 + RPG 재해석.
- **[rpg-combat](02-gdd/rpg-combat.md)** — 클래스·스탯·턴제 전투(권장 A+B), 몬스터·보스, 장비.
- **[economy-progression](02-gdd/economy-progression.md)** — 재화 4종, 성장 4축, **자산↔전투력** 연결.
- **[narrative](02-gdd/narrative.md)** — 보드 대륙 세계관, 캐릭터, 퀘스트, 보드-스토리 연결.
- **[world-outline](02-gdd/world-outline.md)** — 1~3막 아웃라인, 지역 6곳, 진영/인물.
- **[act1-campaign](02-gdd/act1-campaign.md)** — 1막 비트시트, 이벤트 4종, 주사위 기사 3페이즈.
- **[event-pool](02-gdd/event-pool.md)** — 공용 이벤트 **24종**, 풀 가중치, 스키마.
- **[content-tables](02-gdd/content-tables.md)** — 클래스/적/아이템/스킬/칸/메타 데이터 초안.
- **[skill-trees](02-gdd/skill-trees.md)** — 클래스 3종 × 3분기 트리, 빌드 예시 5.
- **[balance-design](02-gdd/balance-design.md)** — 성장·적·전투·경제 **수식**과 시작 계수.

### 03 · 기술
- **[tech-stack](03-technical/tech-stack.md)** — Godot, GDScript 권장, GodotSteam, 빌드/CI.
- **[architecture](03-technical/architecture.md)** — 씬 계층, 턴 상태기계, 데이터주도, 세이브, 결정성.
- **[multiplayer-plan](03-technical/multiplayer-plan.md)** — 싱글→멀티 원칙(결정적·명령·권위), Steam 로비.
- **[prototype-scope](03-technical/prototype-scope.md)** — M0 범위·폴더·빌드 순서·DoD.

### 04 · 아트·사운드
- **[art-style-guide](04-art/art-style-guide.md)** — 해상도/팔레트/스프라이트, 톤 혼합 연출.
- **[sound-design](04-art/sound-design.md)** — 칩튠+오케스트라, BGM/SFX 리스트, 적응형 오디오.

### 05 · 프로덕션
- **[roadmap](05-production/roadmap.md)** — M0~M4 마일스톤, 검증 게이트, 병렬 트랙.
- **[risk-register](05-production/risk-register.md)** — 리스크 12종(R1 스코프, R3 밸런스, R4 결정성 등).

### 06 · 비즈니스
- **[steam-release-plan](06-business/steam-release-plan.md)** — Steamworks·스토어·등급·출시 체크리스트.
- **[monetization-marketing](06-business/monetization-marketing.md)** — 유료+EA, 위시리스트 우선 마케팅.

---

## 2. 콘텐츠/수치 스냅샷

| 영역 | 현재 정리값(초안) |
|------|------------------|
| 클래스 | 3종(전사/마법사/도적), 각 3분기 스킬 트리 |
| 적 | 슬라임·고블린·산적·골렘(엘리트)·주사위 기사(보스) |
| 이벤트 | 공용 풀 24종 + 1막 전용 4종 |
| 보드 | 28칸 예시(땅12/전투5/상점2/보물2/이벤트3/함정1/휴식1/출발1/보스1) |
| 막 | 1막(EA) → 2막(EA 업데이트) → 3막(1.0) |
| 핵심 계수 | EXP^1.5, 적 구역당 +25%, 통행료율 0.2, 자산→스탯 0.01 등(시작값) |

---

## 3. 통합 미결정 항목 (Open Questions) — 우선순위

### 🔴 즉시 결정 필요 (다른 작업 차단)
- [ ] **정식 타이틀** 확정(상표·Steam 중복 검색)
- [ ] **프로토타입 코드 위치**: 이 repo `game-prototype/` vs **별도 repo**
- [ ] **Godot 버전** 고정값, **GDScript vs C#** 최종
- [ ] **캠페인 vs 로그라이트 콘텐츠 분량 배분**(EA 진입 기준)

### 🟠 1차 개발 전 결정
- [ ] 전투 방식 최종(클래식 A 단독 vs A+B 혼합)
- [ ] 핵심 밸런스 계수(ASSET_TO_STAT 등) 1차 플레이테스트 값
- [ ] 승리 조건 표준(보스 격파 vs 자산 달성), 한 판 바퀴 수
- [ ] EA 진입 콘텐츠 분량(막 수·런 깊이), EA 기간

### 🟡 출시·확장 시점 결정
- [ ] 가격(EA가/1.0가), DLC·코스메틱 도입 여부
- [ ] 멀티플레이 착수 시점(EA 중 vs 1.0 후), 세션 모델·인원
- [ ] 데모/Next Fest 참여, 지원 언어 범위, 등급분류 국가
- [ ] 아트/사운드 제작 방식(내부 vs 외주 vs 라이선스), 한글 도트 폰트

---

## 4. 다음 단계 (순서)

1. **🔴 즉시 결정 항목** 확정 (특히 타이틀·코드 위치·엔진 버전)
2. **Godot M0 프로토타입 스캐폴딩** — [prototype-scope](03-technical/prototype-scope.md) §5 폴더 구조대로
3. **M0 코어 루프 구현 → 재미 검증 게이트** ([roadmap](05-production/roadmap.md))
4. 통과 시 **M1 Vertical Slice**(아트·사운드·UX 1구역 완성)

> ⚠️ 현재 클라우드 환경은 Godot 에디터 실행 검증 불가 → 스캐폴딩은 텍스트로 생성,
> 실제 실행·확인은 로컬 Godot에서 수행.
