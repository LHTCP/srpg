#!/usr/bin/env bash
set -euo pipefail

# Unity를 실행하지 않고, Unity의 어셈블리 구성(asmdef 경계, 에디터/플레이어)대로 C# 소스를 컴파일해 오류를 찾는다.
# 전투 로직 테스트(run-logic-tests.sh)가 컴파일하지 않는 컨트롤러·HUD·메이커·로비 코드와 PlayMode 테스트까지 본다.
# 설치된 Unity의 DLL과 Unity가 만든 Library가 있어야 하므로 PR CI에서는 돌리지 않는다.
# 설명: docs/project/logic-tests.md
#
# 종료 코드
#   0  통과: 고른 변형의 모든 어셈블리를 컴파일했고 오류가 없다.
#   1  실패: 컴파일 오류가 있거나, 확인 자체가 성립하지 않는다(소스 0개, 참조 DLL 누락, 대조 불일치 등).
#   2  건너뜀: Unity 설치나 Library가 없어 컴파일하지 않았다. 통과가 아니다.

exit_fail=1
exit_skip=2

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tool_dir="$repo_root/tools/compile-check"
out_dir="$tool_dir/obj"

# 변형별 어셈블리. 참조되는 쪽이 먼저 오도록 적는다. tools/compile-check의 프로젝트 폴더와 같아야 한다.
editor_assemblies="Srp.Runtime Srp.Simulation Srp.PlayMode.Tests Assembly-CSharp Assembly-CSharp-Editor"
player_assemblies="Srp.Runtime Srp.Simulation Assembly-CSharp"

# 소스가 쓰는 전처리 기호 중 변형별 정의 여부를 정해 둔 것(tools/compile-check/Directory.Build.targets의 DefineConstants).
# 여기에 없는 기호를 소스가 쓰면 그 구간을 Unity와 다르게 컴파일하게 되므로 실패로 알린다.
known_symbols="UNITY_EDITOR UNITY_INCLUDE_TESTS"

say() { printf '[srpg] %s\n' "$*"; }
warn() { printf '[srpg] %s\n' "$*" >&2; }

# 컴파일을 시작하기 전에 멈춘다. 첫 인자는 이유, 나머지는 덧붙일 안내 줄이다.
die() {
  warn "실패: $1"
  shift
  local line
  for line in "$@"; do
    warn "  $line"
  done
  warn "결과: 실패 — 컴파일을 시작하지 못했습니다. (종료 코드 $exit_fail)"
  exit "$exit_fail"
}

usage() {
  cat <<'EOF'
사용법: bash ./scripts/run-compile-check.sh [옵션]

  --variant editor|player|all  확인할 변형 (기본 all)
  --unity <경로>               Unity 설치 경로. 버전 폴더, Editor 폴더, Data 폴더 어느 것이든 된다.
                               기본: ProjectSettings/ProjectVersion.txt의 버전을 Unity Hub 기본 설치 위치에서 찾는다.
                               환경 변수 SRPG_UNITY_PATH로도 줄 수 있다.
  --library <경로>             Unity가 만든 Library 폴더.
                               기본: 소스 루트의 Library, 없으면(워크트리) 메인 체크아웃의 Library.
                               환경 변수 SRPG_UNITY_LIBRARY로도 줄 수 있다.
  --source <경로>              확인할 Unity 프로젝트 루트 (기본: 이 저장소). 다른 워크트리나 내보낸 사본을 볼 때 쓴다.
  --warnings                   경고 목록도 출력한다 (기본은 개수만).
  -h, --help                   이 도움말.

종료 코드: 0 통과 / 1 실패 / 2 건너뜀(Unity나 Library가 없어 확인하지 않음)
EOF
}

# Windows(Git Bash)에서는 dotnet에 넘기거나 화면에 보일 경로를 C:/... 꼴로 바꾼다. 다른 OS에서는 그대로 둔다.
to_native() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -m "$1"
  else
    printf '%s\n' "$1"
  fi
}

count_lines() {
  if [[ -z "$1" ]]; then
    echo 0
  else
    printf '%s\n' "$1" | wc -l | tr -d '[:space:]'
  fi
}

# 인자로 받은 경로 아래에서 Managed/UnityEngine이 있는 Data 폴더를 찾아 출력한다.
resolve_unity_data() {
  local base="$1" candidate
  for candidate in "$base" "$base/Editor/Data" "$base/Data" "$base/Unity.app/Contents" "$base/Contents"; do
    if [[ -f "$candidate/Managed/UnityEngine/UnityEngine.dll" ]]; then
      (cd "$candidate" && pwd)
      return 0
    fi
  done
  return 1
}

