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
| 증거 | ☐ 수정 전·후 캡처 (z 좌표가 ±9를 넘는지) |

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

### P4. 4:3(1024×768) 창에서 최고 점수·저장 버튼이 화면 밖으로 잘림 — 직접 분석, 해상도 고정으로 대응

| 항목 | 내용 |
| :--- | :--- |
| 증상 | PC 창 1024×768(4:3)에서 UI가 어긋나 보임. 16:9에서는 정상 |
| 재현 조건 | Windows Player, Windowed 1024×768 또는 1280×1024(5:4) |
| 원인 | BestText·SaveButton이 중앙 Anchor(0.5,0.5)에 x=590 고정 오프셋(오른쪽 끝 740). Canvas Scaler(1600×900, Match 0.5)에서 4:3일 때 Canvas 절반 너비 ≈ 692 < 740 → 잘림. HudPanel(y=370)도 중앙 기준이라 세로 비율 변화에 취약 |
| 수정 | PC 기본 해상도 1280×1024 → 1280×800(16:10), Windowed·크기 조절 불가 유지. UI Anchor는 변경하지 않음 |
| 재검증 | ☑ PC 16:10에서 정상 (사용자 확인) ☐ Android 20:9 실행 캡처 |
| 계산 근거 | 20:9(2400×1080) Canvas 1789×805 → HUD 상단 약 3단위 초과, 실사용 문제 없음. 4:3 태블릿 Canvas ≈1386×1039 → BestText·SaveButton 잘림 |
| 알려진 한계 | 4:3 비율 Android 태블릿은 미대응·미확인. 보완 계획: BestText·SaveButton을 오른쪽 위 Anchor(1,1)로, HudPanel을 위 중앙 Anchor로 변경 |

### 플랫폼 정책

- FramePolicy ([FramePolicy.cs](../Assets/MyAssets/Scripts/Ruintime/FramePolicy.cs)), GameSession 오브젝트에 부착: PC/Editor VSync 1, Android VSync 0 + targetFrameRate 30(Inspector 변경 가능) + 화면 꺼짐 방지, Web targetFrameRate -1. 수업 코드 대비 추가: Android FPS 직렬화, sleepTimeout, 적용 값 로그
- 확인: ☐ PC Player.log `[FramePolicy]` 줄 ☐ Android logcat ☐ Web 브라우저 콘솔

### P5. StageLoader 로딩 중 파괴 시 해제 누락·파괴된 객체 접근 가능성 — 직접 발견 (코드 검토)

| 항목 | 내용 |
| :--- | :--- |
| 증상(예상) | 스테이지 로딩 중 씬 종료·오브젝트 파괴 시, 완료 콜백이 파괴된 GameSession에 접근(MissingReferenceException)하고 인스턴스가 해제되지 않음 |
| 원인 | `ownerDestroyed` 필드가 선언만 되고 true로 바뀌는 곳이 없음. `OnDestroy`는 완료된 핸들만 해제 |
| 수정 | `OnDestroy`에서 `ownerDestroyed = true` 설정 → 늦게 끝난 로드는 콜백에서 해제만 수행 ([StageLoader.cs](../Assets/MyAssets/Scripts/Ruintime/StageLoader.cs)) |
| 재검증 | ☐ 원격 로드 지연 중 Play Mode 종료 → 오류 로그 없음 (서버 응답 지연 조건 필요) |

## 직접 추가·개선 기능

### D1. 스테이지 선택 (Addressables Label 기반 목록)

| 항목 | 내용 |
| :--- | :--- |
| 무엇 | 메뉴의 ◀ ▶로 스테이지를 골라 로드. 마지막 선택 저장 |
| 왜 | 수업 구현은 고정 주소 `stage-main` 하나만 로드. 콘텐츠를 늘릴 때마다 코드·Player를 고쳐야 함 |
| 어떻게 | `Addressables.LoadResourceLocationsAsync("stage")`로 라벨이 붙은 프리팹 주소를 모아 정렬(`List<string>`), 위치 핸들은 즉시 해제. 조회 실패·0개면 기본 주소로 대체. 선택 변경 시 이전 인스턴스 해제 → 새 주소 `InstantiateAsync`. 인덱스 순환 `(i + step + n) % n` |
| 원격 확장 | 원격 카탈로그 사용 시 서버에 라벨 붙은 새 스테이지를 배포하면 Player 재빌드 없이 목록에 추가됨 (☐ 원격 비교 절차에서 확인) |
| 코드 | `StageLoader.Start / OnLocationsCompleted / ChangeSelection / LoadStage` |
| 결과 | ☑ PC Player에서 stage-main·stage-a·stage-b 전환 동작 (2026-11-12) ☐ 캡처 |

