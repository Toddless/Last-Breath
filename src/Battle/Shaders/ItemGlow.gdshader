shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float intensity = 1.0;
uniform float falloff = 2.0;      // как резко гаснет к краям. Больше = меньше и плотнее пятно.
uniform float pulse_speed = 1.5;
uniform float pulse_strength = 0.2;

void fragment() {
    // Расстояние от центра спрайта (0.5, 0.5) до текущего пикселя.
    // length(...) даёт евклидово расстояние -> получаем КРУГ, а не квадрат.
    float dist = length(UV - vec2(0.5)) * 2.0; // 0 в центре, ~1 у края

    // Радиальный градиент: 1 в центре, 0 к краю. pow задаёт крутизну затухания.
    float radial = pow(clamp(1.0 - dist, 0.0, 1.0), falloff);

    // Мягкая пульсация, чтобы пятно "дышало" синхронно ощущению луча.
    float pulse = 1.0 - pulse_strength + pulse_strength * (0.5 + 0.5 * sin(TIME * pulse_speed));

    float alpha = radial * pulse;
    COLOR = vec4(glow_color.rgb * intensity, alpha * glow_color.a);
}
