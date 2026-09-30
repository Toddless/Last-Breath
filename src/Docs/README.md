# Документация LastBreath

Одна папка объединяет документы одной системы. Описание существующей реализации хранится отдельно от архитектуры новых возможностей, концепций и решений. Описания систем пишутся на русском; существующие проектные документы при этой реорганизации не переводились.

- [Правила для агента и краткая карта систем](CLAUDE.md).
- [Сверка с кодом: источник, исправленные расхождения и ограничения](Guidelines/SystemDocumentationAudit.md).
- [Исторический контекст сессий](HANDOFF.md).
- [Журнал решений](decisions.md).

## Существующие системы и связанные проекты

Файлы CurrentSystem.md описывают существующую реализацию. Название Architecture/Concept/Decisions само по себе не подтверждает реализацию: статус читается внутри документа. Для боя дополнительно выделено представление, остающееся в папке Combat.

| Система | Существующая реализация | Проектирование и расширения |
| --- | --- | --- |
| Abilities | [Активные способности](Abilities/CurrentSystem.md) | — |
| AI | [AI, создание и жизненный цикл NPC](AI/CurrentSystem.md) | [Группы NPC, оповещения и подкрепления — проект архитектуры](AI/EntityGroupsArchitecture.md) |
| Augments | [Аугменты, сокеты и орнаменты](Augments/CurrentSystem.md) | — |
| Combat | [Боевой урон и события](Combat/CurrentSystem.md); [Воспроизведение боя, анимации и VFX](Combat/Presentation.md) | [Расширение боевой системы — проект архитектуры](Combat/CombatExpansionArchitecture.md); [Расширение боевой системы](Combat/CombatExpansionConcept.md); [Расширение боевой системы — обсуждение решений](Combat/CombatExpansionDecisions.md) |
| Crafting | [Существующая система крафта](Crafting/CurrentSystem.md) | [Расширение крафта — теги модификаторов](Crafting/CraftingTagsConcept.md); [Расширение крафта — обсуждение решений](Crafting/CraftingTagsDecisions.md) |
| Effects | [Существующая система эффектов](Effects/CurrentSystem.md) | [Временные эффекты: расширение системы бафов и дебафов](Effects/TimedEffectsArchitecture.md) |
| GameData | [Загрузка игровых данных](GameData/CurrentSystem.md) | — |
| Interaction | [Общее взаимодействие, сундуки и NPC](Interaction/CurrentSystem.md) | [Shared Interaction Architecture](Interaction/InteractionArchitecture.md); [Интерактивное окружение](Interaction/InteractiveEnvironmentConcept.md); [Интерактивное окружение — обсуждение решений](Interaction/InteractiveEnvironmentDecisions.md); [NPC в общей системе взаимодействия — #266](Interaction/NpcInteractions.md) |
| Items | [Экипировка и гранты предметов](Items/CurrentSystem.md) | [Используемые предметы — проект архитектуры](Items/UsableItemsArchitecture.md) |
| Localization | [Локализация и форматирование текста](Localization/CurrentSystem.md) | — |
| Locations | [Локации, пространства боя и снимки мира](Locations/CurrentSystem.md) | [Location and Battle Spaces](Locations/LocationSpaces.md) |
| Loot | [Генерация лута](Loot/CurrentSystem.md) | — |
| Parameters | [Параметры сущностей и модификаторы](Parameters/CurrentSystem.md) | — |
| PassiveTree | [Пассивное дерево и пассивные способности](PassiveTree/CurrentSystem.md) | — |
| Reputation | [Репутация и рейды](Reputation/CurrentSystem.md) | — |
| Services | [Регистрация сервисов и межсистемные сообщения](Services/CurrentSystem.md) | — |
| Trade | [Торговля и кошелёк](Trade/CurrentSystem.md) | — |
| UI | [Окна, HUD и всплывающие элементы](UI/CurrentSystem.md) | — |

## Другие проектные документы

- [Разработка Godot/C#: жизненный цикл, состояние и интеграция](Guidelines/GodotDevelopment.md).
- [Проверка изменений: тесты, Godot и приёмка](Guidelines/Verification.md).
- [Автоматизация квестов](Narrative/QuestAutomationArchitecture.md).
- [Тайлы DualGrid](DualGrid/DualGridTileset.md).
- [Шаблон запроса на проектирование системы](Guidelines/SystemArchitecturePrompt.md).
- [Экспорт инструментов](Guidelines/ToolExports.md).
- [Стиль игровых ассетов](Guidelines/AssetStyleGuide.md).

Отдельные описания этих систем не создавались из CLAUDE.md: в нём не было самостоятельного описания их существующей реализации.

## Как поддерживать документацию

1. При изменении работающей системы обновлять её описание и связанные документы в той же папке.
2. Новую возможность до реализации описывать отдельным проектным документом. Не дописывать её в текущее поведение как уже работающую.
3. Ссылаться на описание системы, вместо копирования одного правила в несколько документов. Ссылки на документы — относительные Markdown-ссылки.
4. При переносе или переименовании документа обновлять входящие ссылки и эту карту.
5. В CLAUDE.md держать только правила и навигацию; исторические записи HANDOFF.md и decisions.md не заменяют описание текущей реализации.

В этой редакции изменена только копия agent-settings. Она не синхронизирована автоматически с src/Docs рабочего дерева LastBreath.
