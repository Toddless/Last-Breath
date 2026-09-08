shader_type canvas_item;

// Breaks up the visible repetition of tiled ground art. The variation is read
// from seamless noise TEXTURES (FastNoiseLite, tunable on the material) and
// sampled in WORLD space, so the effect is glued to the map, flows across tile
// borders, and its features ignore the grid — which is what hides the pattern.
//
// show_noise paints the raw macro noise instead of the ground — flip it on to
// check the material is actually live and to eyeball feature size.

uniform sampler2D macro_noise : repeat_enable, filter_linear;
uniform sampler2D grain_noise : repeat_enable, filter_linear;

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
}
