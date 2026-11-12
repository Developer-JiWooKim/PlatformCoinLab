# 포트폴리오 작업 기록

소개 문서(문제 해결 사례·성능 개선·추가 개선)의 원자료입니다. 사례마다 재현 조건·원인·수정·재검증과 증거 파일 위치를 남깁니다. 증거 파일은 `Docs/Evidence/`에 둡니다.

## 문제 해결 사례

### P1. 플레이어가 Ground 앞뒤(Z축)로 벗어남 — 직접 발견·수정

| 항목 | 내용 |
| :--- | :--- |
| 증상 | 위/아래(W/S, 스틱 상하) 입력을 계속하면 Player가 Ground 밖으로 나감. 좌우는 정상적으로 막힘 |
| 재현 조건 | Start 후 W 또는 S를 약 2초 이상 유지 (speed 5, Ground 20×20) |
| 원인 | `PlatformMover.Update`가 입력을 XZ 평면(`new Vector3(input.x, 0, input.y)`)으로 이동시키면서 제한은 `next.y`에 적용. Y는 이동하지 않으므로 제한이 무의미했고 Z는 제한이 없었음 |
| 수정 | `next.y = Mathf.Clamp(...)` → `next.z = Mathf.Clamp(next.z, -9f, 9f)` ([PlatformMover.cs](../Assets/MyAssets/Scripts/Ruintime/PlatformMover.cs)) |
| 재검증 | ☐ PC: W/S 유지 시 z=±9에서 정지 ☐ Android 터치 스틱 ☐ Web |
| 증거 | ☐ 수정 전 영상 (`git stash`로 되돌려 녹화) ☐ 수정 후 영상 |

### P2. Web 빌드 실패: `wasm-ld: undefined symbol: _emval_take_value / _emval_as` — 수업 중 해결 (커밋 ac3ca4a)

| 항목 | 내용 |
| :--- | :--- |
| 증상 | Web Player 빌드 시 링크 오류로 빌드 실패 |
| 원인 | `com.unity.ai.assistant`가 의존하는 `com.unity.dt.app-ui`의 사전 빌드 라이브러리(libAppUITextMateLib.a)가 구버전 Emscripten embind 심볼을 사용. Unity 6000.6의 Emscripten 4.x에서 해당 심볼이 제거됨 |
| 수정 | AI Assistant 패키지 제거, `Assets/Plugins/WebGL/EmvalCompat.jslib` 호환 함수 추가, Web 프로필 `webGLEmscriptenArgs: -lembind` |
| 재검증 | ☐ Web 빌드 성공 로그 ☐ 브라우저에서 한 판 완료 |
| 비고 | 외부 자료(오류 검색) 활용 여부와 출처를 소개 문서에 표시. jslib 주석의 `Assets/Editor/WebGLEmbindFix.cs`는 존재하지 않음(플래그는 빌드 프로필에 있음) → 주석 정리 필요 |

### P3. Web에서 원격 Addressables 로드 실패 — 수업 중 해결 (커밋 ac3ca4a)

| 항목 | 내용 |
| :--- | :--- |
| 증상 | Web Player에서 스테이지 로드 실패 |
| 원인 | (재확인 필요) 원격 번들이 `ServerData/WebGL`에 빌드되어 Web Player를 제공하는 서버 루트(Builds/Web) 밖에 있었음 |
| 수정 | RemotePractice의 Remote.BuildPath를 `ServerData/[BuildTarget]` → `Builds/Web/[BuildTarget]` |
| 재검증 | ☐ 원격 비교 절차에서 재현·기록 |

## 성능 개선

### O1. HUD 텍스트 매 프레임 갱신으로 인한 GC Alloc — 수업 재현 (DAY 14, 커밋 ed131e7)

| 항목 | 내용 |
| :--- | :--- |
| 변경 | `GameSession.RefreshUi`에 표시 값 캐시와 `ProfilerMarker("CoinLab.PresentUi")` 추가 |
| 측정 환경 | ☐ Windows Development Player, 해상도·VSync·플레이 구간 고정 |
| 수정 전/후 | ☐ GC Alloc/frame ☐ PresentUi ms ☐ GC.Collect 횟수 |
| 기능 유지 | ☐ 점수·시간·최고 점수 표시 정상 |

## 설정 변경 이력

- 2026-11-12: Addressables 활성 프로필 RemotePractice → Default, CourseStage 그룹 경로 Remote → Local, Build Remote Catalog 끔. 기본 게임을 서버 없이 실행하기 위함. RemotePractice 프로필은 원격 비교용으로 유지.
- 2026-11-12: ContentServer를 `Server/ContentServer/`로 재작성·이동 (포맷으로 원본 유실).