# 그 폴더가 속한 git 저장소의 메인 체크아웃 루트를 출력한다. 워크트리에는 Library가 없어서 필요하다.
main_checkout_of() {
  local root="$1" common_dir
  command -v git >/dev/null 2>&1 || return 1
  common_dir="$(cd "$root" && git rev-parse --git-common-dir 2>/dev/null)" || return 1
  [[ -n "$common_dir" ]] || return 1
  (cd "$root" && cd "$common_dir/.." 2>/dev/null && pwd)
}

variant_arg="all"
unity_arg="${SRPG_UNITY_PATH:-}"
library_arg="${SRPG_UNITY_LIBRARY:-}"
source_arg=""
show_warnings=0

need_value() {
  if [[ $# -lt 2 || -z "$2" ]]; then
    warn "$1 뒤에 값이 필요합니다."
    exit "$exit_fail"
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --variant) need_value "$@"; variant_arg="$2"; shift 2 ;;
    --variant=*) variant_arg="${1#*=}"; shift ;;
    --unity) need_value "$@"; unity_arg="$2"; shift 2 ;;
    --unity=*) unity_arg="${1#*=}"; shift ;;
    --library) need_value "$@"; library_arg="$2"; shift 2 ;;
    --library=*) library_arg="${1#*=}"; shift ;;
    --source) need_value "$@"; source_arg="$2"; shift 2 ;;
    --source=*) source_arg="${1#*=}"; shift ;;
    --warnings) show_warnings=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *)
      warn "알 수 없는 인자: $1"
      usage >&2
      exit "$exit_fail"
      ;;
  esac
done

case "$variant_arg" in
  all) variants="editor player" ;;
  editor|player) variants="$variant_arg" ;;
  *)
    warn "--variant는 editor, player, all 중 하나여야 합니다: $variant_arg"
    exit "$exit_fail"
    ;;
esac

say "어셈블리별 컴파일 확인 (Unity 실행 없이 Unity DLL을 참조해 컴파일)"

if [[ ! -f "$tool_dir/Directory.Build.props" ]]; then
  die "프로젝트 파일이 없습니다: $(to_native "$tool_dir")"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  die "dotnet SDK가 없습니다. .NET SDK 8 이상을 설치한 뒤 다시 실행하세요."
fi

# ---------------------------------------------------------------------------
# 소스 루트
# ---------------------------------------------------------------------------
if [[ -n "$source_arg" ]]; then
  if [[ ! -d "$source_arg" ]]; then
    die "--source 경로가 없습니다: $source_arg"
  fi
  source_root="$(cd "$source_arg" && pwd)"
else
  source_root="$repo_root"
fi

version_file="$source_root/ProjectSettings/ProjectVersion.txt"
if [[ ! -d "$source_root/Assets" || ! -f "$version_file" ]]; then
  die "Unity 프로젝트 루트가 아닙니다(Assets 또는 ProjectSettings/ProjectVersion.txt 없음): $(to_native "$source_root")"
fi

version_line="$(head -n 1 "$version_file" | tr -d '\r')"
unity_version="${version_line#m_EditorVersion: }"
if [[ -z "$unity_version" || "$unity_version" == "$version_line" ]]; then
  die "ProjectVersion.txt 형식을 해석하지 못했습니다: $version_line"
fi

say "소스: $(to_native "$source_root")"

# ---------------------------------------------------------------------------
# Unity 설치와 Library 찾기. 없으면 무엇이 없는지 알리고 건너뛴다(종료 코드 2).
# 경로를 직접 지정했는데 틀렸다면 건너뛰지 않고 실패한다.
# ---------------------------------------------------------------------------
skip_reasons=""
add_skip_reason() { skip_reasons="${skip_reasons}$1"$'\n'; }

unity_data=""
unity_label="Unity $unity_version"
if [[ -n "$unity_arg" ]]; then
  if ! unity_data="$(resolve_unity_data "$unity_arg")"; then
    die "지정한 Unity 경로에서 Managed/UnityEngine/UnityEngine.dll을 찾지 못했습니다: $unity_arg"
  fi
  unity_label="Unity (지정한 경로, 프로젝트 버전은 $unity_version)"
