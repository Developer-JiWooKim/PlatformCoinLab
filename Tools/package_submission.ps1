# 포트폴리오 제출 폴더를 만듭니다.
# 사용: 저장소 루트에서  powershell -ExecutionPolicy Bypass -File Tools/package_submission.ps1 [-Zip]
# 소스는 현재 커밋(HEAD)의 스냅샷이므로, 커밋하지 않은 변경은 포함되지 않습니다.
param(
    # 저장소 루트의 Submission 폴더 (.gitignore로 제외됨)
    [string]$OutDir = "Submission",
    [switch]$Zip
)

$ErrorActionPreference = "Stop"
$root = (git rev-parse --show-toplevel).Trim()
Set-Location $root

# 1. 작업 트리 확인: 소스 스냅샷과 빌드가 같은 버전인지 보장하기 위해 미커밋 변경이 있으면 중단합니다.
#    (Addressables가 빌드 때 지웠다 만드는 link.xml은 무시)
$dirty = git status --porcelain | Where-Object { $_ -notmatch "AddressableAssetsData/link\.xml" }
if ($dirty) {
    Write-Host "커밋하지 않은 변경이 있습니다. 커밋 후 다시 실행하세요:" -ForegroundColor Red
    $dirty | ForEach-Object { Write-Host "  $_" }
    exit 1
}

$commit = (git rev-parse --short HEAD).Trim()
$out = [System.IO.Path]::GetFullPath((Join-Path $root $OutDir))

if (Test-Path $out) {
    Write-Host "기존 제출 폴더를 지우고 다시 만듭니다: $out"
    Remove-Item -Recurse -Force $out
}
New-Item -ItemType Directory -Force $out | Out-Null

function Export-GitPaths([string]$dest, [string[]]$paths) {
    # git archive는 추적 중인 파일만 내보내므로 Library·Temp·bin·obj·개인 설정이 자동으로 빠집니다.
    $tmp = Join-Path $env:TEMP "pcl_archive_$commit.zip"
    git archive --format=zip -o $tmp HEAD -- @paths
    Expand-Archive -Path $tmp -DestinationPath $dest -Force
    Remove-Item $tmp
}

function Copy-Build([string]$src, [string]$dest) {
    if (-not (Test-Path $src)) { throw "빌드 출력이 없습니다: $src" }
    # IL2CPP 디버그 심볼·Burst 디버그 정보는 배포하지 않는 폴더입니다.
    robocopy $src $dest /E /NFL /NDL /NJH /NJS /NP /XD "*_BackUpThisFolder_ButDontShipItWithYourGame" "*_BurstDebugInformation_DoNotShip" | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "복사 실패: $src (robocopy $LASTEXITCODE)" }
}

Write-Host "[1/6] Unity 프로젝트 (Assets·meta·Packages·ProjectSettings)"
Export-GitPaths (Join-Path $out "UnityProject") @("Assets", "Packages", "ProjectSettings")

Write-Host "[2/6] 빌드 (PC·Android·Web)"
Copy-Build "Builds\PC"      (Join-Path $out "Builds\PC")
Copy-Build "Builds\Android" (Join-Path $out "Builds\Android")
Copy-Build "Builds\Web"     (Join-Path $out "Builds\Web")

Write-Host "[3/6] ContentServer 소스·콘텐츠·설정 (bin·obj 제외)"
Export-GitPaths $out @("Server/ContentServer")
Move-Item (Join-Path $out "Server\ContentServer") (Join-Path $out "ContentServer")
Remove-Item (Join-Path $out "Server")

Write-Host "[4/6] 소개 문서·원격 절차"
$docs = @("Docs\Submission\PORTFOLIO.md", "Docs\Submission\REMOTE_CONTENT_GUIDE.md")
$utf8 = New-Object System.Text.UTF8Encoding $false
foreach ($doc in $docs) {
    if (-not (Test-Path $doc)) { Write-Host "  (없음, 건너뜀) $doc" -ForegroundColor Yellow; continue }
    # 저장소에서는 Docs/Submission/ 기준 상대 링크(../../Assets/...)이므로, 제출 폴더 구조에 맞게 링크를 바꿉니다.
    $text = [System.IO.File]::ReadAllText((Resolve-Path $doc), $utf8)
    $text = $text.Replace("](../../Assets/", "](UnityProject/Assets/")
    $text = $text.Replace("](../../Server/ContentServer/", "](ContentServer/")
    $text = $text.Replace("](../../Tools/", "](Tools/")
    [System.IO.File]::WriteAllText((Join-Path $out (Split-Path $doc -Leaf)), $text, $utf8)
}
# 소개 문서에서 링크하는 패키징 스크립트도 함께 넣습니다.
Export-GitPaths $out @("Tools")

Write-Host "[5/6] 확인 자료 (캡처·로그·Profiler 데이터)"
$evidence = Join-Path $out "Evidence"
New-Item -ItemType Directory -Force $evidence | Out-Null
if (Test-Path "Docs\Submission\Evidence") { Copy-Item "Docs\Submission\Evidence\*" $evidence -Recurse }
if (Test-Path "ProfilerCaptures") { Copy-Item "ProfilerCaptures" (Join-Path $evidence "ProfilerCaptures") -Recurse }

Write-Host "[6/6] 버전 기록"
@"
PlatformCoinLab 제출본
커밋: $commit ($(git log -1 --format=%cd --date=format:'%Y-%m-%d %H:%M'))
생성: $(Get-Date -Format 'yyyy-MM-dd HH:mm')
Unity: $((Get-Content ProjectSettings\ProjectVersion.txt -TotalCount 1) -replace 'm_EditorVersion: ','')
빌드 출력 시각: PC $((Get-Item Builds\PC\PlatformCoinLab.exe).LastWriteTime.ToString('yyyy-MM-dd HH:mm')), Android $((Get-ChildItem Builds\Android\*.apk | Select-Object -First 1).LastWriteTime.ToString('yyyy-MM-dd HH:mm')), Web $((Get-Item Builds\Web\index.html).LastWriteTime.ToString('yyyy-MM-dd HH:mm'))
"@ | Out-File -Encoding utf8 (Join-Path $out "VERSION.txt")

$size = (Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("완료: {0} ({1:N0} MB)" -f $out, $size) -ForegroundColor Green

if ($Zip) {
    $zipPath = "$out.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zipPath
    Write-Host "압축: $zipPath"
}
