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
    [IO.Path]::Combine($releaseRoot, 'TemplateData', 'cheesetama-game-start-cheese-mark-v003.svg'),
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

$artifactFileNames = @(
    'Release.loader.js',
    'Release.framework.js.unityweb',
    'Release.wasm.unityweb',
    'Release.data.unityweb'
)

$artifactFiles = [Collections.Generic.List[string]]::new()
foreach ($artifactFileName in $artifactFileNames) {
    $artifactFile = [IO.Path]::Combine($buildRoot, $artifactFileName)
    if (-not [IO.File]::Exists($artifactFile)) {
        Stop-Validation "필수 Unity 산출물이 없습니다: $artifactFileName"
    }

    [void]$artifactFiles.Add([IO.Path]::GetFullPath($artifactFile))
}

$releaseIndexPath = [IO.Path]::Combine($releaseRoot, 'index.html')
$releaseIndex = [IO.File]::ReadAllText($releaseIndexPath, [Text.Encoding]::UTF8)
$indexMarkers = @(
    'lang="ko"',
    'name="cheesetama-public-base" content="/play/"',
    'rel="icon" type="image/svg+xml" sizes="any" href="TemplateData/cheesetama-game-start-cheese-mark-v003.svg"',
    'id="cheesetama-brand-symbol"',
    'class="cheese-symbol__image"',
    'src="TemplateData/cheesetama-game-start-cheese-mark-v003.svg"',
    'class="hero-cheese"',
    'id="cheesetama-start"',
    'id="cheesetama-progress"',
    'id="cheesetama-error"',
    'id="cheesetama-retry"',
    'id="cheesetama-version"',
    'autoSyncPersistentDataPath: true',
    'id="cheesetama-compatibility"'
)

foreach ($marker in $indexMarkers) {
    if (-not $releaseIndex.Contains($marker, [StringComparison]::Ordinal)) {
        Stop-Validation "릴리스 index.html 표식이 없습니다: $marker"
    }
}

$forbiddenRoutineCopy = @(
    '게임 기록은 현재 주소와 브라우저 저장공간에 보관됩니다.',
    '시크릿 모드나 브라우저 데이터 삭제 시 기록이 사라질 수 있습니다.',
    '최신 데스크톱 Chrome · Edge · Firefox · Safari 권장',
    '브라우저 실행 환경을 확인했습니다.'
)

foreach ($copy in $forbiddenRoutineCopy) {
    if ($releaseIndex.Contains($copy, [StringComparison]::Ordinal)) {
        Stop-Validation "릴리스 시작 화면에 제거 대상 문구가 남아 있습니다: $copy"
    }
}

if ($releaseIndex.Contains('class="hero-symbol"', [StringComparison]::Ordinal)) {
    Stop-Validation '파비콘을 게임 시작 이미지로 다시 사용하고 있습니다.'
}

$faviconPath = [IO.Path]::Combine(
    $releaseRoot,
    'TemplateData',
    'cheesetama-game-start-cheese-mark-v003.svg')
$faviconSvg = [IO.File]::ReadAllText($faviconPath, [Text.Encoding]::UTF8)
$faviconMarkers = @(
    'id="cheesetama-game-start-cheese-mark-v003"',
    'id="game-start-cheese-art"',
    'id="game-start-cheese-piece"',
    'transform="rotate(-8 32 32)"',
    'transform="matrix(1.35 0 0 1.35 -13.02 -9.81)"',
    'id="game-start-cheese-fill" x1="15.44" y1="8.34" x2="48.56" y2="55.66" gradientUnits="userSpaceOnUse"',
    '<stop offset="0" stop-color="#fff2a2"/>',
    '<stop offset="0.3" stop-color="#fff2a2"/>',
    '<stop offset="0.31" stop-color="#f4c95d"/>',
    '<stop offset="0.72" stop-color="#f4c95d"/>',
    '<stop offset="0.73" stop-color="#d8952e"/>',
    '<stop offset="1" stop-color="#d8952e"/>',
    'id="game-start-cheese-shine" x1="5.94" y1="18.14" x2="58.06" y2="45.86" gradientUnits="userSpaceOnUse"',
    '<stop offset="0" stop-color="#ffffdd" stop-opacity="0.58"/>',
    '<stop offset="0.34" stop-color="#ffffdd" stop-opacity="0"/>',
    '<stop offset="0.72" stop-color="#784217" stop-opacity="0"/>',
    '<stop offset="1" stop-color="#784217" stop-opacity="0.25"/>',
    '<path id="game-start-cheese-body" d="M10.94 42.57 52.12 47.47 43.7 18.03 17.96 24.83Z" fill="url(#game-start-cheese-fill)"/>',
    'd="M10.94 42.57 52.12 47.47 43.7 18.03 17.96 24.83Z"',
    'id="game-start-cheese-hole-one" cx="0.5" cy="0.5" r="0.5"',
    'id="game-start-cheese-hole-two" cx="0.5" cy="0.5" r="0.5"',
    'id="game-start-cheese-hole-three" cx="0.5" cy="0.5" r="0.5"',
    '<circle cx="24.51" cy="34.64" r="3.01" fill="url(#game-start-cheese-hole-one)"/>',
    '<circle cx="38.55" cy="39.17" r="2.78" fill="url(#game-start-cheese-hole-two)"/>',
    '<circle cx="35.74" cy="26.72" r="2.18" fill="url(#game-start-cheese-hole-three)"/>',
    'offset="87.5%" stop-color="#bd7624"',
    '<stop offset="100%" stop-color="#bd7624" stop-opacity="0"/>',
    'offset="85.7143%" stop-color="#b66d21"',
    '<stop offset="100%" stop-color="#b66d21" stop-opacity="0"/>',
    'offset="83.3333%" stop-color="#c68128"',
    '<stop offset="100%" stop-color="#c68128" stop-opacity="0"/>',
    '<path id="game-start-cheese-gloss" d="M10.94 42.57 52.12 47.47 43.7 18.03 17.96 24.83Z" fill="url(#game-start-cheese-shine)"/>'
)
foreach ($marker in $faviconMarkers) {
    if (-not $faviconSvg.Contains($marker, [StringComparison]::Ordinal)) {
        Stop-Validation "게임 시작 치즈 파비콘 표식이 없습니다: $marker"
    }
}