else
  # Unity Hub 기본 설치 위치
  case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*)
      program_files="${ProgramW6432:-${PROGRAMFILES:-C:/Program Files}}"
      if command -v cygpath >/dev/null 2>&1; then
        program_files="$(cygpath -u "$program_files")"
      fi
      hub_candidate="$program_files/Unity/Hub/Editor/$unity_version"
      ;;
    Darwin)
      hub_candidate="/Applications/Unity/Hub/Editor/$unity_version"
      ;;
    *)
      hub_candidate="${HOME:-}/Unity/Hub/Editor/$unity_version"
      ;;
  esac
  if ! unity_data="$(resolve_unity_data "$hub_candidate")"; then
    unity_data=""
    add_skip_reason "Unity $unity_version 설치를 찾지 못했습니다. 찾아본 위치(Unity Hub 기본 설치 위치):"
    add_skip_reason "  - $(to_native "$hub_candidate")"
    add_skip_reason "  다른 위치에 설치했다면 --unity <경로> 또는 환경 변수 SRPG_UNITY_PATH로 알려 주세요."
  fi
fi

# Library: ScriptAssemblies가 있어야 쓸 수 있다.
library=""
library_note=""
if [[ -n "$library_arg" ]]; then
  if [[ ! -d "$library_arg/ScriptAssemblies" ]]; then
    die "지정한 Library에 ScriptAssemblies 폴더가 없습니다: $library_arg"
  fi
  library="$(cd "$library_arg" && pwd)"
else
  # 후보는 "경로|설명" 꼴이다. 설명은 소스 루트 밖의 Library를 쓸 때 출력한다.
  library_candidates="$source_root|"
  if main_root="$(main_checkout_of "$source_root")" && [[ "$main_root" != "$source_root" ]]; then
    library_candidates="$library_candidates"$'\n'"$main_root|메인 체크아웃의 것"
  fi
  if [[ "$source_root" != "$repo_root" ]]; then
    # --source로 다른 사본을 볼 때는 이 저장소(와 그 메인 체크아웃)의 Library를 빌려 쓴다. 같은 프로젝트이기 때문이다.
    library_candidates="$library_candidates"$'\n'"$repo_root|이 저장소의 것"
    if main_root="$(main_checkout_of "$repo_root")" && [[ "$main_root" != "$repo_root" ]]; then
      library_candidates="$library_candidates"$'\n'"$main_root|이 저장소 메인 체크아웃의 것"
    fi
  fi
  while IFS='|' read -r candidate candidate_label; do
    if [[ -d "$candidate/Library/ScriptAssemblies" ]]; then
      library="$candidate/Library"
      library_note="${candidate_label:+ ($candidate_label)}"
      break
    fi
  done <<EOF
$library_candidates
EOF
  if [[ -z "$library" ]]; then
    add_skip_reason "Unity가 만든 Library(ScriptAssemblies 포함)를 찾지 못했습니다. 찾아본 위치:"
    while IFS='|' read -r candidate candidate_label; do
      add_skip_reason "  - $(to_native "$candidate")/Library"
    done <<EOF
$library_candidates
EOF
    add_skip_reason "  Unity에서 프로젝트를 한 번 열면 생깁니다. 다른 위치에 있다면 --library <경로> 또는 환경 변수 SRPG_UNITY_LIBRARY로 알려 주세요."
  fi
fi

if [[ -n "$skip_reasons" ]]; then
  printf '%s' "$skip_reasons" | while IFS= read -r line; do
    warn "$line"
  done
  warn "결과: 건너뜀 — 컴파일 확인을 하지 않았습니다. 통과로 보고하지 마세요. (종료 코드 $exit_skip)"
  exit "$exit_skip"
fi

say "$unity_label: $(to_native "$unity_data")"
say "Library: $(to_native "$library")$library_note"

