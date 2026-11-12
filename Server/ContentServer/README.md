# ContentServer

PlatformCoinLab의 원격 콘텐츠(Addressables 번들·카탈로그), JSON 설정, Web 빌드를 HTTPS로 제공하는 ASP.NET Core 정적 파일 서버입니다. 기본 게임은 이 서버 없이 로컬 콘텐츠로 실행되며, 이 서버는 원격 콘텐츠·설정 비교 절차에만 사용합니다.

## 요구 환경

- .NET 10 SDK (확인: `dotnet --list-sdks`)
- 개발용 HTTPS 인증서: `dotnet dev-certs https --check --trust`로 확인하고, 없으면 `dotnet dev-certs https --trust`를 실행

## 폴더 구성

```text
PlatformCoinLab_Private/          Unity 프로젝트 루트 (저장소 루트)
  Assets/ Packages/ ProjectSettings/
  ServerData/                     원격 Addressables 빌드 출력 (FilesRoot: ../../ServerData)
  Builds/Web/                     Web Player 출력 (FilesRoot: ../../Builds/Web, Git 제외)
  Server/
    ContentServer/                이 서버 (Assets 밖이므로 Unity가 가져오지 않음)
      Program.cs
      ContentServer.csproj
      Content/
        platform-config.json        정상 설정 (45초)
        broken-config.json          JSON 문법 오류 → 기본값 30초 확인용
        invalid-range-config.json   범위 밖 값(500) → 기본값 30초 확인용
```

`--FilesRoot`의 상대 경로는 ContentServer 폴더 기준입니다.

## 실행

ContentServer 폴더에서 실행합니다. 종료는 Ctrl+C입니다.

| 목적 | 명령 |
| :--- | :--- |
| JSON 설정 제공 (기본 Content 폴더) | `dotnet run --no-launch-profile` |
| 원격 Addressables 번들 제공 | `dotnet run --no-launch-profile -- --FilesRoot "../../ServerData"` |
| Web 빌드 제공 | `dotnet run --no-launch-profile -- --FilesRoot "../../Builds/Web"` |
| 다른 주소 사용 | `--Urls "https://localhost:6443"` 추가 |

기본 주소는 `https://localhost:5443`이며 Unity의 RemotePractice 프로필 `Remote.LoadPath`(`https://localhost:5443/[BuildTarget]`)와 일치해야 합니다.

## 동작

- `GET /health` → `{"status":"ok","root":"..."}`
- 등록된 확장자만 제공합니다: `.bundle .hash .json .bin .html .js .css .wasm .data .png .ico .svg .gz .br .unityweb`. 이외 확장자(`.cs` 등)와 없는 파일은 404를 반환합니다.
- 모든 응답에 `Cache-Control: no-cache`를 붙입니다. HTTP 재검증 정책이며 Unity 번들 캐시를 지우지 않습니다.
- `.gz`·`.br` 파일은 원래 형식의 Content-Type과 `Content-Encoding`을 붙여 보냅니다(DAY 15 Web 압축 비교).
- 요청마다 콘솔에 `[시각] 메서드 경로 -> 상태코드 (ms)`를 출력합니다. 번들·카탈로그 요청 여부와 실패 원인을 확인할 때 사용합니다.

## 실패 구분

| 상황 | 클라이언트 결과 | 서버 로그 |
| :--- | :--- | :--- |
| 정상 | 200 | `-> 200` |
| 파일 없음·미등록 확장자 | 404 | `-> 404` |
| 서버 중지 | 연결 실패 (Cannot connect) | 출력 없음 |
| 인증서 미신뢰 | SSL/인증서 오류 | 출력 없음 또는 TLS 경고 |

인증서 오류를 CertificateHandler로 무조건 통과시키는 방식은 사용하지 않습니다.

## 검증 기록 (2026-11-12, Windows 10, .NET SDK 10.0.403)

| 요청 | 결과 |
| :--- | :--- |
| `/health` | 200 application/json |
| `/StandaloneWindows64/catalog_0.1.0.json` | 200 application/json |
| `/StandaloneWindows64/catalog_0.1.0.hash` | 200 text/plain |
| `/StandaloneWindows64/coursestage_…bundle` | 200 application/octet-stream (68,373B) |
| `/WebGL/catalog_0.1.0.bin` | 200 application/octet-stream |
| `/missing.json` | 404 |
| `/Program.cs` (Content 루트) | 404 (미등록 확장자) |
| 서버 중지 후 `/health` | 연결 실패 |
