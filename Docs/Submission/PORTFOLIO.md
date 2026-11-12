# PlatformCoinLab 프로젝트 소개

![Web 플레이 화면](Evidence/web_play.png)

## 1. 프로젝트 소개

| 항목 | 내용 |
| :--- | :--- |
| 게임 이름 | PlatformCoinLab |
| 장르 | 3D 탑다운 코인 수집 타임어택 |
| 대상 플랫폼 | PC (Windows), Android, Web |
| 핵심 플레이 | 메뉴에서 스테이지를 고른 뒤, 제한 시간 동안 바닥 위를 이동하며 코인을 모은다. 시간이 끝나면 결과와 스테이지별 최고 점수를 보여 주고, 다시 시작하거나 메뉴로 돌아간다. |

게임 흐름은 `메뉴 → 스테이지 로드 → Start → 수집·시간 감소 → 시간 종료 → 결과 → Restart 또는 Menu`이다. 기본 게임은 서버 없이 로컬 콘텐츠로 실행된다.

## 2. 개발 정보

| 항목 | 내용 |
| :--- | :--- |
| Unity | 6000.6.7f1, Universal 3D 템플릿 |
| 주요 패키지 | Addressables 2.11.2, Input System 1.20.1, Universal RP 17.6.0, uGUI 2.6.0 (TextMesh Pro 포함) |
| 서버 | ASP.NET Core (.NET SDK 10.0.403), localhost 개발용 HTTPS 인증서 |
| 스크립팅 백엔드 | IL2CPP (PC·Android·Web) |

## 3. 실행 자료

| 자료 | 위치 |
| :--- | :--- |
| 대표 이미지 | `Evidence/web_play.png` |
| PC 실행 파일 | `Builds/PC/PlatformCoinLab.exe` (1280×800 창 모드) |
| Android | `Builds/Android/PlatformCoinLab_Android.apk` |
| Web | `Builds/Web/` (서버로 제공해야 실행됨, 9장 참고) |
| Unity 프로젝트 | `UnityProject/` (Assets·meta·Packages·ProjectSettings) |
| 파일 서버 | `ContentServer/` (소스·Content·README) |
| 원격 콘텐츠·설정 절차 | `REMOTE_CONTENT_GUIDE.md` |
| 확인 자료 | `Evidence/` (캡처·서버 로그·Player 로그·Profiler 데이터) |
| 제출 버전 | `VERSION.txt` (커밋 해시·빌드 시각) |

## 4. 주요 구현 기능

수업에서 따라 구현한 기능이며, 직접 추가하거나 고친 부분은 5장과 6장에 따로 정리했다.

### 4-1. 라운드 기록과 중복 수집 방지 — `CoinRound`

- **무엇**: 점수, 남은 시간, 수집한 코인 ID를 관리하는 순수 C# 클래스이다.
- **왜**: 같은 코인과 여러 번 접촉하거나 시간이 끝난 뒤 접촉해도 점수가 오르면 안 된다. 또한 Unity와 분리해 두어야 규칙만 따로 검증할 수 있다.
- **어떻게**: 수집한 코인 ID를 `HashSet<int>`에 저장한다. `TryCollect`는 `IsFinished || !collectedIds.Add(id)` 조건으로 이미 수집한 코인과 종료 후 수집을 거절한다. `Tick`은 음수·NaN·무한대 경과 시간을 예외로 막고, 남은 시간이 0 아래로 내려가지 않게 한다. `Reset`은 ID 집합을 비워 다음 라운드에 같은 코인을 다시 모을 수 있게 한다.
- **코드**: [CoinRound.cs](../../Assets/MyAssets/Scripts/CoinRound.cs)
- **결과**: 같은 코인을 다시 밟아도 점수가 한 번만 오른다. Restart 후에는 다시 수집할 수 있다.

### 4-2. 게임 흐름과 시작 잠금 — `GameSession`