# Library의 패키지 DLL은 Unity가 마지막으로 컴파일한 시점의 것이다. 소스가 그 뒤에 패키지를 더하거나 버전을 바꿨다면
# 옛 DLL로 확인하게 된다. 이 PC에서 Unity를 못 여는 경우도 있어 실패로 막지는 않고, 결과에 주의로 남긴다.
caveat=""
lock_file="$source_root/Packages/packages-lock.json"
if [[ -f "$lock_file" ]]; then
  package_checked=0
  package_diff=""
  while read -r package_name package_version; do
    [[ -n "$package_name" ]] || continue
    package_checked=$((package_checked + 1))
    cached_version=""
    for candidate in "$library"/PackageCache/"$package_name"@*/package.json; do
      [[ -f "$candidate" ]] || continue
      cached_version="$(tr -d '\r' <"$candidate" | sed -n 's/^[[:space:]]*"version":[[:space:]]*"\([^"]*\)".*/\1/p' | awk 'NR == 1')"
    done
    if [[ -z "$cached_version" ]]; then
      package_diff="${package_diff}    $package_name $package_version: Library에 없음"$'\n'
    elif [[ "$cached_version" != "$package_version" ]]; then
      package_diff="${package_diff}    $package_name: 소스 $package_version, Library $cached_version"$'\n'
    fi
  done <<EOF
$(tr -d '\r' <"$lock_file" | awk '
  /^    "[^"]+":[[:space:]]*\{/ { split($0, part, "\""); name = part[2]; next }
  /^      "version":/ { split($0, part, "\""); version = part[4]; next }
  /^      "source":/ {
    split($0, part, "\"")
    # 엔진 모듈(com.unity.modules.*)은 패키지 캐시에 버전 폴더가 없고, 로컬·내장 패키지는 Library 밖에 있다.
    if ((part[4] == "registry" || part[4] == "builtin") && name !~ /^com\.unity\.modules\./) print name, version
  }')
EOF
  if [[ -n "$package_diff" ]]; then
    caveat="Library의 패키지가 소스의 Packages/packages-lock.json과 다릅니다. Unity에서 프로젝트를 열어 Library를 갱신하기 전에는 그 패키지에 닿는 코드의 결과를 믿을 수 없습니다."
    say "주의: $caveat"
    printf '%s' "$package_diff" | while IFS= read -r line; do say "$line"; done
  elif [[ "$package_checked" -eq 0 ]]; then
    caveat="Packages/packages-lock.json에서 패키지 목록을 읽지 못해 Library의 패키지 버전을 대조하지 못했습니다."
    say "주의: $caveat"
  else
    say "패키지 대조: 패키지 ${package_checked}개의 버전이 소스와 Library에서 같음"
  fi
else
  say "패키지 대조: 소스에 Packages/packages-lock.json이 없어 하지 않음"
fi

# 플레이어용 엔진 DLL. 에디터용과 달리 에디터 전용 멤버가 빠져 있어, 플레이어 빌드에서만 나는 오류를 잡는다.
# Windows 빌드 지원 모듈이 없으면 에디터용 엔진 DLL로 대신한다(그만큼 덜 잡는다).
player_managed=""
for playback_root in "$unity_data/PlaybackEngines" "$unity_data/../../PlaybackEngines"; do
  [[ -d "$playback_root" ]] || continue
  for support_dir in "$playback_root"/*; do
    case "$(basename "$support_dir" | tr '[:upper:]' '[:lower:]')" in
      windowsstandalonesupport)
        if [[ -f "$support_dir/Variations/mono/Managed/UnityEngine.dll" ]]; then
          player_managed="$(cd "$support_dir/Variations/mono/Managed" && pwd)"
        fi
        ;;
    esac
  done
  [[ -n "$player_managed" ]] && break
done
player_note=""
if [[ -z "$player_managed" ]]; then
  player_managed="$unity_data/Managed/UnityEngine"
  player_note=" (Windows 플레이어용 엔진 DLL이 없어 에디터용으로 대신함: UnityEngine의 에디터 전용 멤버 사용은 못 잡음)"
fi
case " $variants " in
  *" player "*) say "플레이어용 엔진 DLL: $(to_native "$player_managed")$player_note" ;;
esac

# nunit: 패키지 캐시 폴더 이름의 해시는 패키지 버전마다 달라서 찾아 쓴다. editor 변형에만 필요하다.
nunit_dll=""
case " $variants " in
  *" editor "*)
    nunit_found=""
    for candidate in "$library"/PackageCache/com.unity.ext.nunit@*/net*/unity-custom/nunit.framework.dll; do
      [[ -f "$candidate" ]] || continue
      nunit_found="${nunit_found}${candidate}"$'\n'
    done
    nunit_found="${nunit_found%$'\n'}"
    nunit_count="$(count_lines "$nunit_found")"
    if [[ "$nunit_count" -eq 0 ]]; then
      die "필요한 참조 DLL이 없습니다: $(to_native "$library")/PackageCache/com.unity.ext.nunit@*/net*/unity-custom/nunit.framework.dll" \
        "Unity에서 프로젝트를 열어 패키지를 받은 Library인지 확인하세요."
    fi
    if [[ "$nunit_count" -gt 1 ]]; then
      die "nunit.framework.dll이 여러 개라 어느 것을 쓸지 정할 수 없습니다. 쓰지 않는 패키지 캐시 폴더를 정리하세요." \
        "$(printf '%s' "$nunit_found" | tr '\n' ' ')"
    fi
    nunit_dll="$nunit_found"
    say "nunit: $(to_native "$nunit_dll")"
    ;;
