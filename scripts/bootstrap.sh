#!/usr/bin/env bash
set -euo pipefail

skip_git_safe_directory=0
skip_worktree_root=0

for arg in "$@"; do
  case "$arg" in
    --skip-git-safe-directory)
      skip_git_safe_directory=1
      ;;
    --skip-worktree-root)
      skip_worktree_root=1
      ;;
    *)
      echo "[srpg] 알 수 없는 옵션: $arg" >&2
      exit 1
      ;;
  esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
workspace_root="$(cd "$repo_root/.." && pwd)"
worktree_root="$workspace_root/worktrees"

echo "[srpg] 저장소 루트: $repo_root"
echo "[srpg] 워크스페이스 루트: $workspace_root"

if [[ "$skip_worktree_root" -eq 0 ]]; then
  if [[ ! -d "$worktree_root" ]]; then
    mkdir -p "$worktree_root"
    echo "[srpg] worktrees 폴더 생성: $worktree_root"
  else
    echo "[srpg] worktrees 폴더 이미 존재: $worktree_root"
  fi
fi

if [[ "$skip_git_safe_directory" -eq 0 ]]; then
  normalized_repo_root="${repo_root//\\//}"
  if ! git config --global --get-all safe.directory | grep -Fxq "$normalized_repo_root"; then
    git config --global --add safe.directory "$normalized_repo_root"
    echo "[srpg] Git safe.directory 등록: $normalized_repo_root"
  else
    echo "[srpg] Git safe.directory 이미 등록됨: $normalized_repo_root"
  fi
fi

project_version_file="$repo_root/ProjectSettings/ProjectVersion.txt"
if [[ -f "$project_version_file" ]]; then
  project_version="$(head -n 1 "$project_version_file")"
  echo "[srpg] Unity 버전 확인: $project_version"
fi

echo "[srpg] 완료"
