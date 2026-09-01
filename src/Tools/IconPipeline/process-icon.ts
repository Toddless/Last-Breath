#!/usr/bin/env -S deno run --allow-read --allow-write --allow-run

/**
 * Icon Processing CLI
 *
 * Post-process generated icons for game use: background removal, subject-weight
 * normalization, resizing, trimming, and color key transparency.
 *
 * Provenance: forked from the third-party godot-asset-generator skill's `process-sprite.ts`
 * (v1.0.0). It lives here rather than in the skill because the skill is not version-controlled
 * with the project and reinstalling it would drop these fixes:
 *   1. The ImageMagick command was assembled as strings and re-split on spaces, so --color-key
 *      handed magick a color wrapped in literal quote characters. Magick rejected it as an
 *      unrecognized color, exited 0 anyway, and the script reported success having done nothing.
 *      Arguments are now a real argv array.
 *   2. --remove-bg was nailed to white, so it could not lift the icon canon's warm cream
 *      background. It now takes an optional color, and --fuzz sets the match tolerance.
 *   3. Every failure was treated as "ImageMagick is not installed": the input was copied over
 *      the output and reported as a success. Only a missing binary takes that path now, and a
 *      magick warning that names a rejected argument is raised instead of swallowed.
 * Added for this pipeline: --fit, which normalizes how much of the frame the subject occupies.
 *
 * Usage:
 *   deno run --allow-read --allow-write --allow-run src/Tools/IconPipeline/process-icon.ts \
 *     --input ./raw.png --output ./icon.png \
 *     --remove-bg F6EFD9 --fuzz 12 --fit 0.85 --resize 256x256 --filter linear
 *
 * Permissions:
 *   --allow-read: Read input images
 *   --allow-write: Write processed images
 *   --allow-run:   Invoke ImageMagick
 */

// === Constants ===
const VERSION = "1.1.0";
const SCRIPT_NAME = "process-icon";

/** Background color --remove-bg assumes when the caller names none. */
const DEFAULT_BACKGROUND_COLOR = "white";
/** Tolerance both transparency operations use when --fuzz is absent. */
const DEFAULT_FUZZ = "10%";

// === Types ===
interface ProcessOptions {
  input: string;
  output: string;
  /** true = default background color, string = the background color to knock out. */
  removeBg?: boolean | string;
  resize?: string;
  filter?: "nearest" | "linear";
  trim?: boolean;
  padding?: number;
  colorKey?: string;
  /** Match tolerance for --remove-bg / --color-key, e.g. "12%" or "12". */
  fuzz?: string;
  /** Share of the frame's larger side the trimmed subject must occupy, 0..1. Implies trim. */
  fit?: number;
}

interface ProcessResult {
  success: boolean;
  input: string;
  output: string;
  originalSize?: { width: number; height: number };
  finalSize?: { width: number; height: number };
  operations: string[];
  error?: string;
}

// === PNG Utilities ===
// Basic PNG reading - extracts dimensions from header
function readPngDimensions(data: Uint8Array): { width: number; height: number } | null {
  // PNG signature check
  const signature = [137, 80, 78, 71, 13, 10, 26, 10];
  for (let i = 0; i < 8; i++) {
    if (data[i] !== signature[i]) {
      return null;
    }
  }

  // IHDR chunk starts at byte 8
  // Length (4 bytes) + Type "IHDR" (4 bytes) + Width (4 bytes) + Height (4 bytes)
  const width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
  const height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];

  return { width, height };
}

/** Accepts "12" or "12%" and hands ImageMagick the percentage form it expects. */
function normalizeFuzz(raw: string | undefined): string {
  if (raw === undefined || raw.trim().length === 0) return DEFAULT_FUZZ;
  const trimmed = raw.trim();
  return trimmed.endsWith("%") ? trimmed : `${trimmed}%`;
}

