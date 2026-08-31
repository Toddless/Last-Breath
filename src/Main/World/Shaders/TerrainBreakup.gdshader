shader_type canvas_item;

// Breaks up the visible repetition of tiled ground art. The variation is read
// from seamless noise TEXTURES (FastNoiseLite, tunable on the material) and
// sampled in WORLD space, so the effect is glued to the map, flows across tile
// borders, and its features ignore the grid — which is what hides the pattern.
//
// The same material works on a transition overlay: colour work only touches
// opaque pixels, and edge_roughen (off by default) additionally wobbles the
// authored alpha edge so the border reads organic instead of stamped.
//
// show_noise paints the raw macro noise instead of the ground — flip it on to
// check the material is actually live and to eyeball feature size.

uniform sampler2D macro_noise : repeat_enable, filter_linear;
uniform sampler2D grain_noise : repeat_enable, filter_nearest;

uniform bool show_noise = false;

// --- macro variation: large soft blotches of warm/cool drift ---
uniform float macro_size : hint_range(128.0, 4096.0) = 900.0;  // world px one noise tile spans
uniform float macro_strength : hint_range(0.0, 0.5) = 0.18;    // brightness drift amplitude
uniform float tint_strength : hint_range(0.0, 1.0) = 0.5;      // how much of the warm/cool tint applies
uniform vec3 warm_tint : source_color = vec3(1.05, 1.02, 0.95);
uniform vec3 cool_tint : source_color = vec3(0.94, 0.97, 1.04);

// --- micro grain: fine speckle over the macro blotches ---
uniform float grain_size : hint_range(16.0, 512.0) = 96.0;     // world px one grain tile spans
uniform float grain_strength : hint_range(0.0, 0.3) = 0.07;

// --- edge work (transition overlays only, keep 0 on the base ground) ---
uniform float edge_roughen : hint_range(0.0, 0.5) = 0.0;       // organic wobble of the alpha edge
uniform float edge_noise_size : hint_range(32.0, 1024.0) = 160.0;

varying vec2 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
	float n = texture(macro_noise, world_pos / macro_size).r;

	if (show_noise) {
		COLOR.rgb = vec3(n);
	} else {
		vec3 tint = mix(cool_tint, warm_tint, n);
		vec3 col = COLOR.rgb * mix(vec3(1.0), tint, tint_strength);
		col *= 1.0 + (n - 0.5) * 2.0 * macro_strength;

		float g = texture(grain_noise, world_pos / grain_size).r;
		col *= 1.0 + (g - 0.5) * 2.0 * grain_strength;

		COLOR.rgb = col;
	}

	if (edge_roughen > 0.0) {
		// Only the semi-transparent rim moves: fully opaque body and fully clear
		// ground are anchored, so the wobble reshapes the border without eating
		// the terrain or leaking past it.
		float rim = smoothstep(0.02, 0.6, COLOR.a) * (1.0 - smoothstep(0.9, 0.98, COLOR.a));
		float e = texture(macro_noise, world_pos / edge_noise_size).r;
		COLOR.a = clamp(COLOR.a + (e - 0.5) * 2.0 * edge_roughen * rim, 0.0, 1.0);
	}
}
