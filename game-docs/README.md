# 🎲 (가제) 다이스마블 — 도트 부루마블식 RPG

> 도트(픽셀아트) 그래픽의 **부루마블식 보드 + RPG**를 결합한 PC(Steam) 게임의
> 기획·개발 문서 모음입니다.

| 항목 | 내용 |
|------|------|
| **장르** | 보드게임(부루마블식) × RPG(전투·육성·스토리) |
| **플랫폼** | PC / Steam 우선 (추후 확장 여지) |
| **엔진** | Godot 4.x |
| **플레이 방식** | 단계적 — 싱글플레이(AI 상대) 우선 출시 → 온라인 멀티 확장 |
| **메인 구조** | 하이브리드 — 캠페인 + 로그라이트(공용 메타 성장) |
| **톤앤매너** | 혼합 — 명랑(기본) + 긴장(보스·위기) |
| **목표** | 본격 상업 출시 (얼리 액세스 → 1.0) |
| **그래픽** | 도트(픽셀아트) |
| **언어** | 한국어 우선, 다국어 현지화 고려 |

---

## ⭐ 빠른 시작

전체를 한눈에 보려면 **[OVERVIEW (프로젝트 한눈 요약)](OVERVIEW.md)** 부터 — 확정 사항,
문서별 핵심, 콘텐츠/수치 스냅샷, 통합 미결정 항목을 순서대로 정리했습니다.

▶ **프로토타입(Godot, 로컬 4.3+ 실행)**
> - **M0**(공유보드 부루마블, 레퍼런스): [`../game-prototype`](../game-prototype/README.md)
> - **M0.5**(개별보드 파밍→침공, 현 방향): [`../game-prototype-m05`](../game-prototype-m05/README.md)

## 📂 문서 맵

기획 → 설계 → 개발 → 아트 → 프로덕션 → 비즈니스 순서로 정리되어 있습니다.

| 분류 | 문서 | 설명 |
|------|------|------|
| **00 비전** | [concept-brief](00-vision/concept-brief.md) | 1페이지 컨셉 / 엘리베이터 피치 / 가제 |
| **01 PRD** | [PRD](01-prd/PRD.md) | 제품 요구사항 문서 (핵심) |
| **02 게임 디자인** | [GDD](02-gdd/GDD.md) | 게임 디자인 마스터 문서 |
| | [board-system](02-gdd/board-system.md) | 보드/칸/이동/땅·건물 시스템 |
| | [rpg-combat](02-gdd/rpg-combat.md) | 전투/스킬/캐릭터 클래스 |
| | [economy-progression](02-gdd/economy-progression.md) | 경제·재화·육성·밸런스 |
| | [narrative](02-gdd/narrative.md) | 세계관/스토리/퀘스트/NPC |
| | [world-outline](02-gdd/world-outline.md) | 세계관 설정·막(Act) 아웃라인·지역 카탈로그 |
| | [act1-campaign](02-gdd/act1-campaign.md) | 1막 시나리오·비트시트·이벤트 스크립트·보스전 |
| | [event-pool](02-gdd/event-pool.md) | 로그라이트/캠페인 공용 이벤트 풀(24종) |
| | [content-tables](02-gdd/content-tables.md) | 클래스·적·아이템·스킬·이벤트 데이터 |
| | [skill-trees](02-gdd/skill-trees.md) | 클래스별 스킬 트리·빌드 가이드 |
| | [balance-design](02-gdd/balance-design.md) | 성장/적 스케일/경제 수식·튜닝 파라미터 |
| | [ux-screens](02-gdd/ux-screens.md) | 화면 흐름·와이어프레임·UX 원칙 |
| | 🟠 [proposal-dual-board](02-gdd/proposal-dual-board.md) | **[채택]** 개별 보드 파밍·침공·방해 설계안 + 의사결정 분석 |
| | 🟠 [round-tempo-system](02-gdd/round-tempo-system.md) | 라운드(1완주=1R)·속도=운·타일 축적/티어업 |
| | 🟠 [invasion-combat](02-gdd/invasion-combat.md) | 침공 전투·트랩·수비 유닛·코어 |
| **03 기술** | [tech-stack](03-technical/tech-stack.md) | Godot 버전·플러그인·툴체인 |
| | [architecture](03-technical/architecture.md) | 씬/노드 구조·데이터 모델·세이브 |
| | [multiplayer-plan](03-technical/multiplayer-plan.md) | 싱글→멀티 단계적 네트워킹 설계 |
| | [prototype-scope](03-technical/prototype-scope.md) | M0 프로토타입 범위·폴더 구조·작업 순서·DoD |
| **04 아트·사운드** | [art-style-guide](04-art/art-style-guide.md) | 해상도·팔레트·스프라이트·애니메이션 규격 |
| | [sound-design](04-art/sound-design.md) | BGM/SFX·적응형 오디오·에셋 규격 |
| **05 프로덕션** | [roadmap](05-production/roadmap.md) | 마일스톤·일정 |
| | [risk-register](05-production/risk-register.md) | 리스크 식별·완화책 |
| **06 비즈니스** | [steam-release-plan](06-business/steam-release-plan.md) | Steamworks 셋업·출시 체크리스트 |
| | [monetization-marketing](06-business/monetization-marketing.md) | 가격/수익화·마케팅·커뮤니티 |

