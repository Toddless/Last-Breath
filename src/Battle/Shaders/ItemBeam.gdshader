shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 beam_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float horizontal_sharpness = 2.5;
uniform float intensity = 1.6;

// --- Параметры шума ---
uniform float noise_scale = 12.0;   // сколько "полос" по высоте. Больше = мельче рябь.
uniform float noise_speed = 2.0;    // как быстро энергия течёт вверх.
uniform float noise_strength = 0.4; // 0 = ровный луч, 1 = сильное мерцание.

// hash: подделка случайности из координаты (см. объяснение).
float hash(float n) {
    return fract(sin(n) * 43758.5453123);
}

// value noise 1D: плавный шум вдоль оси.
float noise(float x) {
    float i = floor(x);
    float f = fract(x);
    f = f * f * (3.0 - 2.0 * f); // сглаживание переходов между узлами
    return mix(hash(i), hash(i + 1.0), f);
}

void fragment() {
    // Горизонтальный профиль: ярко в центре, прозрачно к краям.
    float dist_x = abs(UV.x - 0.5) * 2.0;
    float horizontal = pow(1.0 - dist_x, horizontal_sharpness);

    // Вертикальный профиль: ярко снизу, гаснет вверх.
    float vertical = smoothstep(0.0, 1.0, UV.y);

    // Шум течёт вверх: минус TIME сдвигает полосы к вершине.
    float n = noise(UV.y * noise_scale - TIME * noise_speed);
    // Смешиваем "ровный луч" (1.0) и шум. noise_strength управляет глубиной эффекта.
    float flicker = mix(1.0, n, noise_strength);

    float alpha = horizontal * vertical * flicker;

    COLOR = vec4(beam_color.rgb * intensity, alpha * beam_color.a);
}
