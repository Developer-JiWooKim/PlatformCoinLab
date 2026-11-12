# 원격 콘텐츠·설정 비교 절차

기본 게임은 서버 없이 로컬 Addressables 콘텐츠로 실행됩니다. 이 문서는 같은 게임이 HTTPS 파일 서버(ContentServer)에서 콘텐츠와 설정을 받도록 바꾸어 다음을 확인하는 절차입니다.

- 원격 로드
- 실패 안내와 복구
- Player를 다시 빌드하지 않는 스테이지 추가
- 원격 JSON 설정의 기본값 처리

확인 환경: Windows 10, Unity 6000.6.7f1, .NET SDK 10.0.403, Windows Player(StandaloneWindows64). Android와 Web의 원격 로드는 이 절차의 범위가 아닙니다(아래 "한계" 참고). 실제 확인 결과는 `PORTFOLIO.md` 7장과 `Evidence/`에 있습니다.

경로는 제출 폴더 기준입니다. `UnityProject/`와 `ContentServer/`가 같은 폴더 안에 나란히 있습니다.

## 1. 준비

### 1-1. 서버 준비

- .NET 10 SDK를 설치합니다(`dotnet --list-sdks`로 확인).
- 개발용 HTTPS 인증서를 신뢰하도록 설정합니다(`dotnet dev-certs https --trust`).

자세한 내용은 `ContentServer/README.md`에 있습니다.

### 1-2. 원격 설정

`UnityProject`를 Unity로 열고 다음을 바꿉니다. 기본 제출본은 왼쪽 열의 로컬 설정입니다.

| 위치 | 기본(로컬) | 원격 비교용 |
| :--- | :--- | :--- |
| Addressables 활성 프로필 | Default | RemotePractice |
| RemotePractice `Remote.BuildPath` | — | `ServerData/[BuildTarget]` |
| RemotePractice `Remote.LoadPath` | — | `https://localhost:5443/[BuildTarget]` |
| CourseStage Build & Load Paths | Local | Remote |
| Settings → Build Remote Catalog | 끔 | 켬 (Remote 경로) |
| Settings → Build Addressables on Player Build | Do not build | Do not build (콘텐츠 업데이트 기준 유지) |

### 1-3. 콘텐츠와 Player 빌드 (V1)

1. Build Profiles에서 Windows를 활성 플랫폼으로 선택합니다.
2. Addressables Groups → Build → New Build → Default Build Script를 실행합니다.
3. `UnityProject/ServerData/StandaloneWindows64/`에 `catalog_*.json`, `catalog_*.hash`, `*.bundle`이 생성됐는지 확인합니다.
4. `UnityProject/Assets/AddressableAssetsData/Windows/addressables_content_state.bin`이 생성됐는지 확인합니다. 3장 업데이트 빌드의 기준 파일이며, 서버에 올리지 않습니다.
5. Player를 `UnityProject/Builds/WindowsRemote/`에 빌드합니다.

### 1-4. 서버 실행

`ContentServer` 폴더에서 실행합니다. FilesRoot는 원격 콘텐츠 빌드 결과 폴더입니다.

```powershell
dotnet run --no-launch-profile -- --FilesRoot "../UnityProject/ServerData"
```

`https://localhost:5443/health`에서 `{"status":"ok"}`가 나오는지 확인합니다. 서버 콘솔에는 요청마다 `[시각] GET 경로 -> 상태코드 (ms)`가 출력됩니다. 이 출력을 파일로도 남기려면 명령 끝에 `| Tee-Object -FilePath remote_server.log -Append`를 붙입니다.

### 1-5. 캐시 초기화

이전 실행의 캐시가 남아 있으면 서버 없이도 로드가 성공해서 실패 조건을 확인할 수 없습니다. 각 시나리오 전에 Player를 종료하고 아래 폴더를 삭제합니다.

| 캐시 | 위치 |
| :--- | :--- |
| 번들 캐시 | `%USERPROFILE%\AppData\LocalLow\Unity\DefaultCompany_PlatformCoinLab` |
| 원격 카탈로그 캐시 | `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PlatformCoinLab\com.unity.addressables` |

