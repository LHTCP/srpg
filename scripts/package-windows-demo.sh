#!/usr/bin/env bash
set -euo pipefail

build_dir="${BUILD_DIR:-build/StandaloneWindows64}"
artifact_name="${ARTIFACT_NAME:?ARTIFACT_NAME is required}"
package_root="${PACKAGE_ROOT:-dist/${artifact_name}}"

base_version="${BASE_VERSION:-unknown}"
full_version="${FULL_VERSION:-unknown}"
build_number="${BUILD_NUMBER:-unknown}"
channel="${CHANNEL:-development}"
short_sha="${SHORT_SHA:-unknown}"
commit_sha="${COMMIT_SHA:-unknown}"
branch_name="${BRANCH_NAME:-unknown}"
workflow_run_url="${WORKFLOW_RUN_URL:-unknown}"

if [ ! -d "$build_dir" ]; then
  echo "::error::Windows 빌드 산출물 폴더가 없습니다: $build_dir"
  find build -maxdepth 3 -type f -print || true
  exit 1
fi

mkdir -p "$package_root"
cp -R "$build_dir"/. "$package_root"/

cat > "$package_root/README.txt" <<'README'
SRPG Demo

실행 방법:
1. zip을 원하는 폴더에 압축 해제합니다.
2. Windows 실행 파일을 실행합니다.
3. Windows 보안 경고가 뜨면 배포 출처와 버전을 확인한 뒤 실행 여부를 결정합니다.

Known issue:
- 최신 known issue는 GitHub issue 또는 itch.io 페이지를 확인합니다.
README

cat > "$package_root/BUILD_INFO.txt" <<BUILDINFO
version=${full_version}
base_version=${base_version}
build_number=${build_number}
channel=${channel}
commit=${commit_sha}
short_commit=${short_sha}
branch=${branch_name}
build_time_utc=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
workflow_run=${workflow_run_url}
source_repo=https://github.com/LHTCP/srpg
itch_project=https://lhtcp.itch.io/lhtcp-srpg
BUILDINFO

(cd dist && zip -r "${artifact_name}.zip" "${artifact_name}")
