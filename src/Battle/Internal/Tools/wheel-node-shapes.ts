#!/usr/bin/env -S deno run --allow-write
/**
 * wheel-node-shapes.ts — procedural placeholder faces for passive-wheel node classes.
 *
 * Why a script and not the AI pipeline (`.claude/skills/godot-asset-generator`): the wheel's node
 * faces are flat single-colour geometry whose ALPHA RAMP is quoted verbatim from
 * PassiveWheelStyle.tres (Gradient_ring / Gradient_disc). A generator that hits an exact pixel
 * size, an exact palette entry and an exact alpha profile beats a diffusion model that would then
 * need flood-fill, downscale and an eyeball pass — and it is re-runnable when the owner retunes a
 * Radius.
 *
 * The channel contract this obeys (Battle/Source/UIElements/PassiveWheel):
 *  - size  = 2 x Radius x PassiveWheelStyle.TextureOversample px  (PassiveNodeVisual.Body doc)
 *  - alpha = the same ramps the class textures use, measured on the POLYGON instead of the circle
 *  - colour is BAKED: PassiveNodeVisualConfig.ModulateOf paints authored art White unless the row
 *    authors a Tint, so the file must already be the colour the player sees.
 *
 * Alpha safety: RGB is constant over the WHOLE canvas and only alpha varies. That is exactly what
 * premultiply-resample-unpremultiply buys you (the reason the icon pipeline in
 * SharedData/Assets/Icons/ATTRIBUTION.md premultiplies before its downscale) — bilinear filtering
 * can never pull a dark or white fringe out of a texture whose colour channels are flat.
 *
 * Run:  deno run --allow-write wheel-node-shapes.ts --out <dir>
 */

// ---------------------------------------------------------------- palette --

/** Colours quoted from PassiveWheelStyle.cs — the wheel's own table, not a new palette. */
const NEUTRAL = [0.847, 0.706, 0.369] as const; // _neutral / _frontier / _socketOpen: bronze
const TAKEN = [0.941, 0.831, 0.537] as const; // _edgeTaken / _edgePath: "the gold of what is taken"

// ------------------------------------------------------------- alpha ramps --

type Stop = readonly [number, number];

/** Gradient_ring: 0 at 0.54, 1 at 0.78, 0 at the rim — an outline with the wheel's own softness. */
const RING: Stop[] = [[0, 0], [0.54, 0], [0.78, 1], [1, 0]];

/** Gradient_disc: solid to 0.72, faded out by the rim — the wheel's own filled face. */
const DISC: Stop[] = [[0, 1], [0.72, 1], [1, 0]];

function ramp(stops: Stop[], u: number): number {
    if (u <= stops[0][0]) return stops[0][1];
    for (let i = 1; i < stops.length; i++) {
        const [x1, y1] = stops[i];
        if (u > x1) continue;
        const [x0, y0] = stops[i - 1];
        return x1 === x0 ? y1 : y0 + (y1 - y0) * ((u - x0) / (x1 - x0));
    }
    return 0;
}

// ---------------------------------------------------------------- geometry --

/**
 * Signed distance to a regular n-gon with a VERTEX POINTING UP, circumradius R, centred on 0.
 * Convex, so the max of the edge half-planes is the distance.
 */
function polygonDistance(x: number, y: number, sides: number, radius: number): number {
    const apothem = radius * Math.cos(Math.PI / sides);
    let far = -Infinity;
    for (let i = 0; i < sides; i++) {
        // Vertices sit at -90deg + i*360/n; an edge normal bisects two neighbours.
        const angle = -Math.PI / 2 + (i + 0.5) * (2 * Math.PI / sides);
        far = Math.max(far, x * Math.cos(angle) + y * Math.sin(angle) - apothem);
    }
    return far;
}

/**
 * Where a point falls on the polygon's own radial scale: 0 at the centre, 1 on the outline. Exact,
 * because scaling a polygon scales its distance field — so the circle's gradient offsets can be
 * read straight off it and the outline keeps an even thickness all the way round.
 */
function polygonFraction(x: number, y: number, sides: number, radius: number): number {
    const apothem = radius * Math.cos(Math.PI / sides);
    return 1 + polygonDistance(x, y, sides, radius) / apothem;
}

// ----------------------------------------------------------------- drawing --

const SUPERSAMPLE = 4;