### D2. 스테이지별 최고 점수와 기존 기록 이전

| 항목 | 내용 |
| :--- | :--- |
| 무엇 | 저장 키를 `PlatformCoinLab.Best.{address}`로 분리 |
| 왜 | 스테이지마다 난이도가 달라 기록을 하나로 비교하면 의미가 없음. 업데이트 후 기존 사용자 기록을 잃지 않아야 함 |
| 어떻게 | `GameSession.MigrateLegacyBest`: 이전 키가 있고 새 키가 없을 때만 `stage-main` 기록으로 복사 후 이전 키 삭제(1회 실행). `SetStageId`로 표시·저장 대상 전환, 플레이 중 변경 금지 |
| 결과 | ☑ PC Player에서 스테이지별 기록 동작 확인 ☐ 캡처 |

### D3. 터치 컨트롤 방식 전환 (조이스틱 ↔ 키패드)

| 항목 | 내용 |
| :--- | :--- |
| 무엇 | 버튼 하나로 OnScreenStick 조이스틱과 OnScreenButton 방향키(W/A/S/D)를 전환, 선택 저장 |
| 왜 | 수업 구현은 씬에서 한 방식만 켜 둠. 아날로그 이동과 정확한 4방향 이동의 선호가 사용자마다 다름 |
| 어떻게 | `MobileControlsToggle.Apply`에서 두 그룹을 SetActive로 전환. OnScreen 컨트롤은 OnDisable 시 입력을 기본값으로 되돌리므로 이동 중 전환해도 입력이 남지 않음. `PlatformCoinLab.TouchLayout`에 저장 |
| 결과 | ☑ PC Player에서 전환 동작 확인 ☐ Android 실기기 ☐ 캡처 |

## 성능 개선

### O1. HUD 텍스트 매 프레임 갱신으로 인한 GC Alloc — 수업 재현 (DAY 14, 커밋 ed131e7)

| 항목 | 내용 |
| :--- | :--- |
| 변경 | `GameSession.RefreshUi`에 표시 값 캐시(점수·초·최고점·상태 6개)와 `ProfilerMarker("CoinLab.PresentUi")` 추가. 값이 바뀐 프레임에만 SetActive·문자열 생성 수행 |
| 측정 환경 | 2026-11-12, Windows 10, Windows Development Player(IL2CPP, Autoconnect Profiler), 1280×800 Windowed, VSync 1(60Hz), 동일 스테이지. Load Stage → Clear → Start → 약 10초 이동·수집 |
| 수정 전 측정 방법 | `RefreshUi` 본문을 커밋 ed131e7^ 버전으로 임시 교체(마커 유지) 후 같은 설정으로 재빌드. 측정 후 `git checkout`으로 복원 |

| 지표 | 수정 전 (매 프레임 갱신) | 수정 후 (값 캐시) |
| :--- | :--- | :--- |
| PlayerLoop GC Alloc (플레이 중 임의 프레임) | **226 B** (Frame 1044/1633) | **0 B** (Frame 776/1319) |
| 환산 할당량 (60FPS) | 약 13.6 KB/s | 초가 바뀌는 프레임만 할당 |
| 프레임 CPU 시간 | 16.40 ms | 16.89 ms |

| 항목 | 내용 |
| :--- | :--- |
| 해석 | 두 빌드 모두 VSync 대기(약 13~14ms)가 프레임 대부분을 차지 → 이 게임은 60FPS 제한에 묶여 있어 CPU 시간으로는 차이가 드러나지 않음. 개선 효과는 **매 프레임 GC 할당 제거**(GC 수집 빈도·스파이크 위험 감소)로 설명 |
| 기능 유지 | ☑ 점수·시간·최고 점수 표시 정상 (수정 후 빌드에서 플레이 확인) |
| 증거 | `ProfilerCaptures/O1_before_every_frame.*`, `O1_after_uicache.*` (.data 각 139MB·111MB, Git 제외 → 제출 파일로 별도 전달). 캡처 이미지 2장 → ☐ `Docs/Evidence/`에 저장 |
| 측정 한계 | 각 1회 측정, 대표 프레임 1개 비교. `CoinLab.PresentUi` 마커 단독 값과 GC.Collect 횟수는 ☐ 미측정 (저장된 .data를 열어 Hierarchy에서 `PresentUi` 검색으로 보완 가능). Android·Web Player 미측정 |

