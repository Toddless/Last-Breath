namespace Core.Reputation
{
    using System.Collections.Generic;
    using Entity;
    using Enums;
    using Events;
    using Events.GameEvents;
    using MessageBus;
    using Save;

    /// <summary>
    /// Bridges IFactionRelationService's C# events outward: republishes them on the game bus
    /// (so interested systems don't inject the service) and feeds the notification overlay.
    /// Everything is muted while a save is being applied. Direct writes (save-load, quest sets)
    /// republish but never toast.
    /// </summary>
    public class ReputationBroadcaster
    {
        private const string ReputationNotificationId = "UI_Reputation_Changed";
        private const string StandingNotificationId = "UI_Standing_Changed";

        private readonly IGameEventBus _events;
        private readonly IGameMessageBus _messages;
        private readonly ILoadScope _loadScope;

        public ReputationBroadcaster(IFactionRelationService relations, IGameEventBus events, IGameMessageBus messages, ILoadScope loadScope)
        {
            _events = events;
            _messages = messages;
            _loadScope = loadScope;
            relations.PlayerReputationChanged += OnReputationChanged;
            relations.PlayerRelationChanged += OnStandingChanged;
        }

        private void OnReputationChanged(ReputationChangedArgs change)
        {
            if (_loadScope.IsLoading) return;

            _events.Publish(new ReputationChangedEvent(change));
            if (change.Reason == ReputationReasons.DirectSet) return;

            _ = _messages.PublishMessageAsync(new SendNotificationMessageMessage(ReputationNotificationId, NotificationCategory.System,
                new Dictionary<string, object?>
                {
                    ["Faction"] = FactionName(change.Faction),
                    ["Delta"] = change.Delta > 0 ? $"+{change.Delta}" : change.Delta.ToString(),
                }));
        }

        private void OnStandingChanged(Fractions faction, RelationLevel level)
        {
            if (_loadScope.IsLoading) return;

            _events.Publish(new PlayerStandingChangedEvent(faction, level));
            _ = _messages.PublishMessageAsync(new SendNotificationMessageMessage(StandingNotificationId, NotificationCategory.System,
                new Dictionary<string, object?>
                {
                    ["Faction"] = FactionName(faction),
                    ["Level"] = Localization.Localization.Localize($"RelationLevel_{level}"),
                }));
        }

        private static string FactionName(Fractions faction) => Localization.Localization.Localize($"Fraction_{faction}");
    }
}
