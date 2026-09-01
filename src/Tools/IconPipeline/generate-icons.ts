#!/usr/bin/env -S deno run --allow-env --allow-net --allow-read --allow-write
//
// Batch icon generation through the text-to-image nano-banana model on fal.ai.
// Consumes the corpus written by build-icon-corpus.ts and drops one <id>.png per entry.
//
// A run never spends more than --limit requests, never re-spends on files that already
// exist under --resume, never waits on one entry past its time budget, and never aborts the
// whole batch because one entry failed.
//
// Usage:
//   deno run --allow-env --allow-net --allow-read --allow-write src/Tools/IconPipeline/generate-icons.ts \
//     --corpus ./icon-corpus.json --output ./raw --limit 6 \
//     [--only Ability_Armageddon,weapon] [--resume] [--concurrency 1] [--delay 1500] [--dry-run]

// ============================================================================
// Constants
// ============================================================================

const MODEL_ENDPOINT = "https://queue.fal.run/fal-ai/nano-banana";
const OUTPUT_FORMAT = "png";
const POLL_INTERVAL_MS = 1500;
const POLL_ATTEMPTS = 120;
const DEFAULT_DELAY_MS = 1500;
const DEFAULT_CONCURRENCY = 1;
const MAX_CONCURRENCY = 3;
const LOG_FILE_NAME = "generation-log.jsonl";

/**
 * Ceilings per network call. Without them a socket that stops answering hangs the whole batch:
 * the poll loop is bounded, the calls it makes were not. Sized against observed round trips
 * (submit and status answer in well under a second, a finished icon downloads in a few).
 */
const SUBMIT_TIMEOUT_MS = 30_000;
const STATUS_TIMEOUT_MS = 15_000;
const RESULT_TIMEOUT_MS = 30_000;
const DOWNLOAD_TIMEOUT_MS = 60_000;

/**
 * Ceiling on one entry end to end, polling included. Whatever is still unfinished at this point is
 * reported as a failed entry so the run moves on. A whole icon normally takes about ten seconds.
 */
const ENTRY_BUDGET_MS = 300_000;

// ============================================================================
// Types
// ============================================================================

interface CorpusEntry {
  id: string;
  family: string;
  prompt: string;
}

type Outcome = "generated" | "skipped" | "failed";

interface Result {
  id: string;
  family: string;
  outcome: Outcome;
  reason?: string;
  bytes?: number;
}

interface Options {
  corpus: string;
  output: string;
  limit: number;
  only: string[];
  resume: boolean;
  concurrency: number;
  delay: number;
  dryRun: boolean;
  log: string;
}

// ============================================================================
// CLI
// ============================================================================

function arg(name: string, fallback?: string): string {
  const i = Deno.args.indexOf(`--${name}`);
  if (i >= 0 && i + 1 < Deno.args.length) return Deno.args[i + 1];
  if (fallback !== undefined) return fallback;
  console.error(`Missing --${name}`);
  Deno.exit(1);
}

function positiveInt(name: string, fallback?: string): number {
  const value = Number(arg(name, fallback));
  if (!Number.isInteger(value) || value < 0) {
    console.error(`--${name} must be a non-negative integer`);
    Deno.exit(1);
  }
  return value;
}

function readOptions(): Options {
  const output = arg("output").replace(/[\\/]+$/, "");
  return {
    corpus: arg("corpus"),
    output,
    limit: positiveInt("limit"),
    only: arg("only", "").split(",").map((s) => s.trim()).filter((s) => s.length > 0),
    resume: Deno.args.includes("--resume"),
    concurrency: Math.min(positiveInt("concurrency", String(DEFAULT_CONCURRENCY)) || 1, MAX_CONCURRENCY),
    delay: positiveInt("delay", String(DEFAULT_DELAY_MS)),
    dryRun: Deno.args.includes("--dry-run"),
    log: arg("log", `${output}/${LOG_FILE_NAME}`),
  };
}

// ============================================================================
// Generation
// ============================================================================