- **무엇**: 메뉴·플레이·결과 상태 전환, HUD, 최고 점수, 효과음을 담당한다.
- **왜**: 스테이지가 준비되지 않았거나 로딩 중일 때 시작하면 코인이 없는 라운드가 진행된다.
- **어떻게**: `IsReady = coins != null && coins.Length > 0 && !busy`일 때만 Start 버튼을 활성화한다. `Update`에서 `round.Tick` 후 종료를 판정하고, 종료되면 `FinishRound → SaveBest`를 호출한다. 수집 효과음은 외부 음원 없이 `AudioClip.Create`로 880Hz 감쇠 사인파 0.1초를 만들어 사용하고, `OnDestroy`에서 직접 만든 클립만 정리한다.
- **코드**: [GameSession.cs](../../Assets/MyAssets/Scripts/GameSession.cs) `BeginRound`, `TryCollect`, `FinishRound`, `RefreshUi`
- **결과**: 메뉴 → 시작 → 결과 → 재시작 → 메뉴 흐름이 PC와 Web에서 정상 동작한다.

### 4-3. 입력 — Input System

- **무엇**: 키보드(WASD·방향키), 게임패드 왼쪽 스틱, 화면 터치 컨트롤(조이스틱·방향키)이 하나의 `Move` 액션으로 같은 이동을 만든다.
- **왜**: 플랫폼마다 입력 장치가 달라도 게임 로직은 하나로 유지하기 위해서이다.
- **어떻게**: `PlatformControls.inputactions`의 `Move`(2D Vector)를 `PlatformMover`가 읽는다. 화면 조이스틱은 `OnScreenStick`(`<Gamepad>/leftStick`), 화면 방향키는 `OnScreenButton`(`<Keyboard>/w·a·s·d`)으로 같은 액션에 연결한다. 입력 벡터는 `ClampMagnitude(…, 1)`로 대각선 가속을 막고, XZ 평면 위치를 ±9로 제한한다.
- **코드**: [PlatformMover.cs](../../Assets/MyAssets/Scripts/Ruintime/PlatformMover.cs)

### 4-4. 화면 비율과 Safe Area — `SafeAreaFitter`

- **무엇**: UI 루트를 `Screen.safeArea`에 맞춘다.
- **왜**: 노치나 둥근 모서리가 있는 휴대폰에서 UI가 가려지지 않게 하기 위해서이다.
- **어떻게**: 안전 영역의 픽셀 좌표를 화면 크기로 나눠 Anchor(0~1)로 바꾼다. 화면 크기나 안전 영역이 바뀐 프레임에만 다시 계산한다.
- **코드**: [SafeAreaFitter.cs](../../Assets/MyAssets/Scripts/SafeAreaFitter.cs)

### 4-5. 저장과 앱 중단·복귀

- **무엇**: 최고 점수와 설정을 PlayerPrefs에 저장하고, 앱이 중단되면 이동과 시간을 멈춘 뒤 저장한다.
- **왜**: 모바일에서는 백그라운드로 전환된 뒤 앱이 종료될 수 있어 기록을 잃을 위험이 있다.
- **어떻게**: `OnApplicationPause(true)`에서 `paused`로 이동과 시간을 잠그고 `SaveBest → PlayerPrefs.Save`를 호출한다. 최고 점수는 `Mathf.Max`로 낮아지지 않게 한다.
- **결과**: PC와 Web에서 다시 실행하거나 새로고침한 뒤에도 최고 점수, 선택한 스테이지, 컨트롤 방식이 유지된다(`Evidence/web_reload_saved.png`).

### 4-6. Editor 전용 코드와 플랫폼별 처리

- **무엇**: Editor 도구는 `Editor` 폴더에 두고, 종료 동작은 플랫폼마다 다르게 처리한다.
- **어떻게**: [PlatformDiagnostics.cs](../../Assets/MyAssets/Scripts/Editor/PlatformDiagnostics.cs)는 `Editor` 폴더에 있어 Player 빌드에서 제외된다(메뉴 `Tools/Platform Lab/Print Build Target`). [ExitGame.cs](../../Assets/MyAssets/Scripts/Ruintime/ExitGame.cs)는 `#if UNITY_EDITOR`이면 Play Mode를 끝내고, `UNITY_WEBGL`이면 탭을 닫지 않고 메뉴로 돌아가 안내 문구를 띄우며, 그 밖의 플랫폼에서는 `Application.Quit`을 호출한다.
- **결과**: Web에서 Quit을 누르면 `Use the page navigation or close the tab.`이 표시되는 것을 확인했다.

### 4-7. Addressables 비동기 스테이지 로드·실패 안내·재시도·해제 — `StageLoader`