---

## 🧭 읽는 순서 (추천)

1. **처음 오셨다면** → [concept-brief](00-vision/concept-brief.md) 로 큰 그림 파악
2. **무엇을 만드는가** → [PRD](01-prd/PRD.md) 로 목표·범위·릴리스 단계 확인
3. **어떻게 노는가** → [GDD](02-gdd/GDD.md) 및 02 하위 문서로 게임 시스템 파악
4. **어떻게 만드는가** → 03 기술 문서로 구현 방향 확인
5. **언제·어떻게 출시하는가** → 05 프로덕션, 06 비즈니스 문서

---

## 📌 문서 상태표

| 문서 | 상태 | 버전 |
|------|------|------|
| concept-brief | 🟠 리뷰 | v0.2 |
| PRD | 🟠 리뷰 | v0.2 |
| GDD | 🟠 리뷰 | v0.2 |
| board-system | 🟡 초안 | v0.1 |
| rpg-combat | 🟡 초안 | v0.1 |
| economy-progression | 🟠 리뷰 | v0.2 |
| narrative | 🟠 리뷰 | v0.2 |
| world-outline | 🟡 초안 | v0.1 |
| act1-campaign | 🟡 초안 | v0.1 |
| event-pool | 🟡 초안 | v0.1 |
| content-tables | 🟡 초안 | v0.1 |
| skill-trees | 🟡 초안 | v0.1 |
| balance-design | 🟡 초안 | v0.2 |
| ux-screens | 🟡 초안 | v0.1 |
| proposal-dual-board | 🟠 채택·검토 | v0.2 |
| round-tempo-system | 🟠 설계·검토 | v0.3 |
| invasion-combat | 🟠 설계·검토 | v0.1 |
| tech-stack | 🟡 초안 | v0.1 |
| architecture | 🟡 초안 | v0.1 |
| multiplayer-plan | 🟡 초안 | v0.1 |
| prototype-scope | 🟡 초안 | v0.1 |
| art-style-guide | 🟠 리뷰 | v0.2 |
| sound-design | 🟡 초안 | v0.1 |
| roadmap | 🟠 리뷰 | v0.2 |
| risk-register | 🟡 초안 | v0.1 |
| steam-release-plan | 🟠 리뷰 | v0.2 |
| monetization-marketing | 🟠 리뷰 | v0.2 |

> 상태 범례: 🟡 초안(작성중) · 🟠 리뷰 · 🟢 확정

### 문서 작성 규칙
- 모든 문서 상단에 메타 헤더(`상태 / 버전 / 최종수정일 / 작성자`) 표기
- 상호 참조는 상대경로 Markdown 링크 사용
- 확정이 필요한 항목은 각 문서 말미 **"결정 필요(Open Questions)"** 에 명시
- 큰 변경 시 버전(v0.x)과 최종수정일 갱신

---

_최종수정일: 2026-06-30 · 작성자: 기획팀_