/** Shell-safe rendering of one argv entry, for the copy-paste command shown when magick is absent. */
function quoteForDisplay(argument: string): string {
  return /[\s"']/.test(argument) ? `"${argument.replace(/"/g, '\\"')}"` : argument;
}

// === Core Processing ===
export async function processIcon(options: ProcessOptions): Promise<ProcessResult> {
  const operations: string[] = [];

  try {
    // Read input file
    const inputData = await Deno.readFile(options.input);
    const dimensions = readPngDimensions(inputData);

    if (!dimensions) {
      throw new Error("Invalid PNG file or unable to read dimensions");
    }

    // ImageMagick argument list. Every element is one argv entry: arguments are never
    // re-split on spaces and colors are never wrapped in quote characters, both of which
    // silently corrupted the color the tool was asked to knock out.
    const magickArgs: string[] = [];
    const currentInput = options.input;
    const fuzz = normalizeFuzz(options.fuzz);

    if (options.colorKey) {
      magickArgs.push("-fuzz", fuzz, "-transparent", `#${options.colorKey}`);
      operations.push(`color-key: #${options.colorKey} (fuzz ${fuzz})`);
    }

    if (options.removeBg) {
      const color = typeof options.removeBg === "string" ? options.removeBg : DEFAULT_BACKGROUND_COLOR;
      magickArgs.push("-fuzz", fuzz, "-transparent", color);
      operations.push(`remove-bg: ${color} (fuzz ${fuzz})`);
    }

    if (options.trim || options.fit !== undefined) {
      magickArgs.push("-trim", "+repage");
      operations.push("trim");
    }

    if (options.fit !== undefined) {
      // Square the canvas around the trimmed subject so its larger side is exactly `fit` of it.
      // Every icon then carries the same visual weight whatever its aspect ratio.
      const side = `%[fx:round(max(w,h)/${options.fit})]`;
      magickArgs.push("-background", "none", "-gravity", "center", "-extent", `${side}x${side}`);
      operations.push(`fit: ${(options.fit * 100).toFixed(0)}% of frame`);
    }

    if (options.resize) {
      const [width, height] = options.resize.split("x").map(Number);
      const filter = options.filter === "linear" ? "Triangle" : "Point";
      magickArgs.push("-filter", filter, "-resize", `${width}x${height}!`);
      operations.push(`resize: ${options.resize} (${options.filter || "nearest"})`);
    }

    if (options.padding && options.padding > 0) {
      magickArgs.push("-bordercolor", "transparent", "-border", String(options.padding));
      operations.push(`padding: ${options.padding}px`);
    }

    // If no ImageMagick commands needed, just copy
    if (magickArgs.length === 0) {
      await Deno.copyFile(options.input, options.output);
      operations.push("copy (no processing)");

      return {
        success: true,
        input: options.input,
        output: options.output,
        originalSize: dimensions,
        finalSize: dimensions,
        operations,
      };
    }

    // Try to run ImageMagick
    const magickCmd = `magick "${currentInput}" ${magickArgs.map(quoteForDisplay).join(" ")} "${options.output}"`;

    try {
      const process = new Deno.Command("magick", {
        args: [currentInput, ...magickArgs, options.output],
        stdout: "piped",
        stderr: "piped",
      });

      const { code, stderr } = await process.output();
      const errorText = new TextDecoder().decode(stderr);

      if (code !== 0) {
        throw new Error(`ImageMagick failed: ${errorText}`);
      }
      // ImageMagick reports an unusable color or geometry as a warning and still exits 0.
      // Treating that as success is how a no-op used to pass for a finished sprite.
      if (/unrecognized|unable to parse|no such/i.test(errorText)) {
        throw new Error(`ImageMagick rejected an argument: ${errorText.trim()}`);
      }

      // Read output dimensions
      const outputData = await Deno.readFile(options.output);
      const outputDimensions = readPngDimensions(outputData);

      return {
        success: true,
        input: options.input,
        output: options.output,
        originalSize: dimensions,
        finalSize: outputDimensions || dimensions,
        operations,
      };
    } catch (magickError) {
      // Only a missing binary earns the copy-and-instructions fallback. A magick that ran and
      // refused the work is a real failure: copying the unprocessed input over the output and
      // calling it a success is what hid the broken color key.
      if (!(magickError instanceof Deno.errors.NotFound)) throw magickError;

      // ImageMagick not available - provide manual instructions
      console.warn("\nWarning: ImageMagick not found. Manual processing required.");
      console.warn("\nRun this command manually:");
      console.warn(`  ${magickCmd}`);
      console.warn("\nOr install ImageMagick:");
      console.warn("  - macOS: brew install imagemagick");
      console.warn("  - Ubuntu: sudo apt install imagemagick");
      console.warn("  - Windows: https://imagemagick.org/script/download.php");

      // Copy file as fallback
      await Deno.copyFile(options.input, options.output);
      operations.push("copy (ImageMagick unavailable)");

      return {
        success: true,
        input: options.input,
        output: options.output,
        originalSize: dimensions,
        finalSize: dimensions,
        operations,
        error: "ImageMagick not available - file copied without processing",
      };
    }
  } catch (error) {
    return {
      success: false,
      input: options.input,
      output: options.output,
      operations,
      error: error instanceof Error ? error.message : String(error),
    };
  }
}

// === Help Text ===
function printHelp(): void {
  console.log(`
${SCRIPT_NAME} v${VERSION} - Post-process sprites for game use

Usage:
  deno run --allow-read --allow-write --allow-run src/Tools/IconPipeline/process-icon.ts [options]

Required:
  --input <path>      Input image path
  --output <path>     Output image path

Processing Options:
  --remove-bg [color] Make the background transparent. Without an argument the color is
                      "${DEFAULT_BACKGROUND_COLOR}"; name one to knock out a different background
                      (e.g. --remove-bg F6EFD9 for the warm cream icon canon)
  --fuzz <n>          Match tolerance for --remove-bg / --color-key (default ${DEFAULT_FUZZ}).
                      A painted background needs headroom: 12% covers the icon canon
  --fit <fraction>    Normalize subject weight: trim to the opaque content, then square the
                      canvas so the subject's larger side is this share of it (e.g. 0.85).
                      Implies --trim. Apply before --resize for a uniform icon frame
  --resize <WxH>      Resize to exact dimensions (e.g., 64x64)
  --filter <type>     Resize filter: nearest (default) or linear
  --trim              Trim transparent/white borders
  --padding <n>       Add transparent padding (pixels)
  --color-key <hex>   Make specific color transparent (e.g., ff00ff)

Other:
  --json              Output result as JSON
  -h, --help          Show this help

Note:
  Advanced processing requires ImageMagick installed.
  Basic operations work without external dependencies.

Examples:
  # Remove background and resize for pixel art
  ./src/Tools/IconPipeline/process-icon.ts --input raw.png --output sprite.png \\
    --remove-bg --resize 64x64 --filter nearest

  # Icon canon: knock out the warm cream, normalize weight, land on 256x256
  ./src/Tools/IconPipeline/process-icon.ts --input raw.png --output icon.png \\
    --remove-bg F6EFD9 --fuzz 12 --fit 0.85 --resize 256x256 --filter linear

  # Trim whitespace and add padding
  ./src/Tools/IconPipeline/process-icon.ts --input raw.png --output sprite.png \\
    --trim --padding 2

  # Make magenta transparent (color key)
  ./src/Tools/IconPipeline/process-icon.ts --input raw.png --output sprite.png \\
    --color-key ff00ff
`);
}

// === Argument Parsing ===
/** Bare hex reads as a color name to ImageMagick; "#" makes it a color. Named colors pass through. */
function withHash(color: string): string {
  return /^[0-9a-fA-F]{3,8}$/.test(color) ? `#${color}` : color;
}

function parseArgs(args: string[]): (ProcessOptions & { outputJson?: boolean }) | null {
  const options: Partial<ProcessOptions & { outputJson?: boolean }> = {};

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];

    if (arg === "-h" || arg === "--help") {
      return null;
    } else if (arg === "--input" && args[i + 1]) {
      options.input = args[++i];
    } else if (arg === "--output" && args[i + 1]) {
      options.output = args[++i];
    } else if (arg === "--remove-bg") {
      // Optional color argument: "--remove-bg" keeps the default, "--remove-bg F6EFD9" names one.
      const next = args[i + 1];
      const named = next !== undefined && !next.startsWith("--");
      options.removeBg = named ? withHash(args[++i]) : true;
    } else if (arg === "--fuzz" && args[i + 1]) {
      options.fuzz = args[++i];
    } else if (arg === "--fit" && args[i + 1]) {
      options.fit = parseFloat(args[++i]);
    } else if (arg === "--resize" && args[i + 1]) {
      options.resize = args[++i];
    } else if (arg === "--filter" && args[i + 1]) {
      options.filter = args[++i] as "nearest" | "linear";
    } else if (arg === "--trim") {
      options.trim = true;
    } else if (arg === "--padding" && args[i + 1]) {
      options.padding = parseInt(args[++i], 10);
    } else if (arg === "--color-key" && args[i + 1]) {
      options.colorKey = args[++i].replace("#", "");
    } else if (arg === "--json") {
      options.outputJson = true;
    }
  }

  return options as ProcessOptions & { outputJson?: boolean };
}

// === Main CLI Handler ===
async function main(args: string[]): Promise<void> {
  if (args.length === 0 || args.includes("--help") || args.includes("-h")) {
    printHelp();
    Deno.exit(0);
  }

  const options = parseArgs(args);

  if (!options) {
    printHelp();
    Deno.exit(0);
  }

  if (!options.input) {
    console.error("Error: --input is required");
    Deno.exit(1);
  }
  if (!options.output) {
    console.error("Error: --output is required");
    Deno.exit(1);
  }

  const outputJson = options.outputJson;
  delete options.outputJson;

  if (!outputJson) {
    console.log(`\nProcessing: ${options.input}`);
  }

  const result = await processIcon(options);

  if (outputJson) {
    console.log(JSON.stringify(result, null, 2));
  } else {
    if (result.success) {
      console.log(`Output: ${result.output}`);
      if (result.originalSize && result.finalSize) {
        console.log(
          `Size: ${result.originalSize.width}x${result.originalSize.height} -> ${result.finalSize.width}x${result.finalSize.height}`
        );
      }
      console.log(`Operations: ${result.operations.join(", ")}`);
      if (result.error) {
        console.warn(`Warning: ${result.error}`);
      }
    } else {
      console.error(`Error: ${result.error}`);
      Deno.exit(1);
    }
  }
}

// === Entry Point ===
if (import.meta.main) {
  main(Deno.args);
}
