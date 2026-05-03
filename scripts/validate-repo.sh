#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Unity 에디터를 띄우기 전에도 잡을 수 있는 저장소 형태의 회귀를 먼저 막는다.
# 이 목록은 "프로젝트가 Unity 프로젝트로 열릴 최소 조건"과 "에이전트 진입 문서"에 집중한다.
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

# ProjectVersion.txt가 Unity 버전의 기준 파일이다.
# AGENTS.md의 버전 표기는 사람이 가장 먼저 보는 안내라서, 이 값과 다르면 문서 드리프트로 본다.
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
# 진입 문서 링크가 끊기면 에이전트/신규 기여자가 서로 다른 절차를 따르기 쉽다.
grep -Fq "docs/project/setup.md" "$repo_root/AGENTS.md"
grep -Fq "docs/project/worktrees.md" "$repo_root/AGENTS.md"
grep -Fq "project/setup.md" "$repo_root/docs/README.md"
grep -Fq "project/worktrees.md" "$repo_root/docs/README.md"

echo "[srpg] Build Settings 씬 등록 확인"
# 씬 파일 존재만으로는 빌드 진입점이 보장되지 않는다.
# Unity 에디터 없이도 EditorBuildSettings.asset의 필수 씬 누락을 빠르게 잡는다.
for scene_name in "SrpgLobby" "SrpgBattle" "SrpgSkillMaker" "SrpgUnitMaker" "SrpgMapMaker"; do
  if ! grep -Fq "Assets/Scenes/${scene_name}.unity" "$repo_root/ProjectSettings/EditorBuildSettings.asset"; then
    echo "[srpg] Build Settings에 씬이 없습니다: ${scene_name}" >&2
    exit 1
  fi
done

echo "[srpg] 패키지 핵심 의존성 확인"
# 핵심 패키지 누락은 Unity import 이후에야 크게 터질 수 있어서, PR 단계에서 먼저 확인한다.
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
