# Окна, HUD и всплывающие элементы

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- **Три слоя**: Hud (постоянный, один активный) / Window (временные комплексные окна, `IWindow`) / **Overlay** (тултипы, попапы, нотификации — `IPopup` с `PopupLifetime` Timed/WhileHovered/Pinned и `OverlayRegion` Cursor/TopCenter/TopRight/BottomRight; системный фид едет в BottomRight). Расположение: элемент декларирует ТОЛЬКО регион; слой владеет геометрией — регионы это ноды-контейнеры в сцене (VBox стекует соседей, наложение исключено). Нотификации: категория в `SendNotificationMessageMessage` → регион (маппинг в NotificationService). Всё открывается через `IUiElementsManager`: `ToggleWindow` (хоткеи: повтор = закрыть) / `OpenWindow` (открыть или вернуть открытое) / `ShowPopup` (всегда новая инстанция; предыдущий попап ТОГО ЖЕ типа закрывается, разные типы сосуществуют).
- **Жизненный цикл**: новое открытие закрытого окна создаёт новую инстанцию; OpenWindow для уже открытого окна возвращает существующую, закрытие = `QueueFree` (`Close()` умирает сам, менеджер подстраховывает FreeIfValid). Окно с подписками на шины ОБЯЗАНО отписываться при смерти (`_ExitTree`).
- **Esc по слоям** (`HandleEscape` в UiElementsManager): первое нажатие чистит Overlay, следующее закрывает все окна с `IsDismissable` (у GameOver — false). Pinned-попапы также закрываются кликом мимо.
- Границы видимости плавающих элементов — только через `UiPlacement.PlaceClamped` (Core).

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Core/Services/UiElementsManager.cs`, `Core/Views/UI/IUiElementsManager.cs`, `Main/Services/UiLayerManager.cs`.

## Связанные документы

- [Воспроизведение боя, анимации и VFX](../Combat/Presentation.md)
- [Общее взаимодействие, сундуки и NPC](../Interaction/CurrentSystem.md)
- [Торговля и кошелёк](../Trade/CurrentSystem.md)
- [Пассивное дерево и пассивные способности](../PassiveTree/CurrentSystem.md)
- [Локализация и форматирование текста](../Localization/CurrentSystem.md)
- [Карта документации](../README.md)