- **무엇**: 코인 배치를 담은 스테이지 프리팹을 Addressables로 비동기 생성한다.
- **왜**: 콘텐츠를 실행 파일과 분리해야 로컬과 원격 배포를 같은 코드로 처리하고, 콘텐츠만 교체할 수 있다.
- **처리 순서**:
  1. `SetBusy(true)`로 Start를 잠그고 이전 인스턴스를 `ReleaseInstance`로 해제한다.
  2. `InstantiateAsync(address)`를 호출한다.
  3. 완료되면 실패 시 안내 문구 `Stage load failed. Press Reload Stage.`를 띄우고 핸들을 해제한다. 성공하면 `CoinPickup`을 모아 `SetStage`에 넘긴 뒤 잠금을 푼다.
  4. Reload Stage 버튼으로 같은 과정을 다시 시도한다.
- **코드**: [StageLoader.cs](../../Assets/MyAssets/Scripts/Ruintime/StageLoader.cs) `LoadStage`, `OnCompleted`, `ReleaseOwnedOperation`

### 4-8. ASP.NET Core HTTPS 파일 서버 — `ContentServer`

- **무엇**: Addressables 번들·카탈로그, JSON 설정, Web 빌드를 HTTPS(`https://localhost:5443`)로 제공한다.
- **어떻게**: `PhysicalFileProvider`로 `--FilesRoot` 폴더 하나만 제공한다. MIME 매핑을 비운 뒤 필요한 확장자만 등록해 `.cs` 같은 파일은 404를 반환한다. `/health`는 상태 확인용이고, 모든 응답에 `Cache-Control: no-cache`를 붙인다.
- **코드**: [Program.cs](../../Server/ContentServer/Program.cs), 실행 방법은 [ContentServer/README.md](../../Server/ContentServer/README.md)

### 4-9. JSON 설정의 검증·적용과 기본값 — `PlatformConfigLoader`

- **무엇**: Load Settings를 누르면 `platform-config.json`의 `roundSeconds`를 읽어 다음 라운드 시간에 적용한다.
- **어떻게**: 기본 경로는 `StreamingAssets`이고, Remote Url을 지정하면 서버에서 읽는다. `UnityWebRequest`(timeout 10초) → `JsonUtility` 순서로 처리하며, 다음 경우에는 경고 로그를 남기고 30초를 사용한다.
  - 통신 실패
  - JSON 해석 예외
  - 10~120초 범위를 벗어난 값

  읽는 동안에는 시작을 잠그고, 플레이 중에는 적용하지 않는다.
- **코드**: [PlatformConfigLoader.cs](../../Assets/MyAssets/Scripts/Ruintime/PlatformConfigLoader.cs)
- **결과**: 7장의 C1~C5 참고.

## 5. 플랫폼 대응

| 항목 | PC (Windows) | Android | Web |
| :--- | :--- | :--- | :--- |
| 입력 | 키보드, 게임패드, 마우스로 화면 컨트롤 조작 | 화면 조이스틱·방향키(전환 가능) | 키보드, 마우스로 화면 컨트롤 조작 |
| 화면 | 1280×800(16:10) 창 모드, 크기 조절 불가 | Safe Area 적용, Canvas Scaler 1600×900·Match 0.5 | 기본 960×600(16:10) |
| 저장 | PlayerPrefs(레지스트리) | PlayerPrefs | PlayerPrefs(브라우저 IndexedDB) |
| 생명주기 | 중단 시 저장 | 백그라운드 전환 시 이동·시간 정지와 저장 | 탭 닫기 대신 메뉴 복귀 안내 |
| 프레임 정책 (`FramePolicy`) | VSync 1 | VSync 0, 30 FPS(Inspector에서 변경 가능), 화면 꺼짐 방지 | `targetFrameRate -1`(브라우저 스케줄링) |
| 콘텐츠 | 로컬 Addressables(`StreamingAssets/aa`) | APK 내부 로컬 Addressables | 로컬 Addressables, Strip Engine Code 대응 `link.xml` |
| **실행 결과** | ✅ 한 판 완료, 스테이지 전환, 기록 유지 확인. 원격 콘텐츠·설정 확인 | ⚠️ **미확인**: APK 빌드와 APK 안의 Android용 번들·카탈로그 포함까지만 확인함. 실기기·에뮬레이터 실행은 하지 않음 | ✅ ContentServer로 실행, 한 판 완료, 새로고침 후 기록 유지, Quit 안내, Console 오류 없음(7-1 수정 후) |

