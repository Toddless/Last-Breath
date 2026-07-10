namespace Core.Narrative.Actions
{
    using Events;
    using Events.GameEvents;
    using Godot;
    using Newtonsoft.Json.Linq;
    using Services;

    /// <summary>
    /// Relation effects of a dialogue choice go through the deed pipeline — personal reputation,
    /// repeat decay (no flattery farming) and notifications come with it. The interlocutor is the
    /// deed's target; dialogue deeds should carry requiresWitness: false in ReputationDeeds.json.
    /// </summary>
    public class PublishDeedAction(IGameEventBus events, IPlayerAccessor playerAccessor, string deedId) : INarrativeAction
    {
        public void Execute(NarrativeContext context)
        {
            if (context.NpcFaction is not { } faction)
            {
                Tracker.TrackNull($"Deed action '{deedId}': no NPC in the narrative context");
                return;
            }

            Vector2 position = playerAccessor.Player is Node2D node ? node.GlobalPosition : Vector2.Zero;
            events.Publish(new PlayerDeedEvent(deedId, faction, context.NpcInstanceId, position));
        }
    }

    public class PublishDeedActionFactory(IGameEventBus events, IPlayerAccessor playerAccessor) : INarrativeActionFactory
    {
        public string Type => "Deed";

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string deedId = json.Value<string>("deedId") ?? string.Empty;
            if (deedId.Length == 0)
            {
                Tracker.TrackError("Deed action: deedId is missing");
                return null;
            }

            return new PublishDeedAction(events, playerAccessor, deedId);
        }
    }
}
