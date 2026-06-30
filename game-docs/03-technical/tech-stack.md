# 기술 스택 (Tech Stack)

> **상태:** 🟡 초안 · **버전:** v0.1 · **최종수정일:** 2026-06-30 · **작성자:** 개발팀
>
> 관련: [architecture](architecture.md) · [multiplayer-plan](multiplayer-plan.md)

---

## 1. 엔진

- **Godot 4.x** (안정/LTS 라인 채택 권장).
  - 2D·도트 친화(픽셀 스냅, 정수 스케일, TileMap), 오픈소스, 로열티 없음.
  - 내장 High-Level Multiplayer로 후속 멀티 확장 용이.
- **버전 고정**: 프로젝트에 Godot 버전 명시, 팀 전원 동일 버전 사용.

## 2. 스크립트 언어 — GDScript vs C#

| 기준 | GDScript | C# |
|------|----------|-----|
| 생산성/엔진 통합 | ◎ 빠른 반복, 엔진 1급 시민 | ○ |
| 성능 | ○ (대부분 충분) | ◎ 무거운 연산 유리 |
| 생태계/도구 | ○ | ◎ (라이브러리·테스트) |

> **권장: GDScript를 기본**으로 빠르게 개발, 성능 병목(전투 시뮬레이션·AI 등)은
> 필요 시 부분 최적화. 팀 역량이 C#에 강하면 C# 채택도 가능(혼용은 지양).

## 3. 핵심 시스템/애드온 후보

| 영역 | 선택지 | 비고 |
|------|--------|------|
| Steam 연동 | **GodotSteam** | 실적·클라우드·(후속)멀티 로비 |
| 저장/세이브 | Godot `Resource`/JSON 직렬화 | 손상 방지 저장 패턴 적용 |
| 현지화 | Godot 내장 i18n(번역 CSV/PO) | 번역키 외부화 |
| 입력 | Godot InputMap | 키/패드 리매핑 |
| 사운드 | Godot Audio Bus | BGM/SFX 믹싱 |
| UI | Godot Control + 테마 | 도트 UI 테마 |
| 디버그/치트 | 자체 콘솔(개발 빌드 전용) | QA 효율 |

> 서드파티 애드온은 라이선스·유지보수 상태 확인 후 채택([risk-register](../05-production/risk-register.md)).

## 4. 데이터 파이프라인

- **게임 데이터**: 칸/적/아이템/스킬/이벤트를 `Resource`(.tres) 또는 CSV/JSON으로
  정의 → 코드와 분리, 밸런싱·현지화 용이.
- **밸런스 작업**: 스프레드시트 → 변환 스크립트 → 게임 데이터(가능 시 자동화).

## 5. 버전 관리 & 협업

- **Git** + 원격 저장소. 대용량 에셋은 Git LFS 검토.
- **브랜치 전략**: `main`(안정) / 기능 브랜치 / PR 리뷰.
- **.gitignore**: Godot `.godot/`, 임포트 캐시, 빌드 산출물 제외.
- **컨벤션**: 커밋 메시지·코드 스타일 가이드 문서화.

## 6. 빌드 & CI/CD

- **빌드 타깃**: Windows 우선, macOS/Linux 검토.
- **자동화**: Godot 헤드리스 export로 CI 빌드, Steam(steamcmd)으로 업로드.
- **버전닝**: 시맨틱 버전 + 빌드 번호, 변경 로그 유지.

## 7. 테스트 & 품질

- 핵심 로직(전투 계산·경제·세이브) 단위 테스트(GUT 등 GDScript 테스트 프레임워크).
- 결정적 로직 보장(멀티 대비) → [architecture](architecture.md), [multiplayer-plan](multiplayer-plan.md).
- 플레이테스트 빌드 배포(내부/클로즈드).

## 8. 결정 필요 (Open Questions)

- [ ] GDScript vs C# 최종 결정
- [ ] Godot 정확한 버전(LTS) 고정값
- [ ] GodotSteam 등 애드온 채택 확정 및 라이선스 검토
- [ ] Git LFS 사용 여부 / 저장소 구조(게임을 별도 repo로 분리할지)
- [ ] CI 환경(러너) 선택