Android 보완 계획: 실기기에 APK를 설치해 터치 조이스틱·방향키 전환, 20:9 화면 Safe Area, 백그라운드 전환 후 복귀, 30 FPS 정책(logcat의 `[FramePolicy]` 줄)을 확인한다. 실기기와 에뮬레이터 결과는 나누어 기록한다.

## 6. 추가·개선한 부분

| 구분 | 내용 |
| :--- | :--- |
| 수업에서 따라 구현 | 4장의 기본 구조 전체(CoinRound, GameSession, PlatformMover, SafeAreaFitter, ExitGame, PlatformDiagnostics, StageLoader 기본 로드, PlatformConfigLoader, ContentServer 기본 구조, FramePolicy 기본 정책, RefreshUi 캐시) |
| 직접 추가·개선 | 아래 A1~A6, 문제 해결 7-1·7-2 |
| 외부 자료 활용 | 7-3의 Web 빌드 링크 오류 대응(`EmvalCompat.jslib`), 출처는 7-3에 표기 |

### A1. 스테이지 선택 — Addressables Label 기반 목록

- **수업 구현과의 차이**: 수업 구현은 고정 주소 `stage-main` 하나만 로드했다. 지금은 `stage` 라벨이 붙은 모든 프리팹을 스테이지로 인식하고, 메뉴의 `<` `>`로 고른다.
- **필요한 이유**: 스테이지를 추가할 때마다 코드나 실행 파일을 고치지 않기 위해서이다.
- **구현**: `Addressables.LoadResourceLocationsAsync("stage", typeof(GameObject))`로 주소(`PrimaryKey`)를 모은 뒤 중복을 제거하고 정렬해 `List<string>`으로 보관한다. 위치 핸들은 주소를 읽은 직후 `Release`한다. 조회에 실패하거나 결과가 0개면 기본 주소로 진행한다. 선택 인덱스는 `(i + step + n) % n`으로 순환하고, 마지막 선택은 PlayerPrefs에 저장한다. 스테이지가 바뀌면 이전 인스턴스를 해제한 뒤 새 주소로 로드한다. 플레이 중이거나 로딩 중에는 입력을 무시한다.
- **코드**: [StageLoader.cs](../../Assets/MyAssets/Scripts/Ruintime/StageLoader.cs) `Start`, `OnLocationsCompleted`, `ChangeSelection`
- **결과**: 3개 스테이지(main·a·b) 전환을 확인했다. 원격 구성에서는 **Player를 다시 빌드하지 않고** 서버에 stage-c를 추가했더니 목록이 `/4`로 늘어났다. 서버 로그에서 카탈로그 해시, 새 카탈로그, 새 번들 요청을 확인했다(`Evidence/R4_stage_c_added.png`, `Evidence/remote_server.log`).

### A2. 스테이지별 최고 점수와 기존 기록 이전

- **차이·이유**: 최고 점수 키 하나를 모든 스테이지가 공유하면 난이도가 다른 스테이지의 기록이 섞인다. 업데이트 뒤에도 기존 사용자의 기록은 유지되어야 한다.
- **구현**: 키를 `PlatformCoinLab.Best.{address}`로 나눴다. `MigrateLegacyBest`는 예전 키가 있고 새 키가 없을 때만 예전 기록을 `stage-main` 기록으로 복사하고 예전 키를 지운다. 그래서 한 번만 실행된다. `SetStageId`는 플레이 중에는 기록 대상을 바꾸지 않는다.
- **코드**: [GameSession.cs](../../Assets/MyAssets/Scripts/GameSession.cs) `SetStageId`, `MigrateLegacyBest`
- **결과**: 스테이지마다 Best가 따로 표시되고 다시 실행해도 유지된다.

### A3. 터치 컨트롤 방식 전환 (조이스틱 ↔ 방향키)

