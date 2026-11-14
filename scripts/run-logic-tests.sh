#!/usr/bin/env bash
set -euo pipefail

# Unity 없이 순수 전투 로직과 EditMode 테스트를 .NET SDK로 컴파일·실행한다.
# Unity 라이선스가 없는 PC와 PR CI에서 전투 규칙 회귀와 로직 코드의 컴파일 오류를 잡는 용도다.
# 컨트롤러·HUD·메이커 화면 코드는 컴파일하지 않는다. 그쪽은 Unity 에디터에서 확인해야 한다.
# 설명: docs/project/logic-tests.md

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/tools/logic-tests/LogicTests.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[srpg] dotnet SDK가 없습니다. .NET SDK 8 이상을 설치한 뒤 다시 실행하세요." >&2
  exit 1
fi

# AI 시뮬레이션 리포트는 테스트 실행 폴더(tools/logic-tests/bin/...) 아래 TestResults/SrpSim/에 생긴다(.gitignore 대상).
cd "$repo_root"

echo "[srpg] 전투 로직 테스트 실행 (.NET)"
# RequiresUnity: Unity 타입이나 컨트롤러가 있어야 의미가 있는 테스트. 스텁으로는 검증할 수 없어 뺀다.
# 추가 인자는 그대로 넘긴다. 예: bash ./scripts/run-logic-tests.sh --filter "FullyQualifiedName~SrpM1SkillMapTests"
if [[ $# -gt 0 ]]; then
  dotnet test "$project" --nologo "$@"
else
  dotnet test "$project" --nologo --filter "TestCategory!=RequiresUnity"
fi

echo "[srpg] 완료"
