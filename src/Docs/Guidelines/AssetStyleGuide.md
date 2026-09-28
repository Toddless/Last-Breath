# Стайл-гайд генерации персонажей (канон утверждён Todd 2026-08-26)

Стиль спрайтов NPC: **комикс-стилизация в манере Battle Chasers / Darksiders** — жирная тушевая линия со штриховкой, утрированные героические пропорции (крупные кисти, массивные плечи, компактный тяжёлый силуэт, слегка увеличенные голова и оружие), краска поверх туши. **Не фотореализм, не чиби.** Палитра — как в Darkest Dungeon, но ещё менее насыщенная: выцветшие холодные серые и коричневые, блёклая олива, тусклая ржавчина акцентом, глубокие тени, мрачное готическое настроение. Существующий чиби-арт (Robber/Assasin/Thug) референсом НЕ является.

Тон мира — «скорее Ведьмак, чем Dark Souls»: мрачно и бедно, но жизнь продолжается; нежить — норма быта. Лор-зацепки по фракциям — в отчёте лор-разведки (05_Мир и Сюжет): люди — запад (двуручники) и восток (катаны); эльфы — высокие, элегантные, луки; гномы — коренастые, вычурные зачарованные доспехи, золото с камнями; нежить — градация «дозревания» от свежей до неотличимой от живых; демоны и мистические существа — лор пуст, внешность выдумывается с пометкой.

## Канонический базовый промпт (EN, вставляется целиком)

```
stylized comic book dark fantasy character, full body game sprite, bold expressive ink linework with crosshatching accents, exaggerated heroic proportions - oversized hands, massive shoulders, compact stocky silhouette, slightly oversized head and weapon, in the manner of Battle Chasers and Darksiders concept art, painted color over inked lines, heavily desaturated washed-out faded palette, near-muted colors like Darkest Dungeon but even less saturated - cold greys and browns, faded olive, dim rust accents, deep shadows, grim gothic mood, centered, isolated on plain light background, 2D game asset, no photorealism
```

## Конвейер

1. **Фронт** генерируется по канону + персонажный промпт (`fal-ai/flux/dev`, batch-generate.ts) и утверждается владельцем как якорь персонажа.
2. **Спина/бок** — через `anchored-generate.ts` (image-to-image от утверждённого фронта, strength ~0.7): консистентность бороды/одежды/оружия между ракурсами.
3. **Обработка** — process-sprite.ts (ImageMagick): прозрачный фон, 480×480 (формат кадров существующей библиотеки).
4. **Раскладка**: статичные фейсинги пакуются в `SpriteFrames` однокадровыми клипами `Idle_Down/Up/Left` (Right = FlipH от Left в твин-компоненте, #210), запись NPC — в `SharedData/Assets/Npc/Animations/NpcVisuals.tres`.

Ракурсов на NPC — три: Front (Down), Back (Up), Left. Ростер и приоритеты (A: демо-сцена, B: базовые враги/жители, C: фракционные, D: звери/боссы) — в отчёте лор-разведки сессии 2026-08-26.

## Связанные документы

- [Карта документации LastBreath](../README.md)
