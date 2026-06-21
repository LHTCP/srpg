#!/usr/bin/env node

const repository = process.env.GITHUB_REPOSITORY;
const token = process.env.GITHUB_TOKEN;
const limitBytes = Number.parseInt(process.env.CACHE_STORAGE_LIMIT_BYTES ?? `${10 * 1024 ** 3}`, 10);
const minFreeBytes = Number.parseInt(process.env.MIN_FREE_CACHE_BYTES ?? `${2 * 1024 ** 3}`, 10);

if (!repository) {
  throw new Error("GITHUB_REPOSITORY 환경변수가 필요합니다.");
}

if (!token) {
  throw new Error("GITHUB_TOKEN 환경변수가 필요합니다.");
}

if (!Number.isInteger(limitBytes) || limitBytes < 1) {
  throw new Error("CACHE_STORAGE_LIMIT_BYTES는 1 이상의 정수여야 합니다.");
}

if (!Number.isInteger(minFreeBytes) || minFreeBytes < 0) {
  throw new Error("MIN_FREE_CACHE_BYTES는 0 이상의 정수여야 합니다.");
}

function formatBytes(bytes) {
  if (!Number.isFinite(bytes)) {
    return "unknown";
  }

  const units = ["B", "KiB", "MiB", "GiB"];
  let value = bytes;
  let unitIndex = 0;
  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  return `${value.toFixed(unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
}

const response = await fetch(`https://api.github.com/repos/${repository}/actions/cache/usage`, {
  headers: {
    Accept: "application/vnd.github+json",
    Authorization: `Bearer ${token}`,
    "X-GitHub-Api-Version": "2022-11-28",
  },
});

if (!response.ok) {
  const body = await response.text();
  throw new Error(`GitHub Actions cache usage 조회 실패: ${response.status} ${response.statusText}\n${body}`);
}

const usage = await response.json();
const usedBytes = usage.active_caches_size_in_bytes ?? 0;
const freeBytes = Math.max(limitBytes - usedBytes, 0);
const isSafe = freeBytes >= minFreeBytes;

console.log(`cache limit: ${formatBytes(limitBytes)}`);
console.log(`cache used: ${formatBytes(usedBytes)}`);
console.log(`cache free: ${formatBytes(freeBytes)}`);
console.log(`minimum required free cache: ${formatBytes(minFreeBytes)}`);
console.log(`active cache count: ${usage.active_caches_count ?? "unknown"}`);

const summaryPath = process.env.GITHUB_STEP_SUMMARY;
if (summaryPath) {
  const fs = await import("node:fs/promises");
  await fs.appendFile(
    summaryPath,
    [
      "## Actions cache 예산 가드",
      "",
      `- cache limit: \`${formatBytes(limitBytes)}\``,
      `- cache used: \`${formatBytes(usedBytes)}\``,
      `- cache free: \`${formatBytes(freeBytes)}\``,
      `- minimum free: \`${formatBytes(minFreeBytes)}\``,
      `- active cache count: \`${usage.active_caches_count ?? "unknown"}\``,
      "",
    ].join("\n"),
    "utf8",
  );
}

if (!isSafe) {
  throw new Error(
    `Actions cache 여유 공간이 ${formatBytes(minFreeBytes)} 미만입니다. ` +
      "`Actions 저장소 정리` workflow를 dry_run=true로 먼저 확인한 뒤 필요하면 dry_run=false로 실행하세요.",
  );
}
