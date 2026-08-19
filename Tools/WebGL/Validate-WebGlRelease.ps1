[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SiteRoot,

    [string]$PublicBasePath = '/play/'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Stop-Validation {
    param([string]$Message)

    throw "WebGL 배포 검증 실패: $Message"
}

if (-not $PublicBasePath.StartsWith('/') -or -not $PublicBasePath.EndsWith('/')) {
    Stop-Validation '공개 경로는 앞뒤에 슬래시가 있어야 합니다.'
}

if ($PublicBasePath.Contains('..') -or $PublicBasePath.Contains('?') -or $PublicBasePath.Contains('#')) {
    Stop-Validation '공개 경로에 상위 이동, 쿼리 또는 해시를 사용할 수 없습니다.'
}

$fullSiteRoot = [IO.Path]::GetFullPath($SiteRoot)
if (-not [IO.Directory]::Exists($fullSiteRoot)) {
    Stop-Validation "사이트 루트가 없습니다: $SiteRoot"
}

$relativeBase = $PublicBasePath.Trim('/').Replace('/', [IO.Path]::DirectorySeparatorChar)
$releaseRoot = [IO.Path]::GetFullPath([IO.Path]::Combine($fullSiteRoot, $relativeBase))
$rootPrefix = $fullSiteRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $releaseRoot.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    Stop-Validation '공개 경로가 사이트 루트 밖을 가리킵니다.'
}

$requiredFiles = @(
    [IO.Path]::Combine($fullSiteRoot, 'index.html'),
    [IO.Path]::Combine($fullSiteRoot, '_headers'),
    [IO.Path]::Combine($fullSiteRoot, '_redirects'),
    [IO.Path]::Combine($releaseRoot, 'index.html'),
    [IO.Path]::Combine($releaseRoot, 'TemplateData', 'style.css'),
    [IO.Path]::Combine($releaseRoot, 'ThirdPartyNotices', 'NanumGothic-OFL.txt')
)

foreach ($requiredFile in $requiredFiles) {
    if (-not [IO.File]::Exists($requiredFile)) {
        Stop-Validation "필수 파일이 없습니다: $([IO.Path]::GetFileName($requiredFile))"
    }
}

$buildRoot = [IO.Path]::Combine($releaseRoot, 'Build')
if (-not [IO.Directory]::Exists($buildRoot)) {
    Stop-Validation 'Build 폴더가 없습니다.'
}

$artifactPatterns = @(
    '*.loader.js',
    '*.framework.js.unityweb',
    '*.wasm.unityweb',
    '*.data.unityweb'
)

foreach ($pattern in $artifactPatterns) {
    $matches = @(Get-ChildItem -LiteralPath $buildRoot -File -Recurse -Filter $pattern)
    if ($matches.Count -ne 1) {
        Stop-Validation "$pattern 산출물은 정확히 1개여야 하지만 $($matches.Count)개입니다."
    }
}

$releaseIndexPath = [IO.Path]::Combine($releaseRoot, 'index.html')
$releaseIndex = [IO.File]::ReadAllText($releaseIndexPath, [Text.Encoding]::UTF8)
$indexMarkers = @(
    'lang="ko"',
    'name="cheesetama-public-base" content="/play/"',
    'id="cheesetama-start"',
    'id="cheesetama-progress"',
    'id="cheesetama-error"',
    'id="cheesetama-retry"',
    'id="cheesetama-version"',
    'autoSyncPersistentDataPath: true'
)

foreach ($marker in $indexMarkers) {
    if (-not $releaseIndex.Contains($marker, [StringComparison]::Ordinal)) {
        Stop-Validation "릴리스 index.html 표식이 없습니다: $marker"
    }
}

if ($releaseIndex.Contains('{{{', [StringComparison]::Ordinal) -or
    $releaseIndex -match '(?m)^\s*#(?:if|else|endif)\b') {
    Stop-Validation '처리되지 않은 Unity 템플릿 지시문이 남아 있습니다.'
}

$headers = [IO.File]::ReadAllText(
    [IO.Path]::Combine($fullSiteRoot, '_headers'),
    [Text.Encoding]::UTF8)
$headerMarkers = @(
    '/play/Build/*.framework.js.unityweb',
    '/play/Build/*.wasm.unityweb',
    '/play/Build/*.data.unityweb',
    'Content-Type: application/wasm',
    'Content-Encoding: gzip',
    'Cache-Control: no-cache, must-revalidate',
    'Cache-Control: no-cache, no-store, must-revalidate'
)

foreach ($marker in $headerMarkers) {
    if (-not $headers.Contains($marker, [StringComparison]::Ordinal)) {
        Stop-Validation "호스트 헤더 계약이 없습니다: $marker"
    }
}

$blockedOutput = @(Get-ChildItem -LiteralPath $fullSiteRoot -Recurse -Force | Where-Object {
    $_.Name -match '(?i)(DoNotShip|\.pdb$|\.map$|AGENTS?\.md$|기획안|작업기록|작업일지|worklog)'
})
if ($blockedOutput.Count -gt 0) {
    Stop-Validation "게시 금지 파일이 포함되어 있습니다: $($blockedOutput[0].Name)"
}

$textExtensions = @('.html', '.css', '.js', '.json', '.txt', '.md', '.xml', '.yml', '.yaml')
$textFiles = @(Get-ChildItem -LiteralPath $fullSiteRoot -File -Recurse -Force | Where-Object {
    $textExtensions -contains $_.Extension.ToLowerInvariant() -or $_.Name -in @('_headers', '_redirects')
})
$sensitivePatterns = [ordered]@{
    'Windows 사용자 경로' = '(?i)[A-Z]:[\\/]+Users[\\/]'
    'macOS 사용자 경로' = '(?i)/Users/[^/\s]+'
    'Linux 사용자 경로' = '(?i)/home/[^/\s]+'
    '개인 키' = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'
    'AWS 접근 키' = '\bAKIA[0-9A-Z]{16}\b'
    '긴 API 토큰' = '\bsk-[A-Za-z0-9_-]{20,}\b'
    '이메일 주소' = '\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b'
    '내부 문서 경로' = '(?i)Docs[\\/]Internal|GAME_DESIGN\.md|CHANGELOG\.md'
}

foreach ($textFile in $textFiles) {
    $contents = [IO.File]::ReadAllText($textFile.FullName, [Text.Encoding]::UTF8)
    foreach ($entry in $sensitivePatterns.GetEnumerator()) {
        if ($contents -match $entry.Value) {
            Stop-Validation "$($entry.Key) 흔적이 있습니다: $($textFile.Name)"
        }
    }
}

$fileCount = @(Get-ChildItem -LiteralPath $fullSiteRoot -File -Recurse -Force).Count
$totalBytes = (Get-ChildItem -LiteralPath $fullSiteRoot -File -Recurse -Force |
    Measure-Object -Property Length -Sum).Sum

Write-Host "WebGL 배포 검증 통과: $PublicBasePath / 파일 $fileCount 개 / $totalBytes 바이트"