- **차이·이유**: 수업 구현은 씬에서 한 방식만 켜 두었다. 아날로그 이동과 정확한 4방향 이동 중 어느 쪽을 선호하는지는 사용자마다 다르다.
- **구현**: `MobileControlsToggle.Apply`가 두 그룹을 SetActive로 전환하고, 선택을 `PlatformCoinLab.TouchLayout`에 저장한다. OnScreen 컨트롤은 비활성화될 때 입력을 기본값으로 되돌리므로, 이동 중에 전환해도 입력이 눌린 채로 남지 않는다.
- **코드**: [MobileControlsToggle.cs](../../Assets/MyAssets/Scripts/Ruintime/MobileControlsToggle.cs)
- **결과**: PC와 Web에서 전환과 저장을 확인했다. Android 실행은 미확인이다.

### A4. ContentServer 확장

- **차이**: 수업 서버에 다음을 추가했다.
  - 요청 로그(`[시각] 메서드 경로 -> 상태 (ms)`)
  - Web 카탈로그용 `.bin` MIME
  - `.gz`·`.br` 응답 헤더 처리
  - `--Urls` 인수
  - 실패 확인용 설정 파일 2개(`broken-config.json`, `invalid-range-config.json`)
- **이유**: 원격 실패와 복구를 서버 쪽에서도 증명하기 위해서이다. 현재 Web 빌드의 카탈로그가 `.bin`이라 수업 서버로는 Web 원격 로드가 불가능했다.
- **결과**: 7장 R1~R4의 서버 로그로 사용했다.

### A5. FramePolicy 보완

- **차이**: Android 목표 FPS를 Inspector 값으로 바꿀 수 있게 했다(30/60 비교용). 플레이 중 화면 꺼짐을 막는 `Screen.sleepTimeout = NeverSleep`과, 실제 적용된 vSync·FPS·주사율·해상도를 남기는 로그를 추가했다.
- **결과**: PC Player 로그 `[FramePolicy] platform=WindowsPlayer vSync=1 targetFps=-1 refresh=60Hz`.
- **코드**: [FramePolicy.cs](../../Assets/MyAssets/Scripts/Ruintime/FramePolicy.cs)

### A6. 제출 패키징 스크립트

- **내용**: [Tools/package_submission.ps1](../../Tools/package_submission.ps1)이 커밋 기준 소스 스냅샷(`git archive`), 3개 플랫폼 빌드(`DontShip` 폴더 제외), 서버(bin·obj 제외), 문서, 확인 자료, `VERSION.txt`를 한 폴더로 모은다. 커밋하지 않은 변경이 있으면 중단해서 소스와 빌드의 버전이 어긋나지 않게 한다.

## 7. 문제 해결 사례

### 7-1. Web에서만 코인이 수집되지 않음 — 직접 발견·해결

| 단계 | 내용 |
| :--- | :--- |
| 문제 | 최종 Web 빌드에서 코인에 닿아도 수집되지 않았다. 같은 커밋의 PC 빌드는 정상이었다. |
| 재현 조건 | Web Release 프로필(Strip Engine Code 켜짐), 로컬 Addressables 스테이지 로드 후 코인 접촉 |
| 조사 | 브라우저 Console에 코인 수만큼 `Could not produce class with ID 135 … class being stripped from the build`와 `GameObject contains a component type that is not recognized`가 반복되었다. Class ID 135는 `SphereCollider`이다(`Evidence/web_console_class135.png`). |
| 원인 | Strip Engine Code는 빌드에 포함된 씬에서 쓰는 엔진 클래스만 남긴다. 코인의 SphereCollider는 Addressables 프리팹에만 있어서 빌드에서 제거되었고, 코인이 충돌체 없이 생성되어 트리거가 발생하지 않았다. Player의 CapsuleCollider와 Rigidbody는 씬에 있어서 유지되었다. PC는 미리 빌드된 엔진(`UnityPlayer.dll`)을 쓰기 때문에 이 영향을 받지 않는다. Addressables가 콘텐츠를 빌드할 때 만드는 `link.xml`은 빌드마다 생겼다 지워지는 임시 파일이라 Player 빌드에 반영된다는 보장이 없었다. |
| 수정 | 프로젝트에 고정 [link.xml](../../Assets/MyAssets/link.xml)을 추가해 `UnityEngine.SphereCollider`를 보존했다. Strip Engine Code는 유지했다. 대안인 "Strip Engine Code 끄기"는 빌드 크기가 커져서 선택하지 않았다. |
| 재검증 | Web을 다시 빌드한 뒤 Console에 ID 135 오류와 미인식 컴포넌트 경고가 없었다. 코인 수집, 한 판 완료, 새로고침 후 기록 유지도 확인했다. |
| 비용 | `Web.wasm` 23,181,425 B → 23,359,134 B(+0.77%) |

