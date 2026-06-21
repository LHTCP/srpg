#!/usr/bin/env node

const repository = process.env.GITHUB_REPOSITORY;
const token = process.env.GITHUB_TOKEN;
const cachePrefix = process.env.CACHE_PREFIX;
const keepCount = Number.parseInt(process.env.KEEP_COUNT ?? "1", 10);
const dryRun = (process.env.DRY_RUN ?? "true").toLowerCase() !== "false";

if (!repository) {
  throw new Error("GITHUB_REPOSITORY 환경변수가 필요합니다.");
}

if (!token) {
  throw new Error("GITHUB_TOKEN 환경변수가 필요합니다.");
}

if (!cachePrefix) {
  throw new Error("CACHE_PREFIX 환경변수가 필요합니다.");
}

if (!Number.isInteger(keepCount) || keepCount < 1) {
  throw new Error("KEEP_COUNT는 1 이상의 정수여야 합니다.");
}

const apiBase = `https://api.github.com/repos/${repository}/actions/caches`;

async function request(url, options = {}) {
  const response = await fetch(url, {
    ...options,
    headers: {
      Accept: "application/vnd.github+json",
      Authorization: `Bearer ${token}`,
      "X-GitHub-Api-Version": "2022-11-28",
      ...(options.headers ?? {}),
    },
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`GitHub API 요청 실패: ${response.status} ${response.statusText}\n${body}`);
  }

  return response;
}

function nextPageUrl(linkHeader) {
  if (!linkHeader) {
    return null;
  }

  const next = linkHeader
    .split(",")
    .map((part) => part.trim())
    .find((part) => part.endsWith('rel="next"'));

  if (!next) {
    return null;
  }

  const match = next.match(/^<(.+)>/);
  return match ? match[1] : null;
}

async function listCaches() {
  const caches = [];
  let url = `${apiBase}?per_page=100`;

  while (url) {
    const response = await request(url);
    const page = await response.json();
    caches.push(...(page.actions_caches ?? []));
    url = nextPageUrl(response.headers.get("link"));
  }

  return caches;
}

function cacheDate(cache) {
  // last_accessed_at은 cache hit 때 갱신되므로 "실제로 최근 쓰인 cache" 판단에 더 가깝다.
  // 값이 없으면 created_at으로 후퇴해 정렬 안정성을 유지한다.
  return new Date(cache.last_accessed_at ?? cache.created_at ?? 0).getTime();
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

async function deleteCache(cache) {
  await request(`${apiBase}/${cache.id}`, { method: "DELETE" });
}

const allCaches = await listCaches();
const matchingCaches = allCaches
  .filter((cache) => cache.key?.startsWith(cachePrefix))
  .sort((a, b) => cacheDate(b) - cacheDate(a));

const keptCaches = matchingCaches.slice(0, keepCount);
const deleteTargets = matchingCaches.slice(keepCount);
const totalTargetBytes = deleteTargets.reduce((sum, cache) => sum + (cache.size_in_bytes ?? 0), 0);

console.log(`cache prefix: ${cachePrefix}`);
console.log(`keep count: ${keepCount}`);
console.log(`dry run: ${dryRun}`);
console.log(`matched caches: ${matchingCaches.length}`);
console.log(`delete targets: ${deleteTargets.length}`);
console.log(`target size: ${formatBytes(totalTargetBytes)}`);

if (keptCaches.length > 0) {
  console.log("\nkept caches:");
  for (const cache of keptCaches) {
    console.log(`- id=${cache.id} size=${formatBytes(cache.size_in_bytes)} key=${cache.key}`);
  }
}

if (deleteTargets.length > 0) {
  console.log("\ndelete targets:");
  for (const cache of deleteTargets) {
    console.log(`- id=${cache.id} size=${formatBytes(cache.size_in_bytes)} key=${cache.key}`);
  }
}

for (const cache of deleteTargets) {
  if (dryRun) {
    continue;
  }

  await deleteCache(cache);
  console.log(`deleted cache id=${cache.id}`);
}

const summaryPath = process.env.GITHUB_STEP_SUMMARY;
if (summaryPath) {
  const fs = await import("node:fs/promises");
  const lines = [
    "## Actions cache 정리",
    "",
    `- prefix: \`${cachePrefix}\``,
    `- keep count: \`${keepCount}\``,
    `- dry run: \`${dryRun}\``,
    `- matched caches: \`${matchingCaches.length}\``,
    `- delete targets: \`${deleteTargets.length}\``,
    `- target size: \`${formatBytes(totalTargetBytes)}\``,
    "",
  ];

  if (deleteTargets.length > 0) {
    lines.push("| id | size | key |", "| -- | ---- | --- |");
    for (const cache of deleteTargets) {
      lines.push(`| ${cache.id} | ${formatBytes(cache.size_in_bytes)} | \`${cache.key}\` |`);
    }
    lines.push("");
  }

  await fs.appendFile(summaryPath, `${lines.join("\n")}\n`, "utf8");
}