function render(size: number, sides: number, stops: Stop[], rgb: readonly number[]): Uint8Array {
    const red = Math.round(rgb[0] * 255);
    const green = Math.round(rgb[1] * 255);
    const blue = Math.round(rgb[2] * 255);
    const radius = size / 2;
    const pixels = new Uint8Array(size * size * 4);

    for (let py = 0; py < size; py++) {
        for (let px = 0; px < size; px++) {
            let alpha = 0;
            for (let sy = 0; sy < SUPERSAMPLE; sy++) {
                for (let sx = 0; sx < SUPERSAMPLE; sx++) {
                    const x = px + (sx + 0.5) / SUPERSAMPLE - radius;
                    const y = py + (sy + 0.5) / SUPERSAMPLE - radius;
                    alpha += ramp(stops, polygonFraction(x, y, sides, radius));
                }
            }
            alpha /= SUPERSAMPLE * SUPERSAMPLE;

            const at = (py * size + px) * 4;
            // Colour everywhere, alpha only in the shape: no fringe survives any filter.
            pixels[at] = red;
            pixels[at + 1] = green;
            pixels[at + 2] = blue;
            pixels[at + 3] = Math.round(Math.max(0, Math.min(1, alpha)) * 255);
        }
    }

    return pixels;
}

// ------------------------------------------------------------ png encoding --

const CRC_TABLE = (() => {
    const table = new Uint32Array(256);
    for (let n = 0; n < 256; n++) {
        let c = n;
        for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1;
        table[n] = c >>> 0;
    }
    return table;
})();

function crc32(bytes: Uint8Array): number {
    let c = 0xFFFFFFFF;
    for (const byte of bytes) c = CRC_TABLE[(c ^ byte) & 0xFF] ^ (c >>> 8);
    return (c ^ 0xFFFFFFFF) >>> 0;
}

function chunk(type: string, body: Uint8Array): Uint8Array {
    const out = new Uint8Array(body.length + 12);
    const view = new DataView(out.buffer);
    view.setUint32(0, body.length);
    for (let i = 0; i < 4; i++) out[4 + i] = type.charCodeAt(i);
    out.set(body, 8);
    view.setUint32(out.length - 4, crc32(out.subarray(4, out.length - 4)));
    return out;
}

async function zlib(data: Uint8Array): Promise<Uint8Array> {
    const stream = new Blob([data]).stream().pipeThrough(new CompressionStream("deflate"));
    return new Uint8Array(await new Response(stream).arrayBuffer());
}

async function encodePng(pixels: Uint8Array, width: number, height = width): Promise<Uint8Array> {
    const stride = width * 4;
    const raw = new Uint8Array((stride + 1) * height);
    for (let row = 0; row < height; row++) {
        raw[row * (stride + 1)] = 0; // filter: none
        raw.set(pixels.subarray(row * stride, (row + 1) * stride), row * (stride + 1) + 1);
    }

    const header = new Uint8Array(13);
    const view = new DataView(header.buffer);
    view.setUint32(0, width);
    view.setUint32(4, height);
    header[8] = 8; // bit depth
    header[9] = 6; // colour type: RGBA
    // 10..12 stay 0: deflate, adaptive filtering, no interlace.

    const parts = [
        new Uint8Array([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        chunk("IHDR", header),
        chunk("IDAT", await zlib(raw)),
        chunk("IEND", new Uint8Array(0)),
    ];

    const png = new Uint8Array(parts.reduce((sum, part) => sum + part.length, 0));
    let at = 0;
    for (const part of parts) {
        png.set(part, at);
        at += part.length;
    }
    return png;
}

// -------------------------------------------------------------------- main --

interface Face {
    readonly file: string;
    /** 2 x Radius x TextureOversample, per the rule on PassiveNodeVisual.Body. */
    readonly size: number;
    readonly sides: number;
    readonly stops: Stop[];
    readonly rgb: readonly number[];
}

/** Radius 6.0 and 7.5 from PassiveWheelStyle.tres; oversample 4 = CanvasTransform.MaxZoom. */
const FACES: Face[] = [
    { file: "SocketTier2Body.png", size: 48, sides: 4, stops: RING, rgb: NEUTRAL },
    { file: "SocketTier2Taken.png", size: 48, sides: 4, stops: DISC, rgb: TAKEN },
    { file: "SocketTier3Body.png", size: 60, sides: 6, stops: RING, rgb: NEUTRAL },
    { file: "SocketTier3Taken.png", size: 60, sides: 6, stops: DISC, rgb: TAKEN },
];

export { DISC, encodePng, NEUTRAL, polygonFraction, ramp, render, RING, TAKEN };

if (import.meta.main) {
    const index = Deno.args.indexOf("--out");
    const directory = index >= 0 ? Deno.args[index + 1] : ".";

    for (const face of FACES) {
        const png = await encodePng(render(face.size, face.sides, face.stops, face.rgb), face.size);
        await Deno.writeFile(`${directory}/${face.file}`, png);
        console.log(`${face.file}  ${face.size}x${face.size}  ${face.sides}-gon  ${png.length} bytes`);
    }
}
