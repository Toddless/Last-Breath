# Регистрация сервисов и межсистемные сообщения

Статус: описание существующей реализации. Основа и границы статической сверки — [в отчёте](../Guidelines/SystemDocumentationAudit.md).

## Устройство и поведение

- GameServiceProvider своеобразный DI контейнер. Регистрируем и храним необходимые игровые сервисы там. У каждого проекта есть свое расширение позволяющее зарегистрировать необходимые зависимости.
- GameEventBus глобальная игровая шина событый. Служит для уведомления систем на глобальном уровне (к примеру начало и конец боя).
- GameMessageBus глобальная шина обмена информацией между системами. SendRequest разрешает один IRequestHandler и ожидает его ответ. PublishMessageAsync вызывает все IMessageHandler данного типа и ожидает Task.WhenAll; сама реализация не является fire-and-forget.

## Проверенные точки реализации

Пути относительно `src` LastBreath: `Core/Services/GameMessageBus.cs`, `Core/Services/GameEventBus.cs`, `Main/Services/GameServiceProvider.cs`.

## Связанные документы

- [Загрузка игровых данных](../GameData/CurrentSystem.md)
- [Боевой урон и события](../Combat/CurrentSystem.md)
- [Окна, HUD и всплывающие элементы](../UI/CurrentSystem.md)
- [Общее взаимодействие, сундуки и NPC](../Interaction/CurrentSystem.md)
- [Карта документации](../README.md)
