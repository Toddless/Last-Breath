# Общее взаимодействие, сундуки и NPC

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- PlayerInteractionController — единый владелец E для выходов, сундуков и NPC; Area2D поддерживает кандидатов, точная проверка выполняется с интервалом 0,1 с и перед действием. Слой физики 16 — InteractionTargets; препятствия настраиваются через ObstacleMask цели.
- InteractionService связывает окно с живой целью и пространством; команды идут через GameMessageBus, проверяются в физическом цикле и отменяются при закрытии/выгрузке. Действия компонует IInteractionSource. NPC уже подключены к этому механизму.
- ChestCatalog читает SharedData/Chests. ChestComponent сохраняет конкретные предметы, остатки слотов и абсолютный срок удаления через ILocationStateParticipant. Частичный перенос использует IInventoryTransfer.ReceiveUpTo и отложенные уведомления: остатки источника фиксируются до событий инвентаря.
- Первый сундук — StarterChest в SourceOfPowerNearVillage, без замка, пять Uncommon-предметов из JSON. Сапоги имеют ID Boots_Stone_Tread. E открывает, ПКМ переносит позицию, R забирает всё помещающееся. Пустой сундук исчезает через настраиваемые 5 игровых минут.

## NPC

- TestNpc содержит NpcInteractionTarget с DialogueActor и NpcAttackActionSource. Разговор запускается через E/меню; старой кликовой области нет. Атака всегда требует явного выбора, включая случай единственного действия.
- IDialogueService.CanStart и Start разрешают вход одним способом. INarrativeCondition.IsPreviewSafe — явное разрешение предварительной проверки; по умолчанию false, составные условия требуют безопасности каждого ребёнка. QuestOfferRoll и косвенный CanAcceptQuest запрещены во входах, но сохраняют работу в репликах.
- BaseNpc.TryStartBattleWith объединяет контакт и явную атаку. BattleContext помечает только принятых участников; нельзя использовать уведомление Attacked всей группе для установки IsFighting. Позиция/пространство шума фиксируются до переноса в арену.
- InteractionTarget регистрируется при каждом входе в дерево локации и получает новую привязку; NPC использует InstanceId. Смерть/бой/преграда закрывают меню, уход или удаление собеседника завершают принадлежащий ему диалог. Статические собеседники используют обычную цель и авторские параметры DialogueActor.

## Уточнения текущего рабочего дерева

`InteractionTarget` может получать идентичность и доступность от `IInteractionOwner`. Для сундука ссылка `Target` задаётся в сцене и должна вести на непосредственного ребёнка; ошибочная настройка отключает сундук с диагностикой. Цель поддерживает несколько точек взаимодействия и проверяет, покрывает ли их область обнаружения с учётом дистанции.

`ChestContents` владеет остатками и абсолютным сроком удаления. `ChestComponent` связывает содержимое с каталогом, представлением, часами и сохранением. Нечитаемое сохранённое состояние сундука удерживается для следующего сохранения вместо молчаливой потери. NPC получает ID через `npc/` + `InstanceId`.

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Main/World/Interactions/PlayerInteractionController.cs`, `Main/World/Interactions/InteractionService.cs`, `Main/World/Interactions/InteractionTarget.cs`, `Main/World/Containers/ChestComponent.cs`, `Core/World/Containers/ChestContents.cs`, `Main/Npc/NpcInteractionTarget.cs`, `Core/Narrative/Dialogues/DialogueService.cs`, `SharedData/Chests/Chests.json`.

## Связанные документы

- [InteractionArchitecture](../Interaction/InteractionArchitecture.md)
- [NpcInteractions](../Interaction/NpcInteractions.md)
- [InteractiveEnvironmentConcept](../Interaction/InteractiveEnvironmentConcept.md)
- [Локации, пространства боя и снимки мира](../Locations/CurrentSystem.md)
- [Экипировка и гранты предметов](../Items/CurrentSystem.md)
- [AI, создание и жизненный цикл NPC](../AI/CurrentSystem.md)
- [Карта документации](../README.md)
