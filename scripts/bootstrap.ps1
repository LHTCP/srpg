param(
    [switch]$SkipGitSafeDirectory,
    [switch]$SkipWorktreeRoot
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $repoRoot
$worktreeRoot = Join-Path $workspaceRoot "worktrees"

Write-Host "[srpg] 저장소 루트: $repoRoot"
Write-Host "[srpg] 워크스페이스 루트: $workspaceRoot"

# 워크트리는 저장소 밖, 같은 워크스페이스 루트 아래에 모아 병렬 작업 충돌을 줄인다.
if (-not $SkipWorktreeRoot) {
    if (-not (Test-Path -LiteralPath $worktreeRoot)) {
        New-Item -ItemType Directory -Path $worktreeRoot | Out-Null
        Write-Host "[srpg] worktrees 폴더 생성: $worktreeRoot"
    } else {
        Write-Host "[srpg] worktrees 폴더 이미 존재: $worktreeRoot"
    }
}

# 일부 로컬/에이전트 환경에서는 체크아웃 소유자가 달라 Git이 저장소 접근을 막는다.
# 이 설정은 현재 사용자 전역 Git 설정에만 적용된다.
if (-not $SkipGitSafeDirectory) {
    $safeDirectories = git config --global --get-all safe.directory 2>$null

    if ($safeDirectories -notcontains $repoRoot.Replace("\", "/")) {
        git config --global --add safe.directory $repoRoot.Replace("\", "/")
        Write-Host "[srpg] Git safe.directory 등록: $repoRoot"
    } else {
        Write-Host "[srpg] Git safe.directory 이미 등록됨: $repoRoot"
    }
}

$projectVersionFile = Join-Path $repoRoot "ProjectSettings\ProjectVersion.txt"
if (Test-Path -LiteralPath $projectVersionFile) {
    $projectVersion = Get-Content $projectVersionFile | Select-Object -First 1
    Write-Host "[srpg] Unity 버전 확인: $projectVersion"
}

Write-Host "[srpg] 완료"
