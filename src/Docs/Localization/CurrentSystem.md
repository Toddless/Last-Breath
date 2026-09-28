# Локализация и форматирование текста

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- .po файлы (en+ru) — ЕДИНСТВЕННАЯ копия в `SharedData/Localization`, подключены во все 4 project.godot через симлинк Data/Shared. Каталог ru содержит и переведённые значения, и незаполненные строки; фактический выбор локали и перевод выполняет TranslationServer. Конвенция ключей: имя = `<Id>`, описание = `<Id>_Description`, интерфейс = `UI_*`.
- Архитектура: чистое ядро `Core/Localization` + DI (регистрируется в shared-сервисах): `ILocalizationProvider` (Godot-адаптер над TranslationServer, событие LocaleChanged, плюрализация), `ILocalizationService`, статик-фасад `Localization` — тонкая обёртка для UI.
- Описания — шаблоны с именованными плейсхолдерами: `{Damage}`, доля-в-процент `{chance:%}` (×100+знак), плюрализация `{Duration|turn|turns}`, ключевые слова `{@Effect_X}` (клик → карточка `<Key>_Tooltip`, подключение UI — `KeywordLinks.Attach`), экранирование `{{}}`; движок `TextTemplateEngine`. Значения отдаёт домен (`DescriptionValues` у Ability/Effect/Skill; у апгрейдов — автоматом из json `upgradeProperties`). Неизвестный плейсхолдер остаётся видимым в тексте. `TextFormat.Plain` для Label / `TextFormat.Rich` (BBCode, числа красятся `TextPalette.Number`). Синтаксис шаблонов приведён выше; отдельного AbilityDescriptions.md в текущей документационной копии нет. Battle log: тексты `Log_*`/`DamageType_*` в .po, цвета — только из `TextPalette`.
- Модификаторы: шаблоны `Modifier_Flat/Increase/Multiplicative` (+ `_Range` для вилок, + `_Negative` для процентных штрафов — «25% less X» вместо «-25% more X»; выбор шаблона — одна развилка `ModifierFormatter.RenderLine`) в .po; единицы параметров — `SharedData/Formatting/ParameterFormats.json` (источник правды; в данных доли: 0.5 = 50%). Числа всегда с точкой (InvariantCulture).
- Новый форматируемый тип = класс `ITextFormatter` + DI-регистрация (реестр вместо switch).

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Core/Localization/GodotLocalizationProvider.cs`, `Core/Localization/LocalizationService.cs`, `Core/Localization/TextTemplateEngine.cs`, `SharedData/Localization/ru.po`, `SharedData/Formatting/ParameterFormats.json`.

## Связанные документы

- [Активные способности](../Abilities/CurrentSystem.md)
- [Экипировка и гранты предметов](../Items/CurrentSystem.md)
- [Окна, HUD и всплывающие элементы](../UI/CurrentSystem.md)
- [Загрузка игровых данных](../GameData/CurrentSystem.md)
- [Карта документации](../README.md)
