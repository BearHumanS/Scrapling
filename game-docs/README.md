# 🎲 (가제) 다이스마블 — 도트 부루마블식 RPG

> 도트(픽셀아트) 그래픽의 **부루마블식 보드 + RPG**를 결합한 PC(Steam) 게임의
> 기획·개발 문서 모음입니다.

| 항목 | 내용 |
|------|------|
| **장르** | 보드게임(부루마블식) × RPG(전투·육성·스토리) |
| **플랫폼** | PC / Steam 우선 (추후 확장 여지) |
| **엔진** | Godot 4.x |
| **플레이 방식** | 단계적 — 싱글플레이(AI 상대) 우선 출시 → 온라인 멀티 확장 |
| **목표** | 본격 상업 출시 |
| **그래픽** | 도트(픽셀아트) |
| **언어** | 한국어 우선, 다국어 현지화 고려 |

---

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
| **03 기술** | [tech-stack](03-technical/tech-stack.md) | Godot 버전·플러그인·툴체인 |
| | [architecture](03-technical/architecture.md) | 씬/노드 구조·데이터 모델·세이브 |
| | [multiplayer-plan](03-technical/multiplayer-plan.md) | 싱글→멀티 단계적 네트워킹 설계 |
| **04 아트** | [art-style-guide](04-art/art-style-guide.md) | 해상도·팔레트·스프라이트·애니메이션 규격 |
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
| concept-brief | 🟡 초안 | v0.1 |
| PRD | 🟡 초안 | v0.1 |
| GDD | 🟡 초안 | v0.1 |
| board-system | 🟡 초안 | v0.1 |
| rpg-combat | 🟡 초안 | v0.1 |
| economy-progression | 🟡 초안 | v0.1 |
| narrative | 🟡 초안 | v0.1 |
| tech-stack | 🟡 초안 | v0.1 |
| architecture | 🟡 초안 | v0.1 |
| multiplayer-plan | 🟡 초안 | v0.1 |
| art-style-guide | 🟡 초안 | v0.1 |
| roadmap | 🟡 초안 | v0.1 |
| risk-register | 🟡 초안 | v0.1 |
| steam-release-plan | 🟡 초안 | v0.1 |
| monetization-marketing | 🟡 초안 | v0.1 |

> 상태 범례: 🟡 초안(작성중) · 🟠 리뷰 · 🟢 확정

### 문서 작성 규칙
- 모든 문서 상단에 메타 헤더(`상태 / 버전 / 최종수정일 / 작성자`) 표기
- 상호 참조는 상대경로 Markdown 링크 사용
- 확정이 필요한 항목은 각 문서 말미 **"결정 필요(Open Questions)"** 에 명시
- 큰 변경 시 버전(v0.x)과 최종수정일 갱신

---

_최종수정일: 2026-06-30 · 작성자: 기획팀_
