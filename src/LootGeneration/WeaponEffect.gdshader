shader_type canvas_item;
// Обычный alpha-blend: мы модифицируем существующий спрайт, а не рисуем свет с нуля.
render_mode unshaded;

uniform vec4 shine_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float shine_strength = 1.2;   // насколько ярко вспыхивает блик
uniform float shine_width = 0.12;     // ширина полосы (0..1 по диагонали)
uniform float angle = 0.6;            // наклон полосы в радианах (~35°)

// Тайминг: полный цикл и какая его доля занята пробегом блика.
uniform float period = 4.0;           // сек между бликами
uniform float sweep_fraction = 0.35;  // доля цикла, за которую блик проходит спрайт
uniform bool reverse = false;         // сменить направление хода блика на противоположное

// Маскирование блика по краю лезвия:
uniform float luminance_mask = 1.0;   // 1 = блик только по светлым местам (кромка), 0 = по всему спрайту
uniform sampler2D edge_mask;          // опционально: белым помечена кромка (способ Б)
uniform bool use_edge_mask = false;   // включи, если подключил edge_mask

void fragment() {
    // Родной цвет спрайта в этой точке.
    vec4 base = texture(TEXTURE, UV);

    // --- 1. Позиция вдоль диагонали ---
    // Проецируем UV на направление под углом angle -> одно число 0..~1.
    vec2 dir = vec2(cos(angle), sin(angle));
    float proj = dot(UV, dir);

    // --- 2. Где сейчас центр полосы ---
    // fract даёт пилу 0..1 за каждый period. Делим на sweep_fraction и клампим:
    // блик проходит спрайт за первую часть цикла, потом ждёт до конца.
    float cycle = fract(TIME / period);
    float sweep_pos = clamp(cycle / sweep_fraction, 0.0, 1.0);
    // Концы хода с запасом за края (-0.2 .. 1.2), чтобы блик въезжал и выезжал за кадр.
    // reverse меняет местами старт и финиш -> ход идёт в обратную сторону.
    float from = reverse ? 1.2 : -0.2;
    float to   = reverse ? -0.2 : 1.2;
    float center = mix(from, to, sweep_pos);

    // --- 3. Яркость полосы: пик в center, гаснет по краям ---
    float band = smoothstep(shine_width, 0.0, abs(proj - center));

    // --- 4. Маска "только по кромке" ---
    // (А) по яркости: тёмные пиксели почти не ловят блик.
    float lum = dot(base.rgb, vec3(0.299, 0.587, 0.114));
    float mask = mix(1.0, lum, luminance_mask);
    // (Б) по нарисованной маске, если включена.
    if (use_edge_mask) {
        mask *= texture(edge_mask, UV).r;
    }

    // --- 5. Складываем блик поверх спрайта ---
    // Умножаем на base.a, чтобы блик не вылезал за пределы силуэта.
    float shine = band * mask * shine_strength * base.a;
    vec3 rgb = base.rgb + shine_color.rgb * shine;

    COLOR = vec4(rgb, base.a);
}