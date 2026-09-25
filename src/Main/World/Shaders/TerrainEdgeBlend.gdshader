shader_type canvas_item;

// This material belongs to the upper terrain display. It widens the authored alpha into a density field: the
// upper surface is solid toward its body and becomes progressively sparser over the lower TileMapLayer.
// CanvasItem alpha blending performs the actual cross-layer mix; the shader never re-renders the lower layer.
uniform sampler2D edge_noise : repeat_enable, filter_linear;

uniform float blend_width : hint_range(0.0, 48.0) = 24.0;
uniform float blend_low : hint_range(0.0, 1.0) = 0.04;
uniform float blend_high : hint_range(0.0, 1.0) = 0.82;
uniform float outward_bias : hint_range(-0.25, 0.25) = 0.04;
uniform float edge_roughen : hint_range(0.0, 0.25) = 0.05;
uniform float edge_noise_size : hint_range(32.0, 1024.0) = 192.0;
uniform float scatter_size : hint_range(4.0, 128.0) = 18.0;
uniform float scatter_strength : hint_range(0.0, 1.0) = 0.72;
uniform float scatter_softness : hint_range(0.01, 0.4) = 0.12;

varying vec2 world_pos;

const vec2 ATLAS_GRID = vec2(4.0);
const vec2 FILLED_TILE = vec2(3.0);
const int SAMPLE_COUNT = 8;
const vec2 SAMPLE_DIRECTIONS[SAMPLE_COUNT] = {
	vec2(1.0, 0.0),
	vec2(0.70710678, 0.70710678),
	vec2(0.0, 1.0),
	vec2(-0.70710678, 0.70710678),
	vec2(-1.0, 0.0),
	vec2(-0.70710678, -0.70710678),
	vec2(0.0, -1.0),
	vec2(0.70710678, -0.70710678)
};

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

vec2 tile_clamped_uv(vec2 uv, vec2 pixel_size, vec2 offset_pixels) {
	vec2 tile = floor(uv * ATLAS_GRID);
	vec2 half_pixel = pixel_size * 0.5;
	vec2 tile_min = tile / ATLAS_GRID + half_pixel;
	vec2 tile_max = (tile + vec2(1.0)) / ATLAS_GRID - half_pixel;
	return clamp(uv + offset_pixels * pixel_size, tile_min, tile_max);
}

void fragment() {
	vec4 upper = COLOR;
	vec2 atlas_tile = floor(UV * ATLAS_GRID);

	// Mask 1111 is solid upper terrain and must preserve the authored texture exactly.
	if (all(equal(atlas_tile, FILLED_TILE)) || blend_width <= 0.0) {
		COLOR = upper;
	} else {
		float coverage = upper.a;
		vec3 propagated_color = upper.rgb * upper.a;
		float propagated_weight = upper.a;
		for (int index = 0; index < SAMPLE_COUNT; index++) {
			vec2 direction = SAMPLE_DIRECTIONS[index];
			vec4 near_sample = texture(TEXTURE, tile_clamped_uv(UV, TEXTURE_PIXEL_SIZE, direction * blend_width / 3.0));
			vec4 middle_sample = texture(TEXTURE, tile_clamped_uv(UV, TEXTURE_PIXEL_SIZE, direction * blend_width * 2.0 / 3.0));
			vec4 far_sample = texture(TEXTURE, tile_clamped_uv(UV, TEXTURE_PIXEL_SIZE, direction * blend_width));

			coverage += (near_sample.a + middle_sample.a + far_sample.a) / float(SAMPLE_COUNT);
			propagated_color += near_sample.rgb * near_sample.a;
			propagated_color += middle_sample.rgb * middle_sample.a;
			propagated_color += far_sample.rgb * far_sample.a;
			propagated_weight += near_sample.a + middle_sample.a + far_sample.a;
		}
		coverage *= 0.25;

		float noise_shift = (texture(edge_noise, world_pos / edge_noise_size).r - 0.5) * 2.0 * edge_roughen;
		float density = smoothstep(blend_low + noise_shift, blend_high + noise_shift, coverage + outward_bias);

		// Fine world-space noise turns the continuous falloff into progressively rarer islands of grass. Keeping
		// part of the smooth density avoids a binary, torn-paper silhouette.
		float scatter_noise = texture(edge_noise, world_pos / scatter_size).r;
		float scatter_threshold = mix(0.12, 0.88, scatter_noise);
		float sparse_density = smoothstep(scatter_threshold - scatter_softness, scatter_threshold + scatter_softness, density);
		float final_density = mix(density, sparse_density, scatter_strength);

		vec3 carried_grass = propagated_color / max(propagated_weight, 0.0001);
		COLOR.rgb = mix(carried_grass, upper.rgb, upper.a);
		COLOR.a = final_density;
	}
}
