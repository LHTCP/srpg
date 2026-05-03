#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

required_paths=(
  "Assets"
  "Packages"
  "ProjectSettings"
  "docs"
  "AGENTS.md"
  "Packages/manifest.json"
  "ProjectSettings/ProjectVersion.txt"
  "ProjectSettings/EditorBuildSettings.asset"
  "docs/README.md"
  "docs/project/setup.md"
  "docs/project/workflow.md"
  "docs/project/worktrees.md"
)

echo "[srpg] 저장소 기본 구조 확인"
for path in "${required_paths[@]}"; do
  if [[ ! -e "$repo_root/$path" ]]; then
    echo "[srpg] 누락: $path" >&2
    exit 1
  fi
done

echo "[srpg] Unity 버전 확인"
project_version_line="$(head -n 1 "$repo_root/ProjectSettings/ProjectVersion.txt")"
project_version="${project_version_line#m_EditorVersion: }"

if [[ -z "$project_version" || "$project_version" == "$project_version_line" ]]; then
  echo "[srpg] ProjectVersion.txt 형식을 해석하지 못했습니다: $project_version_line" >&2
  exit 1
fi

agents_version_line="$(grep -E '^- \*\*Unity 에디터\*\*:' "$repo_root/AGENTS.md" || true)"
if [[ -z "$agents_version_line" ]]; then
  echo "[srpg] AGENTS.md에서 Unity 버전 안내를 찾지 못했습니다." >&2
  exit 1
fi

if [[ "$agents_version_line" != *"$project_version"* ]]; then
  echo "[srpg] AGENTS.md Unity 버전이 ProjectVersion.txt와 다릅니다." >&2
  echo "[srpg] ProjectVersion.txt: $project_version" >&2
  echo "[srpg] AGENTS.md: $agents_version_line" >&2
  exit 1
fi

echo "[srpg] 필수 문서 연결 확인"
grep -Fq "docs/project/setup.md" "$repo_root/AGENTS.md"
grep -Fq "docs/project/worktrees.md" "$repo_root/AGENTS.md"
grep -Fq "project/setup.md" "$repo_root/docs/README.md"
grep -Fq "project/worktrees.md" "$repo_root/docs/README.md"

echo "[srpg] Build Settings 씬 등록 확인"
for scene_name in "SrpgLobby" "SrpgBattle" "SrpgSkillMaker" "SrpgUnitMaker" "SrpgMapMaker"; do
  if ! grep -Fq "Assets/Scenes/${scene_name}.unity" "$repo_root/ProjectSettings/EditorBuildSettings.asset"; then
    echo "[srpg] Build Settings에 씬이 없습니다: ${scene_name}" >&2
    exit 1
  fi
done

echo "[srpg] 패키지 핵심 의존성 확인"
for package_name in \
  "com.unity.inputsystem" \
  "com.unity.render-pipelines.universal" \
  "com.unity.test-framework"
do
  if ! grep -Fq "\"${package_name}\"" "$repo_root/Packages/manifest.json"; then
    echo "[srpg] manifest.json에 핵심 패키지가 없습니다: ${package_name}" >&2
    exit 1
  fi
done

echo "[srpg] 완료"