### 7-2. 플레이어가 Ground 앞뒤로 벗어남 — 직접 발견·해결

| 단계 | 내용 |
| :--- | :--- |
| 문제 | W/S(스틱 상하) 입력을 계속하면 Player가 Ground 밖으로 나갔다. 좌우는 정상적으로 막혔다. |
| 원인 | 이동은 XZ 평면(`new Vector3(input.x, 0, input.y)`)에서 일어나는데 제한은 `next.y`에 걸려 있었다. 그래서 Z축 제한이 없었다. |
| 수정 | `next.z = Mathf.Clamp(next.z, -9f, 9f)` ([PlatformMover.cs](../../Assets/MyAssets/Scripts/Ruintime/PlatformMover.cs)) |

### 7-3. Web 빌드 링크 오류 — 수업 중 해결, 외부 자료 활용

| 단계 | 내용 |
| :--- | :--- |
| 문제 | Web Player 빌드가 `wasm-ld: undefined symbol: _emval_take_value / _emval_as`로 실패했다. |
| 원인 | `com.unity.ai.assistant`가 의존하는 `com.unity.dt.app-ui`의 미리 빌드된 라이브러리가 예전 Emscripten의 embind 심볼을 사용했다. Unity 6000.6의 Emscripten 4.x에서는 이 심볼이 제거되었다. |
| 수정 | AI Assistant 패키지를 제거하고, `Assets/Plugins/WebGL/EmvalCompat.jslib` 호환 함수를 추가하고, Web 프로필에 `-lembind` 링커 인수를 넣었다. |
| 재검증 | 이후 모든 Web 빌드가 성공했다. 최종 Web 빌드 실행도 확인했다. |
| 출처 | `EmvalCompat.jslib`는 외부 자료를 참고해 작성했다. ☐ 출처 기재 필요 |

### 7-4. 4:3 창에서 최고 점수·저장 버튼이 잘림 — 직접 분석, 해상도 고정으로 대응

- **원인**: BestText와 SaveButton이 화면 중앙 Anchor에 x=590 고정 오프셋으로 놓여 있다(오른쪽 끝 740). 4:3에서는 Canvas 절반 너비가 약 692가 되어 잘린다.
- **대응**: PC 기본 창을 1280×1024에서 1280×800(16:10)으로 바꿨다. Android 20:9에서도 잘리지 않는다는 것은 계산으로만 확인했다(Canvas 1789×805).
- **한계**: 4:3 태블릿은 미대응이다(9장).

### 7-5. StageLoader 로딩 중 파괴 처리 누락 — 코드 검토로 발견

- **원인**: `ownerDestroyed` 필드가 true로 바뀌는 곳이 없었다. 그래서 로딩 중에 오브젝트가 파괴되면 완료 콜백이 파괴된 GameSession에 접근하고, 인스턴스도 해제되지 않을 수 있었다.
- **수정**: `OnDestroy`에서 플래그를 설정해, 늦게 끝난 로드는 콜백에서 해제만 하도록 했다.

### 원격 콘텐츠 실패·복구 확인 (REMOTE_CONTENT_GUIDE.md 절차)

환경: Windows Player(`RemotePractice` 프로필, Remote Catalog), ContentServer `https://localhost:5443`

| # | 조건 | 결과 | 증거 |
| :--- | :--- | :--- | :--- |
| R1 | 서버 중지, 캐시 없음 | 스테이지 목록 표시, `Stage load failed. Press Reload Stage.`, Start 잠금. Player 로그는 `ConnectionError : Cannot connect to destination host`로 404와 구분된다. | `R1_server_off.png`, `R1_player.log` |
| R2 | 서버 실행 후 Reload Stage | `Stage ready`, 한 판 완료. 서버 로그 `GET …/coursestage_…bundle -> 200` | `R2_recovered.png`, `remote_server.log` |
| R4 | stage-c 추가 → Update a Previous Build | Player를 다시 빌드하지 않고 목록 `/4`. 서버 로그 `catalog_0.1.0.hash` → `catalog_0.1.0.json` → 새 번들 모두 200 | `R4_stage_c_added.png`, `remote_server.log` |