function describe(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

function seconds(ms: number): string {
  return `${(ms / 1000).toFixed(0)}s`;
}

/** Wall-clock ceiling for one entry. Every call it makes is bounded by what is left of it. */
interface Deadline {
  expiresAt: number;
}

function deadlineIn(budgetMs: number): Deadline {
  return { expiresAt: Date.now() + budgetMs };
}

/**
 * Fetch bounded by two ceilings at once: this call's own timeout and the remainder of the entry's
 * budget. A socket that never answers costs one timeout instead of the run, and a sequence of slow
 * but answered calls still cannot outlive the entry.
 */
async function fetchWithin(
  label: string,
  url: string,
  init: RequestInit,
  timeoutMs: number,
  deadline: Deadline,
): Promise<Response> {
  const remaining = deadline.expiresAt - Date.now();
  if (remaining <= 0) throw new Error(`${label}: entry budget of ${seconds(ENTRY_BUDGET_MS)} exhausted`);
  const limit = Math.min(timeoutMs, remaining);
  try {
    return await fetch(url, { ...init, signal: AbortSignal.timeout(limit) });
  } catch (error) {
    if (error instanceof DOMException && error.name === "TimeoutError") {
      throw new Error(`${label} timed out after ${seconds(limit)}`);
    }
    throw error;
  }
}

async function submit(
  prompt: string,
  key: string,
  deadline: Deadline,
): Promise<{ status_url: string; response_url: string }> {
  const response = await fetchWithin("submit", MODEL_ENDPOINT, {
    method: "POST",
    headers: { Authorization: `Key ${key}`, "Content-Type": "application/json" },
    body: JSON.stringify({ prompt, num_images: 1, output_format: OUTPUT_FORMAT }),
  }, SUBMIT_TIMEOUT_MS, deadline);
  if (!response.ok) throw new Error(`submit ${response.status}: ${(await response.text()).slice(0, 300)}`);
  return await response.json();
}

async function awaitImageUrl(
  statusUrl: string,
  responseUrl: string,
  key: string,
  deadline: Deadline,
): Promise<string> {
  const headers = { Authorization: `Key ${key}` };
  for (let attempt = 0; attempt < POLL_ATTEMPTS; attempt++) {
    await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS));
    const status = await (await fetchWithin("status", statusUrl, { headers }, STATUS_TIMEOUT_MS, deadline)).json();
    if (status.status === "FAILED" || status.status === "ERROR") {
      throw new Error(`generation failed: ${JSON.stringify(status).slice(0, 300)}`);
    }
    if (status.status !== "COMPLETED") continue;
    const result = await (await fetchWithin("result", responseUrl, { headers }, RESULT_TIMEOUT_MS, deadline)).json();
    const url = result?.images?.[0]?.url;
    if (typeof url !== "string") throw new Error(`completed with no image: ${JSON.stringify(result).slice(0, 300)}`);
    return url;
  }
  throw new Error(`polling gave up after ${seconds(POLL_ATTEMPTS * POLL_INTERVAL_MS)}`);
}

async function generateOne(entry: CorpusEntry, target: string, key: string): Promise<number> {
  const deadline = deadlineIn(ENTRY_BUDGET_MS);
  const { status_url, response_url } = await submit(entry.prompt, key, deadline);
  const imageUrl = await awaitImageUrl(status_url, response_url, key, deadline);
  const download = await fetchWithin("download", imageUrl, {}, DOWNLOAD_TIMEOUT_MS, deadline);
  if (!download.ok) throw new Error(`download ${download.status}`);
  const bytes = new Uint8Array(await download.arrayBuffer());
  await Deno.writeFile(target, bytes);
  return bytes.length;
}

// ============================================================================
// Selection
// ============================================================================

function matchesFilter(entry: CorpusEntry, only: string[]): boolean {
  return only.length === 0 || only.includes(entry.id) || only.includes(entry.family);
}

async function exists(path: string): Promise<boolean> {
  try {
    await Deno.stat(path);
    return true;
  } catch {
    return false;
  }
}

function targetPath(options: Options, entry: CorpusEntry): string {
  return `${options.output}/${entry.id}.${OUTPUT_FORMAT}`;
}

