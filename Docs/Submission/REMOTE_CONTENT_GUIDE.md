# 원격 콘텐츠·설정 비교 절차

기본 게임은 서버 없이 로컬 Addressables 콘텐츠로 실행됩니다. 이 문서는 같은 게임을 HTTPS 파일 서버(ContentServer)에서 콘텐츠를 받도록 바꾸어, 원격 로드·실패·복구와 Player 재빌드 없는 스테이지 추가를 확인하는 절차입니다. 제출 프로젝트(로컬 설정)를 먼저 확보한 뒤, 같은 프로젝트의 설정을 원격으로 바꿔 시험하고 마지막에 Git으로 로컬 설정을 복원합니다.

확인 환경: Windows 10, Unity 6000.6.7f1, .NET SDK 10.0.403, Windows Player (StandaloneWindows64). Android·Web의 원격 로드는 이 절차의 범위 밖입니다(아래 "한계" 참고).

## 1. 준비

### 1-1. 시작 전 확인

- 제출용 소스와 PC·Android·Web 빌드를 이미 제출 폴더로 복사했는지 확인합니다.
- `git status`가 깨끗한지 확인합니다. 아래 변경은 커밋하지 않고 5단계에서 되돌립니다.

### 1-2. 원격 설정 (커밋하지 않음)

| 위치 | 기본(로컬) | 원격 비교용 |
| :--- | :--- | :--- |
| Addressables 활성 프로필 | Default | RemotePractice |
| RemotePractice `Remote.BuildPath` | — | `ServerData/[BuildTarget]` |
| RemotePractice `Remote.LoadPath` | — | `https://localhost:5443/[BuildTarget]` |
| CourseStage Build & Load Paths | Local | Remote |
| Settings → Build Remote Catalog | 끔 | 켬 (Remote 경로) |
| Settings → Build Addressables on Player Build | Do not build | Do not build (콘텐츠 업데이트 기준 유지) |

### 1-3. 콘텐츠와 Player 빌드 (V1)

1. Addressables Groups → Build → New Build → Default Build Script
2. `ServerData/StandaloneWindows64/`에 `catalog_*.json`·`.hash`·`*.bundle` 생성 확인
3. `Assets/AddressableAssetsData/Windows/addressables_content_state.bin` 확인 (4단계 업데이트 빌드의 기준. 서버에 올리지 않음)
4. File → Build Profiles → Windows → `Builds/WindowsRemote/`에 Player 빌드

### 1-4. 서버 실행

`Server/ContentServer` 폴더에서 실행합니다. FilesRoot는 프로젝트의 ServerData입니다.

```powershell
dotnet run --no-launch-profile -- --FilesRoot "../../ServerData"
```

제출 폴더 구조(`ContentServer/`와 `UnityProject/`가 나란히 있음)에서는 원격 콘텐츠 빌드가 `UnityProject/ServerData`에 생성되므로 `--FilesRoot "../UnityProject/ServerData"`를 사용합니다.

`https://localhost:5443/health`에서 `{"status":"ok"}`를 확인합니다.

### 1-5. 캐시 초기화

이전 실행의 캐시가 있으면 서버 없이도 로드가 성공해 실패 조건을 확인할 수 없습니다. 각 시나리오 전에 Player를 종료하고 아래 폴더를 삭제합니다.

| 캐시 | 위치 |
| :--- | :--- |
| 번들 캐시 | `%USERPROFILE%\AppData\LocalLow\Unity\DefaultCompany_PlatformCoinLab` |
| 원격 카탈로그 캐시 | `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PlatformCoinLab\com.unity.addressables` |

## 2. 시나리오

각 시나리오에서 Player 화면(Status 문구)을 캡처하고, 서버 요청 로그를 파일로 남깁니다. 서버를 아래처럼 실행하면 콘솔 출력이 그대로 파일에도 기록됩니다.

```powershell
dotnet run --no-launch-profile -- --FilesRoot "../../ServerData" | Tee-Object -FilePath "../../Docs/Submission/Evidence/remote_server.log" -Append
```