$faviconPathCount = [regex]::Matches($faviconSvg, '<path\b', 'IgnoreCase').Count
$faviconCircleCount = [regex]::Matches($faviconSvg, '<circle\b', 'IgnoreCase').Count
$faviconForbiddenVisibleMarkers = @(
    '<rect',
    '<ellipse',
    '<polygon',
    '<polyline',
    '<line ',
    '<line>',
    '<line/',
    '<image',
    '<use',
    '<text',
    '<style',
    '<animate',
    '<set',
    '<clipPath',
    '<mask',
    'stroke='
)
if ($faviconPathCount -ne 2 -or $faviconCircleCount -ne 3) {
    Stop-Validation "게임 시작 치즈 파비콘 가시 요소 수가 잘못되었습니다: path=$faviconPathCount, circle=$faviconCircleCount"
}
foreach ($marker in $faviconForbiddenVisibleMarkers) {
    if ($faviconSvg.Contains($marker, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-Validation "게임 시작 치즈 파비콘에 허용하지 않은 가시 요소가 있습니다: $marker"
    }
}

$faviconLayerMarkers = @(
    'id="game-start-cheese-body"',
    'fill="url(#game-start-cheese-hole-one)"',
    'fill="url(#game-start-cheese-hole-two)"',
    'fill="url(#game-start-cheese-hole-three)"',
    'id="game-start-cheese-gloss"'
)
$previousLayerIndex = -1
foreach ($marker in $faviconLayerMarkers) {
    $layerIndex = $faviconSvg.IndexOf($marker, [StringComparison]::Ordinal)
    if ($layerIndex -le $previousLayerIndex) {
        Stop-Validation "게임 시작 치즈 파비콘 레이어 순서가 잘못되었습니다: $marker"
    }

    $previousLayerIndex = $layerIndex
}

$faviconShadowMarkers = @(
    'fill="#190e08"',
    'opacity="0.32"',
    'translate(0 4)',
    '<filter',
    'filter='
)
foreach ($marker in $faviconShadowMarkers) {
    if ($faviconSvg.Contains($marker, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-Validation "게임 시작 치즈 파비콘에 제거 대상 그림자 표식이 남아 있습니다: $marker"
    }
}

$faviconSparkleMarkers = @(
    'id="spark',
    'class="spark',
    'class="hero-cheese__spark',
    '#fff0a6'
)
foreach ($marker in $faviconSparkleMarkers) {
    if ($faviconSvg.Contains($marker, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-Validation "게임 시작 치즈 파비콘에 제거 대상 별빛 표식이 남아 있습니다: $marker"
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

$allowedOutputFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($requiredFile in $requiredFiles) {
    [void]$allowedOutputFiles.Add([IO.Path]::GetFullPath($requiredFile))
}
foreach ($artifactFile in $artifactFiles) {
    [void]$allowedOutputFiles.Add($artifactFile)
}

$unexpectedOutput = @(Get-ChildItem -LiteralPath $fullSiteRoot -File -Recurse -Force | Where-Object {
    -not $allowedOutputFiles.Contains([IO.Path]::GetFullPath($_.FullName))
})
if ($unexpectedOutput.Count -gt 0) {
    Stop-Validation "배포 계약에 없는 추가 파일이 포함되어 있습니다: $($unexpectedOutput.Count)개"
}

$textExtensions = @('.html', '.css', '.js', '.json', '.txt', '.md', '.svg', '.xml', '.yml', '.yaml')
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