/** Splits the filtered corpus into what this run will request and what it will not touch. */
async function plan(entries: CorpusEntry[], options: Options): Promise<{ queue: CorpusEntry[]; skipped: Result[] }> {
  const queue: CorpusEntry[] = [];
  const skipped: Result[] = [];
  for (const entry of entries) {
    if (!matchesFilter(entry, options.only)) continue;
    if (options.resume && await exists(targetPath(options, entry))) {
      skipped.push({ id: entry.id, family: entry.family, outcome: "skipped", reason: "already in output" });
      continue;
    }
    if (queue.length >= options.limit) {
      skipped.push({ id: entry.id, family: entry.family, outcome: "skipped", reason: "over --limit" });
      continue;
    }
    queue.push(entry);
  }
  return { queue, skipped };
}

// ============================================================================
// Run
// ============================================================================

async function run(): Promise<void> {
  const options = readOptions();
  const key = Deno.env.get("FAL_KEY");
  if (!key && !options.dryRun) {
    console.error("FAL_KEY is not set");
    Deno.exit(1);
  }

  let corpus: CorpusEntry[];
  try {
    corpus = JSON.parse(await Deno.readTextFile(options.corpus));
  } catch (error) {
    console.error(`corpus not readable: ${describe(error)}`);
    Deno.exit(1);
  }
  if (!Array.isArray(corpus)) {
    console.error("corpus must be an array of { id, family, prompt }");
    Deno.exit(1);
  }

  await Deno.mkdir(options.output, { recursive: true });
  const { queue, skipped } = await plan(corpus, options);
  const results: Result[] = [...skipped];

  console.log(`corpus ${corpus.length} | queued ${queue.length} | skipped ${skipped.length} | limit ${options.limit}`);
  if (options.dryRun) {
    for (const entry of queue) console.log(`  would request ${entry.id} [${entry.family}]`);
    report(results, queue.length);
    return;
  }

  const logLines: string[] = [];
  let cursor = 0;
  const worker = async (): Promise<void> => {
    while (cursor < queue.length) {
      const entry = queue[cursor++];
      const started = Date.now();
      let result: Result;
      try {
        const bytes = await generateOne(entry, targetPath(options, entry), key!);
        result = { id: entry.id, family: entry.family, outcome: "generated", bytes };
        console.log(`  ok   ${entry.id} (${(bytes / 1024).toFixed(0)} KB, ${((Date.now() - started) / 1000).toFixed(1)}s)`);
      } catch (error) {
        result = { id: entry.id, family: entry.family, outcome: "failed", reason: describe(error) };
        console.error(`  FAIL ${entry.id}: ${result.reason}`);
      }
      results.push(result);
      logLines.push(JSON.stringify({ ...result, at: new Date().toISOString() }));
      if (options.delay > 0 && cursor < queue.length) {
        await new Promise((resolve) => setTimeout(resolve, options.delay));
      }
    }
  };

  await Promise.all(Array.from({ length: Math.max(1, options.concurrency) }, worker));
  if (logLines.length > 0) await Deno.writeTextFile(options.log, `${logLines.join("\n")}\n`, { append: true });
  report(results, queue.length);
}

function report(results: Result[], requests: number): void {
  const of = (outcome: Outcome) => results.filter((r) => r.outcome === outcome);
  const failed = of("failed");
  console.log("");
  console.log(`requests spent: ${requests}`);
  console.log(`generated: ${of("generated").length}`);
  console.log(`skipped:   ${of("skipped").length} (${countReasons(of("skipped"))})`);
  console.log(`failed:    ${failed.length}`);
  for (const failure of failed) console.log(`  ${failure.id}: ${failure.reason}`);
  if (failed.length > 0) Deno.exit(2);
}

function countReasons(results: Result[]): string {
  const counts = new Map<string, number>();
  for (const result of results) {
    const reason = result.reason ?? "unspecified";
    counts.set(reason, (counts.get(reason) ?? 0) + 1);
  }
  return [...counts].map(([reason, count]) => `${count} ${reason}`).join(", ") || "none";
}

if (import.meta.main) await run();