Player 쪽 오류는 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PlatformCoinLab\Player.log`에 남습니다. 시나리오가 끝날 때마다 필요한 부분을 `Docs/Submission/Evidence/`에 복사합니다.

| # | 조건 | 조작 | 기대 결과 | 서버 로그 |
| :--- | :--- | :--- | :--- | :--- |
| R1 | 서버 중지·캐시 없음 | Player 실행 | 스테이지 목록 표시, 로드 실패 안내 `Stage load failed. Press Reload Stage.`, Start 잠금 | 없음 |
| R2 | R1에서 서버 실행 | Load Stage(Reload) | 번들 다운로드 후 `Stage ready`, 한 판 완료 | `GET /StandaloneWindows64/*.bundle -> 200` |
| R3 | 서버 실행·캐시 있음 | 서버 중지 후 Player 재실행 | 캐시된 번들로 로드 성공 (서버 연결 성공이 아님) | 없음 |
| R4 | 서버 실행 | 스테이지 추가(3단계) 후 Player 재실행 | **Player 재빌드 없이** 목록에 새 스테이지 표시·플레이 | `GET catalog_*.hash`, `catalog_*.json`, 새 `.bundle` -> 200 |
| R5 | 서버 실행·새 번들 미캐시 | 새 번들 파일을 서버 폴더 밖으로 옮긴 뒤 새 스테이지 선택 | 로드 실패 안내, 다른 스테이지는 정상 | `GET *.bundle -> 404` |
| R6 | R5에서 번들 복원 | Reload Stage | 새 스테이지 로드 성공 | `-> 200` |

## 3. 스테이지 추가 (Player 재빌드 없음)

1. StageMain을 복제해 `StageC`를 만들고 코인 배치를 바꿉니다(코인 ID 중복 없음).
2. CourseStage 그룹에 넣고 address `stage-c`, Label `stage`를 지정합니다.
3. Addressables Groups → Build → **Update a Previous Build** → `Assets/AddressableAssetsData/Windows/addressables_content_state.bin` 선택
4. ServerData에 새 번들과 갱신된 카탈로그·해시가 생성됐는지 확인합니다. 이전 번들은 삭제하지 않습니다.
5. Player를 **다시 빌드하지 않고** 재실행합니다. 시작 시 Addressables가 원격 카탈로그 해시를 확인해 새 카탈로그를 받고, `StageLoader`가 `stage` 라벨로 목록을 다시 만들므로 `Stage n/4: stage-c`가 나타납니다.

## 4. 원격 설정(JSON) 비교

GameSession 오브젝트에서 `PlatformConfigLoader`의 Remote Url을 `https://localhost:5443/platform-config.json`으로 지정하고 Player를 한 번 빌드합니다(`Builds/WindowsRemoteConfig/`). 비어 있으면 StreamingAssets의 설정을 읽습니다.

URL은 고정하고 서버 폴더의 파일만 바꾸어 비교하므로 Player를 다시 빌드하지 않습니다. 서버는 기본 Content 폴더로 실행합니다(`dotnet run --no-launch-profile`). 각 조건에서 메뉴의 Load Settings를 누르고 Status 문구와 Player 로그(`%USERPROFILE%\AppData\LocalLow\DefaultCompany\PlatformCoinLab\Player.log`)의 경고를 기록합니다.

| 서버의 `Content/platform-config.json` | 기대 결과 |
| :--- | :--- |
| 원본 `{"roundSeconds":45}` | `Next round: 45 seconds`, 다음 라운드 45초 |
| `broken-config.json` 내용으로 교체 | `Invalid JSON` 경고, 30초 |
| `invalid-range-config.json` 내용(500)으로 교체 | `Invalid roundSeconds` 경고, 30초 |
| 파일 이름을 바꿔 없앰 | `Config failed: HTTP/1.1 404` 경고, 30초 |
| 서버 중지 | `Config failed: Cannot connect` 경고, 30초 |
| 원본 복원·서버 재실행 | 다시 45초 |

실험 후 `Content/platform-config.json`을 원본으로 복원합니다.

## 5. 정리

1. 서버를 Ctrl+C로 종료하고 `Content/platform-config.json`이 원본인지 확인합니다.
2. 캡처와 로그가 `Docs/Submission/Evidence/`에 있는지 확인합니다. 아래 명령은 `Assets`·`ServerData`·`ProjectSettings`만 대상으로 하므로 `Docs`는 지우지 않습니다.
3. Unity를 닫고 저장소 루트에서 원격 설정 변경과 새로 생긴 파일(StageC, 콘텐츠 상태 파일, 새 번들)을 되돌립니다.

```powershell
git restore Assets ProjectSettings ServerData
git clean -n Assets ServerData   # 지울 파일 목록 미리 보기
git clean -f Assets ServerData
```

4. Unity를 다시 열어 Addressables 활성 프로필이 Default이고 CourseStage가 Local 경로인지 확인합니다.

기본 제출본은 Default 프로필과 로컬 경로를 유지합니다.

## 한계

- 개발용 localhost 인증서를 사용하므로 같은 PC의 Windows Player에서만 확인했습니다. Android 기기에서 localhost는 기기 자신이며, LAN 주소·인증서 이름·기기의 인증서 신뢰를 따로 구성해야 합니다(미확인).
- Web Player의 원격 로드는 Web 빌드 출력과 번들을 같은 서버에서 제공하는 구성(Remote.BuildPath `Builds/Web/[BuildTarget]`)으로 수업 중 확인했으며, 이 문서의 시나리오로는 재측정하지 않았습니다.