### 추가 최적화 후보 (미진행)

- IDE 분석기 UNT0044: `hud.text = $"..."` 대신 `TMP_Text.SetText("Score: {0}  Time: {1}", ...)`를 쓰면 초가 바뀌는 프레임의 문자열 할당도 제거 가능

## 제출 전 체크

- PC 빌드 제출 시 `PlatformCoinLab_BackUpThisFolder_ButDontShipItWithYourGame` 폴더 제외 (IL2CPP 디버그 심볼)
- 측정용 Development 빌드(`Builds/Windows_Developement`)와 제출용 Release 빌드 구분
- Windows 빌드 프로필이 현재 Development·Autoconnect Profiler·Script Debugging 켜짐 상태 → 제출 빌드 전 끄기

## 설정 변경 이력

- 2026-11-12: Addressables 활성 프로필 RemotePractice → Default, CourseStage 그룹 경로 Remote → Local, Build Remote Catalog 끔. 기본 게임을 서버 없이 실행하기 위함. RemotePractice 프로필은 원격 비교용으로 유지.
- 2026-11-12: ContentServer를 `Server/ContentServer/`로 재작성·이동 (포맷으로 원본 유실).

## 원격 콘텐츠 확인 (REMOTE_CONTENT_GUIDE.md 절차, 2026-11-12)

환경: Windows 10, `Builds/WindowsRemote` (RemotePractice 프로필, Remote Catalog), ContentServer `https://localhost:5443`, FilesRoot `ServerData`

| # | 조건 | 결과 | 증거 |
| :--- | :--- | :--- | :--- |
| R1 | 서버 중지·캐시 없음 | ☑ 스테이지 목록 표시, `Stage load failed. Press Reload Stage.`, Start 잠금. Player.log: `Curl error 7 … Could not connect`, `ConnectionError : Cannot connect to destination host` (404 아님) | `R1_server_off.png`, `R1_player.log` |
| R2 | 서버 실행 후 Load Stage | ☑ `Stage ready. Press Start.`, 스테이지 로드, 한 판 완료·결과 화면. 서버: `GET /StandaloneWindows64/coursestage_…c8435….bundle -> 200 (11 ms)` (14:47:15) | `R2_recovered.png`, `remote_server.log` |
| R4 | StageC 추가 → Update a Previous Build → **Player 재빌드 없이** V1 재실행 | ☑ 목록 `/4`, stage-c 선택·로드·플레이. 서버: 시작 시 `catalog_0.1.0.hash` → `catalog_0.1.0.json` → 새 번들 `b478….bundle` 모두 200 (14:56:13) | `R4_stage_c_added.png`, `remote_server.log` |

- 관찰: CourseStage는 Pack Together라 스테이지 하나 추가에도 그룹 번들 전체(73.5KB, 기존 스테이지 포함)를 다시 받음. 개선 방향: 스테이지별 Pack Separately → 새 스테이지만 다운로드 (미적용)
- R3(캐시 로드)·R5/R6(번들 404·복구)는 미진행

### 원격 JSON 설정 비교 (Builds/WindowsRemoteConfig, Remote Url 고정·서버 파일만 교체)

| # | 서버의 platform-config.json | 화면 | Player.log | 서버 로그 | 증거 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| C1 | 원본 `{"roundSeconds":45}` | ☑ `Next round: 45 seconds`, 타이머 45부터 시작 | 경고 없음 | 200 | `C1_config_45.png` |
| C2 | 문법 오류 | ☑ 30초 | `Invalid JSON: … Missing a comma or '}'` | 200 | `C2_config_broken.png` |
| C3 | 범위 밖 500 | ☑ 30초 | `Invalid roundSeconds` | 200 | `C3_config_range.png` |
| C4 | 파일 없음 | ☑ 30초 | `Config failed: HTTP/1.1 404 Not Found` | 404 | `C4_config_404.png` |
| C5 | 서버 중지 | ☑ 30초 | `Config failed: Cannot connect to destination host` | 없음 | `C_config_player.log` |

- 화면은 C2~C5가 모두 같은 `30 seconds` → 실패 원인 구분은 로그로 제시. 소개 문서에는 대표 화면 1장 + 로그 표 사용
- 개선 방향(미적용): Status에 실패 사유 표시(예: `Settings invalid (JSON) → 30s`). 적용 시 3개 플랫폼 제출 빌드 재생성 필요
