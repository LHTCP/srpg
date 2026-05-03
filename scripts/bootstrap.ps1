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

if (-not $SkipWorktreeRoot) {
    if (-not (Test-Path -LiteralPath $worktreeRoot)) {
        New-Item -ItemType Directory -Path $worktreeRoot | Out-Null
        Write-Host "[srpg] worktrees 폴더 생성: $worktreeRoot"
    } else {
        Write-Host "[srpg] worktrees 폴더 이미 존재: $worktreeRoot"
    }
}

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
