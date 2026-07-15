namespace Core.Narrative.Quests
{
    using System.Collections.Generic;
    using Events;
    using MessageBus;
    using MessageBus.Messages;
    using Save;

    /// <summary>
    /// Maps quest bus events to notification toasts (mirrors ReputationBroadcaster: eager
    /// singleton living on subscriptions, muted while a save is being applied). Quest display
    /// name comes from the .po by the quest id itself (ids already carry the Quest_ prefix).
    /// </summary>
    public class QuestNotificationBroadcaster
    {
        private static readonly Dictionary<QuestStatus, string> s_statusKeys = new()
        {
            [QuestStatus.Active] = "UI_Quest_Accepted",
            [QuestStatus.ReadyToTurnIn] = "UI_Quest_Ready",
            [QuestStatus.Completed] = "UI_Quest_Completed",
            [QuestStatus.Failed] = "UI_Quest_Failed",
        };

        private readonly IGameMessageBus _messages;
        private readonly ILoadScope _loadScope;

        public QuestNotificationBroadcaster(IGameEventBus events, IGameMessageBus messages, ILoadScope loadScope)
        {
            _messages = messages;
            _loadScope = loadScope;
            events.Subscribe<QuestStatusChangedEvent>(OnStatusChanged);
            events.Subscribe<QuestStageAdvancedEvent>(evnt => Toast("UI_Quest_Updated", evnt.QuestId));
        }

        private void OnStatusChanged(QuestStatusChangedEvent evnt)
        {
            if (s_statusKeys.TryGetValue(evnt.Status, out string? key)) Toast(key, evnt.QuestId);
        }

        private void Toast(string key, string questId)
        {
            if (_loadScope.IsLoading) return;
            _ = _messages.PublishMessageAsync(new SendNotificationMessageMessage(key, NotificationCategory.System,
                new Dictionary<string, object?> { ["Quest"] = Localization.Localization.Localize(questId) }));
        }
    }
}