esac

# ---------------------------------------------------------------------------
# 컴파일 전에 하는 확인
# ---------------------------------------------------------------------------

# 프로젝트 폴더와 위의 어셈블리 목록이 어긋나면 어떤 프로젝트는 영영 컴파일되지 않는다.
for project_file in "$tool_dir"/*/*.csproj; do
  [[ -f "$project_file" ]] || continue
  project_name="$(basename "$project_file" .csproj)"
  case " $editor_assemblies " in
    *" $project_name "*) ;;
    *) die "tools/compile-check/$project_name 프로젝트가 scripts/run-compile-check.sh의 어셈블리 목록에 없습니다." ;;
  esac
done
for assembly in $editor_assemblies; do
  if [[ ! -f "$tool_dir/$assembly/$assembly.csproj" ]]; then
    die "어셈블리 목록의 $assembly 에 해당하는 프로젝트 파일이 없습니다: tools/compile-check/$assembly/$assembly.csproj"
  fi
done

expected_sources="$(cd "$source_root" && find Assets -type f -name '*.cs' | LC_ALL=C sort)"
if [[ -z "$expected_sources" ]]; then
  die "Assets 아래에 .cs 파일이 하나도 없습니다: $(to_native "$source_root")/Assets"
fi

# 소스가 쓰는 전처리 기호. 문자열이나 주석 안의 '#if'는 줄 앞부분 조건으로 걸러 낸다.
used_symbols="$(
  cd "$source_root" && find Assets -type f -name '*.cs' -print0 \
    | LC_ALL=C xargs -0 grep -hE '^[^[:alnum:]"/]*#[[:space:]]*(if|elif)[[:space:](!]' 2>/dev/null \
    | LC_ALL=C sed -e 's#//.*$##' -e 's/^[^#]*#[[:space:]]*[a-z]*//' \
    | LC_ALL=C grep -oE '[A-Za-z_][A-Za-z0-9_]*' \
    | LC_ALL=C sort -u || true
)"
unknown_symbols=""
for symbol in $used_symbols; do
  case "$symbol" in true|false) continue ;; esac
  case " $known_symbols " in
    *" $symbol "*) ;;
    *) unknown_symbols="$unknown_symbols $symbol" ;;
  esac
done
if [[ -n "$unknown_symbols" ]]; then
  die "이 확인이 정의 여부를 정해 두지 않은 전처리 기호를 소스가 씁니다:$unknown_symbols" \
    "그 기호가 에디터·플레이어에서 각각 정의되는지 정해 tools/compile-check/Directory.Build.targets의 DefineConstants에 반영하고," \
    "scripts/run-compile-check.sh의 known_symbols에 더하세요. 그대로 두면 그 구간을 Unity와 다르게 컴파일합니다."
fi

# ---------------------------------------------------------------------------
# 컴파일
# ---------------------------------------------------------------------------

# 이전 실행의 산출물이 결과에 섞이지 않도록 매번 지우고 처음부터 컴파일한다.
if ! rm -rf "$tool_dir/obj" "$tool_dir/bin"; then
  die "이전 산출물을 지우지 못했습니다: $(to_native "$tool_dir")/obj, bin" \
    "다른 프로세스가 잡고 있는지 확인하세요(dotnet build-server shutdown)."
fi
mkdir -p "$out_dir"

export MSBUILDTERMINALLOGGER=off
: "${DOTNET_CLI_UI_LANGUAGE:=en}"
export DOTNET_CLI_UI_LANGUAGE
export DOTNET_NOLOGO=1

msbuild_props=(
  "-p:SrpSourceRoot=$(to_native "$source_root")"
  "-p:SrpUnityData=$(to_native "$unity_data")"
  "-p:SrpPlayerManaged=$(to_native "$player_managed")"
  "-p:SrpLibrary=$(to_native "$library")"
)
if [[ -n "$nunit_dll" ]]; then
  msbuild_props+=("-p:SrpNUnitDll=$(to_native "$nunit_dll")")