### 원격 JSON 설정 비교

Remote Url은 고정하고 서버 파일만 바꿔 같은 Player로 비교했다.

| # | 서버 파일 | 화면 | Player 로그 | 서버 응답 |
| :--- | :--- | :--- | :--- | :--- |
| C1 | `{"roundSeconds":45}` | `Next round: 45 seconds`, 타이머 45부터 시작 | 없음 | 200 |
| C2 | JSON 문법 오류 | 30초 | `Invalid JSON: … Missing a comma or '}'` | 200 |
| C3 | 범위 밖 값 500 | 30초 | `Invalid roundSeconds` | 200 |
| C4 | 파일 없음 | 30초 | `Config failed: HTTP/1.1 404 Not Found` | 404 |
| C5 | 서버 중지 | 30초 | `Config failed: Cannot connect to destination host` | 연결 없음 |

C2~C5는 화면이 같고, 실패 원인은 로그로 구분된다(`Evidence/C1_config_45.png`~`C4_config_404.png`, `C_config_player.log`).

## 8. 성능 개선

### HUD 갱신의 매 프레임 GC 할당 제거 — 수업(DAY 14)에서 구현한 개선을 실제 Player에서 재측정

| 항목 | 내용 |
| :--- | :--- |
| 변경 내용 | `GameSession.RefreshUi`가 표시 값 6개(점수·초·최고점·플레이·결과·준비 상태)를 기억하고, 값이 바뀐 프레임에만 `SetActive`와 문자열 생성을 수행한다. 구간 측정용 `ProfilerMarker("CoinLab.PresentUi")`를 추가했다. |
| 측정 환경 | Windows 10, Windows Development Player(IL2CPP, Autoconnect Profiler), 1280×800 창 모드, VSync 1(60Hz), 같은 스테이지. Load Stage → Clear → Start → 약 10초 이동·수집 |
| 수정 전 측정 방법 | `RefreshUi` 본문을 DAY 14 직전 커밋 버전으로 잠시 되돌리고(마커 유지) 같은 설정으로 다시 빌드해 측정한 뒤 복원했다. |

| 지표 | 수정 전 (매 프레임 갱신) | 수정 후 (값 캐시) |
| :--- | :--- | :--- |
| PlayerLoop GC Alloc (플레이 중 프레임) | **226 B** | **0 B** |
| 60 FPS 환산 할당량 | 약 13.6 KB/s | 숫자가 바뀌는 프레임에서만 할당 |
| 프레임 CPU 시간 | 16.40 ms | 16.89 ms |

- **해석**: 두 빌드 모두 프레임의 대부분(약 13~14 ms)이 VSync 대기였다. 이 게임은 60 FPS 제한에 묶여 있어서 CPU 시간으로는 차이가 드러나지 않는다. 개선 효과는 "빨라짐"이 아니라 **매 프레임 생기던 가비지 할당의 제거**이다. 이로써 GC 수집 빈도와 스파이크 위험이 줄어든다.
- **기능 유지**: 수정 후에도 점수, 남은 시간, 최고 점수 표시가 정상이다.
- **증거**: `Evidence/01_before.jpg`, `Evidence/01_after.jpg`, `Evidence/ProfilerCaptures/O1_before_every_frame.data`, `O1_after_uicache.data` (Unity Profiler에서 Load)
- **측정 한계**: 각각 1회 측정했고 대표 프레임 1개를 비교했다. `CoinLab.PresentUi` 마커만의 값과 GC.Collect 횟수, Android·Web 측정은 하지 않았다.
- **추가 후보(미적용)**: `TMP_Text.SetText(format, args)`를 쓰면 숫자가 바뀌는 프레임의 문자열 할당도 없앨 수 있다.

Web의 `link.xml` 수정에 따른 빌드 크기 변화(+0.77%)는 7-1에 적었다.

## 9. 실행·복원 방법과 한계

### 실행

