shader_type canvas_item;
render_mode blend_add, unshaded;

// The idle breath of a taken node. Modelled on ItemOnGroundGlow: the pulse is computed from TIME in
// the fragment stage, so a wheel full of taken nodes costs the CPU nothing per frame — no _Process,
// no timer, no per-node subscription. Whether a node breathes at all is one line in the view
// (the glow is visible only while the node is taken), because "every taken node has a scene" is
// already guaranteed by the carrier rule.
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float intensity = 1.0;
uniform float falloff = 2.0;
uniform float pulse_speed = 1.5;
uniform float pulse_strength = 0.25;

// Seeded per node from its id. Without it a hundred taken nodes breathe in unison, which reads as a
// bug rather than as life.
uniform float phase = 0.0;

void fragment() {
	float dist = length(UV - vec2(0.5)) * 2.0;
	float radial = pow(clamp(1.0 - dist, 0.0, 1.0), falloff);
	float breath = 0.5 + 0.5 * sin(TIME * pulse_speed + phase * 6.2831853);
	float pulse = 1.0 - pulse_strength + pulse_strength * breath;

	float alpha = radial * pulse;
	COLOR = vec4(glow_color.rgb * intensity, alpha * glow_color.a);
}