fi

# 진단 줄에서 떼어 낼 소스 루트 접두사(dotnet이 출력하는 꼴).
if command -v cygpath >/dev/null 2>&1; then
  source_prefix="$(cygpath -w "$source_root")\\"
else
  source_prefix="$source_root/"
fi

# stdin의 진단 줄을 "Assets/.../파일.cs(줄,칸): error CS....: 메시지" 꼴로 줄여 출력한다.
print_diagnostics() {
  local line path rest
  while IFS= read -r line; do
    line="${line//"$source_prefix"/}"
    path="${line%%(*}"
    rest="${line#"$path"}"
    printf '[srpg]       %s%s\n' "${path//\\//}" "$rest"
  done
}

# 그 어셈블리의 csproj가 ProjectReference로 적은 어셈블리 이름들.
deps_of() {
  grep -oE 'ProjectReference Include="[^"]+"' "$tool_dir/$1/$1.csproj" \
    | sed -E 's#.*/([^/"]+)\.csproj"#\1#' || true
}

max_shown_errors=20  # 어셈블리 하나에서 화면에 보일 오류 줄 수. 문법 오류 하나가 수십 줄로 번지기 때문이다.
not_ok=" "           # 통과하지 못한 "변형:어셈블리" 목록
total_errors=0
failed_count=0
unchecked_count=0
passed=""           # 통과한 어셈블리: "변형 어셈블리 파일수 경고수" 줄

check_assembly() {
  local variant="$1" assembly="$2"
  local project="$tool_dir/$assembly/$assembly.csproj"
  local report="$out_dir/$variant/$assembly.report.txt"
  local log="$out_dir/$variant/$assembly.log"
  local console_log="$out_dir/$variant/$assembly.console.log"
  local dependency

  for dependency in $(deps_of "$assembly"); do
    case "$not_ok" in
      *" $variant:$dependency "*)
        say "  $assembly: 컴파일하지 않음 — 참조하는 어셈블리($dependency)가 통과하지 못했습니다."
        not_ok="$not_ok$variant:$assembly "
        unchecked_count=$((unchecked_count + 1))
        return 0
        ;;
    esac
  done

  mkdir -p "$out_dir/$variant"
  local build_status=0
  # 로그는 파일 로거로 UTF-8로 받는다. 콘솔 출력은 Windows에서 코드 페이지에 따라 깨질 수 있다.
  dotnet build "$(to_native "$project")" --nologo --no-dependencies -v:quiet \
    "-p:SrpVariant=$variant" "${msbuild_props[@]}" \
    "-flp:LogFile=$(to_native "$log");Verbosity=minimal;Encoding=UTF-8" \
    >"$console_log" 2>&1 || build_status=$?

  local diagnostics_source="$log"
  [[ -f "$log" ]] || diagnostics_source="$console_log"

  local errors warnings error_count warning_count
  # 같은 진단이 로그에 두 번(본문과 요약) 나온다. 컴파일러가 낸 순서를 지키며 중복만 뺀다(첫 오류가 보통 원인이다).
  errors="$(tr -d '\r' <"$diagnostics_source" | grep -E '(^|[^A-Za-z])error [A-Za-z]+[0-9]+ ?:' \
    | sed -E 's/[[:space:]]*\[[^]]*\][[:space:]]*$//' | awk '!seen[$0]++' || true)"
  warnings="$(tr -d '\r' <"$diagnostics_source" | grep -E '(^|[^A-Za-z])warning [A-Za-z]+[0-9]+ ?:' \
    | sed -E 's/[[:space:]]*\[[^]]*\][[:space:]]*$//' | awk '!seen[$0]++' || true)"
  error_count="$(count_lines "$errors")"
  warning_count="$(count_lines "$warnings")"

  # 보고 파일은 컴파일 직전에 생긴다. 없으면 그 앞의 확인(참조 DLL, asmdef 대조 등)에서 멈춘 것이다.
  local source_count=0 reference_count="?" output_path="" files_text="컴파일 전 확인에서 멈춤"
  if [[ -f "$report" ]]; then
    source_count="$(tr -d '\r' <"$report" | grep -c '^source=' || true)"
    reference_count="$(tr -d '\r' <"$report" | sed -n 's/^references=//p' | awk 'NR == 1')"
    output_path="$(tr -d '\r' <"$report" | sed -n 's/^output=//p' | awk 'NR == 1')"
    files_text="파일 ${source_count}개"
  fi

  # 통과 조건: 빌드 성공 + 소스가 1개 이상 + 컴파일러가 이번 실행에서 DLL을 실제로 만들었다.
  local verdict="ok" reason=""
  if [[ "$build_status" -ne 0 || "$error_count" -gt 0 ]]; then
    verdict="fail"
  elif [[ ! -f "$report" ]]; then
    verdict="fail"; reason="빌드는 끝났는데 컴파일 단계의 보고 파일이 없습니다. 컴파일하지 않은 것으로 봅니다."
  elif [[ "$source_count" -eq 0 ]]; then
    verdict="fail"; reason="컴파일한 소스가 0개입니다."
  elif [[ -z "$output_path" || ! -f "$output_path" ]]; then
    verdict="fail"; reason="컴파일 산출물(DLL)이 만들어지지 않았습니다. 컴파일하지 않은 것으로 봅니다."
  fi

  local warning_summary=""
  if [[ "$warning_count" -gt 0 ]]; then
    warning_summary=" ($(printf '%s\n' "$warnings" | grep -oE 'warning [A-Za-z]+[0-9]+' | sed 's/^warning //' \
      | LC_ALL=C sort | uniq -c | awk '{printf "%s%s ×%s", (NR > 1 ? ", " : ""), $2, $1}'))"
  fi

  if [[ "$verdict" == "ok" ]]; then
    say "  $assembly: ${files_text}, 참조 DLL ${reference_count}개 → 오류 0, 경고 ${warning_count}${warning_summary}"
    passed="$passed$variant $assembly $source_count $warning_count"$'\n'
  else
    not_ok="$not_ok$variant:$assembly "
    failed_count=$((failed_count + 1))
    total_errors=$((total_errors + error_count))
    say "  $assembly: ${files_text} → 실패, 오류 ${error_count}개"
    if [[ -n "$reason" ]]; then
      say "      $reason"
    fi
    if [[ -n "$errors" ]]; then
      printf '%s\n' "$errors" | awk -v limit="$max_shown_errors" 'NR <= limit' | print_diagnostics
      if [[ "$error_count" -gt "$max_shown_errors" ]]; then
        say "      … 외 $((error_count - max_shown_errors))개. 전체는 로그에 있습니다."
      fi
    elif [[ -z "$reason" ]]; then
      say "      빌드가 실패했는데 오류 줄을 찾지 못했습니다(dotnet 종료 코드 $build_status)."
    fi
    say "      로그: $(to_native "$diagnostics_source")"
  fi

  if [[ "$show_warnings" -eq 1 && -n "$warnings" ]]; then
    printf '%s\n' "$warnings" | print_diagnostics
  fi
}