| 항목 | 내용 |
| :--- | :--- |
| 시작 씬 | `Assets/MyAssets/Scenes/PlatformMain.unity` (빌드 씬 목록의 유일한 씬) |
| 조작 | 이동: WASD, 방향키, 게임패드 왼쪽 스틱, 화면 조이스틱·방향키. 메뉴: `<` `>` 스테이지 선택, Start, Load Stage(다시 불러오기), Load Settings, Save, Quit. 컨트롤 전환 버튼 |
| PC | `Builds/PC/PlatformCoinLab.exe` 실행. 서버 불필요 |
| Android | `adb install Builds/Android/PlatformCoinLab_Android.apk` (실행 미확인) |
| Web | 파일을 더블클릭해서는 열리지 않는다. 서버로 제공해야 하며, ContentServer 폴더에서 `dotnet run --no-launch-profile -- --FilesRoot "<제출 폴더>/Builds/Web"`을 실행한 뒤 `https://localhost:5443/`을 연다. |

### 프로젝트 복원과 빌드 순서

1. Unity Hub에서 `UnityProject` 폴더를 Unity **6000.6.7f1**로 연다. 처음 열 때 Library를 다시 만든다.
2. Addressables Groups의 활성 프로필이 **Default**이고 CourseStage가 Local 경로인지 확인한다.
3. File → Build Profiles에서 대상 플랫폼으로 전환한다.
4. Addressables Groups → Build → **New Build → Default Build Script**를 실행한다. Player를 빌드할 때 Addressables가 자동으로 빌드되지 않도록 설정되어 있으므로, 플랫폼마다 반드시 실행해야 한다.
5. Player를 빌드한다. PC는 Windows 프로필(Release), Android는 APK, Web은 `Web - Desktop - Release` 프로필(압축 Disabled)을 사용한다.

### 서버 실행

.NET 10 SDK와 개발 인증서(`dotnet dev-certs https --trust`)가 필요하다. 자세한 방법은 `ContentServer/README.md`, 원격 콘텐츠·설정 비교 절차는 `REMOTE_CONTENT_GUIDE.md`에 있다.

### 알려진 문제와 미확인 환경

| 구분 | 내용 | 보완 계획 |
| :--- | :--- | :--- |
| 미확인 | Android 실기기·에뮬레이터 실행 전체(입력, Safe Area, 생명주기, FPS) | 5장의 Android 보완 계획 |
| 미확인 | 4:3 태블릿 UI(계산상 BestText·SaveButton이 잘림) | BestText·SaveButton을 오른쪽 위 Anchor, HudPanel을 위 중앙 Anchor로 변경 |
| 한계 | CourseStage가 Pack Together라 스테이지 하나를 추가해도 그룹 번들 전체(73.5 KB)를 다시 받는다 | 스테이지별 Pack Separately |
| 한계 | 설정 읽기 실패의 원인이 화면에 표시되지 않는다(로그에만 남음) | Status에 실패 사유 표시 |
| 미확인 | 원격 R3(캐시 로드), R5·R6(번들 404·복구), 7-5 재검증 | REMOTE_CONTENT_GUIDE.md 시나리오 진행 |
| 미측정 | Android·Web 프로파일, PresentUi 마커만의 값, GC.Collect 횟수 | 저장된 `.data` 분석, 플랫폼별 측정 |
| 범위 밖 | iOS(제출 대상 제외), Android 기기의 원격 콘텐츠(localhost 인증서 제약) | — |
| 버전 | PC·Android 빌드는 `link.xml` 추가 직전 커밋으로 만들었다. link.xml은 엔진 코드 스트리핑을 하는 Web에만 영향을 주므로 동작 차이는 없다. | — |

## 외부 코드·자산 출처

| 항목 | 출처와 사용 범위 |
| :--- | :--- |
| Unity 패키지 | Addressables, Input System(`OnScreenStick`·`OnScreenButton` 포함), URP, uGUI·TextMesh Pro(기본 폰트 LiberationSans SDF): Unity Technologies 패키지를 그대로 사용 |
| UI 스프라이트 | Unity 내장 기본 스프라이트 |
| 효과음 | 외부 음원 없음. 코드로 생성(4-2) |
| `EmvalCompat.jslib` | 외부 자료 참고(7-3) |
| 수업 자료 | `Docs/DAY01~16` 수업 문서의 예제 코드를 바탕으로 4장 기능을 구현 |