Player 쪽 오류는 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PlatformCoinLab\Player.log`에 남습니다.

## 2. 시나리오

| # | 조건 | 조작 | 기대 결과 | 서버 로그 |
| :--- | :--- | :--- | :--- | :--- |
| R1 | 서버 중지, 캐시 없음 | Player 실행 | 스테이지 목록 표시, 로드 실패 안내 `Stage load failed. Press Reload Stage.`, Start 잠금 | 없음 |
| R2 | R1에서 서버 실행 | Load Stage(다시 불러오기) | 번들 다운로드 후 `Stage ready`, 한 판 완료 | `GET /StandaloneWindows64/*.bundle -> 200` |
| R3 | 서버 실행, 캐시 있음 | 서버 중지 후 Player 재실행 | 캐시된 번들로 로드 성공(서버 연결 성공이 아님) | 없음 |
| R4 | 서버 실행 | 스테이지 추가(3장) 후 Player 재실행 | **Player를 다시 빌드하지 않아도** 목록에 새 스테이지가 표시되고 플레이 가능 | `GET catalog_*.hash`, `catalog_*.json`, 새 `.bundle` -> 200 |
| R5 | 서버 실행, 새 번들 미캐시 | 새 번들 파일을 서버 폴더 밖으로 옮긴 뒤 새 스테이지 선택 | 로드 실패 안내, 다른 스테이지는 정상 | `GET *.bundle -> 404` |
| R6 | R5에서 번들 복원 | Load Stage | 새 스테이지 로드 성공 | `-> 200` |

## 3. 스테이지 추가 (Player 재빌드 없음)

1. StageMain을 복제해 `StageC`를 만들고 코인 배치를 바꿉니다. 같은 스테이지 안에서 코인 ID가 겹치지 않게 합니다.
2. StageC를 CourseStage 그룹에 넣고 address는 `stage-c`, Label은 `stage`로 지정합니다.
3. Addressables Groups → Build → **Update a Previous Build**를 실행하고 `Assets/AddressableAssetsData/Windows/addressables_content_state.bin`을 선택합니다.
4. `ServerData`에 새 번들과 갱신된 카탈로그·해시가 생성됐는지 확인합니다. 이전 번들은 삭제하지 않습니다.
5. Player를 **다시 빌드하지 않고** 다시 실행합니다. 시작할 때 Addressables가 원격 카탈로그 해시를 확인해 새 카탈로그를 받습니다. 이어서 `StageLoader`가 `stage` 라벨로 목록을 다시 만들기 때문에 `Stage n/4: stage-c`가 나타납니다.

## 4. 원격 설정(JSON) 비교

GameSession 오브젝트에 있는 `PlatformConfigLoader`의 Remote Url을 `https://localhost:5443/platform-config.json`으로 지정하고, Player를 한 번 빌드합니다(`Builds/WindowsRemoteConfig/`). Remote Url이 비어 있으면 StreamingAssets의 설정을 읽습니다.

URL은 고정해 두고 서버 폴더의 파일만 바꿔 가며 비교하므로 Player를 다시 빌드하지 않습니다. 서버는 기본 Content 폴더로 실행합니다(`ContentServer` 폴더에서 `dotnet run --no-launch-profile`). 각 조건에서 메뉴의 Load Settings를 누르고, Status 문구와 Player 로그의 경고를 확인합니다.

| 서버의 `Content/platform-config.json` | 기대 결과 |
| :--- | :--- |
| 원본 `{"roundSeconds":45}` | `Next round: 45 seconds`, 다음 라운드 45초 |
| `broken-config.json` 내용으로 교체 | `Invalid JSON` 경고, 30초 |
| `invalid-range-config.json` 내용(500)으로 교체 | `Invalid roundSeconds` 경고, 30초 |
| 파일 이름을 바꿔 없앰 | `Config failed: HTTP/1.1 404` 경고, 30초 |
| 서버 중지 | `Config failed: Cannot connect` 경고, 30초 |
| 원본 복원 후 서버 재실행 | 다시 45초 |

실험이 끝나면 `Content/platform-config.json`을 원본으로 되돌립니다.

## 5. 로컬 설정으로 되돌리기

1. 서버를 Ctrl+C로 종료합니다.
2. Unity에서 1-2의 표를 왼쪽 열(기본) 값으로 되돌립니다.
3. 3장에서 추가한 StageC를 CourseStage 그룹에서 빼고 프리팹을 삭제합니다.
4. 4장에서 지정한 `PlatformConfigLoader`의 Remote Url을 비웁니다.
5. 대상 플랫폼마다 Addressables에서 New Build → Default Build Script를 다시 실행한 뒤 Player를 빌드합니다.

## 한계

- 개발용 localhost 인증서를 쓰기 때문에 같은 PC의 Windows Player에서만 확인했습니다. Android 기기에서 localhost는 기기 자신을 가리킵니다. Android에서 확인하려면 LAN 주소, 인증서 이름, 기기의 인증서 신뢰를 따로 구성해야 합니다(미확인).
- Web Player의 원격 로드는 Web 빌드 출력과 번들을 같은 서버에서 제공하는 구성(Remote.BuildPath `Builds/Web/[BuildTarget]`)으로 수업 중에 확인했습니다. 이 문서의 시나리오로는 다시 측정하지 않았습니다.
- R3, R5, R6은 절차만 정리했고 실행하지 않았습니다. 확인한 시나리오는 R1, R2, R4, 설정 비교 전체입니다.