for variant in $variants; do
  if [[ "$variant" == "editor" ]]; then
    say "editor 변형 — 에디터에서 컴파일되는 구성 (UNITY_EDITOR, UNITY_INCLUDE_TESTS 정의, UnityEditor 참조)"
    assemblies="$editor_assemblies"
  else
    say "player 변형 — 플레이어 빌드 구성 (두 기호 없음, UnityEditor·테스트 어셈블리 없음)"
    assemblies="$player_assemblies"
  fi
  for assembly in $assemblies; do
    check_assembly "$variant" "$assembly"
  done
done

# ---------------------------------------------------------------------------
# 소스 대조: Assets의 .cs가 빠짐없이, 한 번씩만 컴파일됐는가 (editor 변형 기준)
# ---------------------------------------------------------------------------
coverage_failed=0
case " $variants " in
  *" editor "*)
    reports_complete=1
    for assembly in $editor_assemblies; do
      [[ -f "$out_dir/editor/$assembly.report.txt" ]] || reports_complete=0
    done
    if [[ "$reports_complete" -eq 1 ]]; then
      compiled_sources="$(
        for assembly in $editor_assemblies; do
          tr -d '\r' <"$out_dir/editor/$assembly.report.txt" | sed -n 's/^source=//p'
        done | tr '\\' '/' | LC_ALL=C sort
      )"
      duplicated="$(printf '%s\n' "$compiled_sources" | uniq -d)"
      not_compiled="$(comm -23 <(printf '%s\n' "$expected_sources") <(printf '%s\n' "$compiled_sources" | uniq))"
      unexpected="$(comm -13 <(printf '%s\n' "$expected_sources") <(printf '%s\n' "$compiled_sources" | uniq))"
      if [[ -n "$not_compiled" ]]; then
        coverage_failed=1
        say "소스 대조 실패: 어느 프로젝트도 컴파일하지 않은 .cs가 있습니다. 새 asmdef 폴더나 Plugins 같은 firstpass 폴더라면"
        say "  tools/compile-check에 그 어셈블리의 프로젝트를 더하고 이 스크립트의 어셈블리 목록에 넣으세요:"
        printf '%s\n' "$not_compiled" | while IFS= read -r line; do say "    $line"; done
      fi
      if [[ -n "$duplicated" ]]; then
        coverage_failed=1
        say "소스 대조 실패: 두 프로젝트가 같은 .cs를 컴파일했습니다(asmdef 폴더가 겹쳤는지 확인):"
        printf '%s\n' "$duplicated" | while IFS= read -r line; do say "    $line"; done
      fi
      if [[ -n "$unexpected" ]]; then
        coverage_failed=1
        say "소스 대조 실패: Assets의 .cs가 아닌 파일이 컴파일됐습니다:"
        printf '%s\n' "$unexpected" | while IFS= read -r line; do say "    $line"; done
      fi
      if [[ "$coverage_failed" -eq 0 ]]; then
        say "소스 대조: Assets의 .cs $(count_lines "$expected_sources")개가 editor 변형에서 빠짐없이 한 번씩 컴파일됨"
      fi
    else
      say "소스 대조: 컴파일 단계까지 가지 못한 어셈블리가 있어 하지 못했습니다."
    fi
    ;;
  *)
    say "소스 대조: editor 변형을 돌릴 때만 합니다(이번에는 하지 않음)."
    ;;
esac

# ---------------------------------------------------------------------------
# 결과
# ---------------------------------------------------------------------------
passed_text=""
total_warnings=0
incomplete=0
for variant in $variants; do
  if [[ "$variant" == "editor" ]]; then
    expected_assemblies="$editor_assemblies"
  else
    expected_assemblies="$player_assemblies"
  fi
  # shellcheck disable=SC2086  # 목록을 낱말로 나누려고 일부러 따옴표를 뺀다
  expected_count="$(printf '%s\n' $expected_assemblies | wc -l | tr -d '[:space:]')"
  variant_line="$(printf '%s' "$passed" | awk -v v="$variant" '
    $1 == v { assemblies++; files += $3; warnings += $4 }
    END { printf "%d %d %d", assemblies, files, warnings }')"
  read -r variant_assemblies variant_files variant_warnings <<EOF
$variant_line
EOF
  # 실패를 세는 것만으로 통과를 정하지 않는다. 목록의 어셈블리가 모두 통과로 기록돼야 통과다.
  if [[ "$variant_assemblies" -ne "$expected_count" ]]; then
    incomplete=1
  fi
  total_warnings=$((total_warnings + variant_warnings))
  passed_text="${passed_text:+$passed_text, }$variant 어셈블리 ${variant_assemblies}개(파일 ${variant_files}개, 경고 ${variant_warnings})"
done

if [[ "$failed_count" -gt 0 || "$unchecked_count" -gt 0 || "$coverage_failed" -ne 0 || "$incomplete" -ne 0 ]]; then
  detail=""
  if [[ "$failed_count" -gt 0 ]]; then
    detail="실패한 어셈블리 ${failed_count}개(오류 ${total_errors}개)"
  fi
  if [[ "$unchecked_count" -gt 0 ]]; then
    detail="${detail:+$detail, }컴파일하지 못한 어셈블리 ${unchecked_count}개"
  fi
  if [[ "$coverage_failed" -ne 0 ]]; then
    detail="${detail:+$detail, }소스 대조 불일치"
  fi
  if [[ -z "$detail" ]]; then
    detail="통과로 기록된 어셈블리 수가 어셈블리 목록과 다릅니다"
  fi
  say "결과: 실패 — $detail. 통과한 것: $passed_text (종료 코드 $exit_fail)"
  if [[ -n "$caveat" ]]; then
    say "  주의: $caveat"
  fi
  exit "$exit_fail"
fi

say "결과: 통과 — 오류 0. $passed_text"
if [[ -n "$caveat" ]]; then
  say "  주의: $caveat"
fi
if [[ "$total_warnings" -gt 0 && "$show_warnings" -eq 0 ]]; then
  say "  경고 목록은 --warnings로 봅니다. 로그: $(to_native "$out_dir")/<변형>/<어셈블리>.log"
fi
