namespace Core.Narrative.Actions
{
    using Data.GameData;
    using Events;
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
        private const string TypeName = "Deed";
        private const string DeedIdKey = "deedId";

        private static readonly NarrativeRecordSpec s_parameters = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Text(DeedIdKey, required: true, DataCatalog.ReputationDeeds));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string deedId = json.Value<string>(DeedIdKey) ?? string.Empty;
            if (deedId.Length == 0)
            {
                Tracker.TrackError($"{TypeName} action: {DeedIdKey} is missing");
                return null;
            }

            return new PublishDeedAction(events, playerAccessor, deedId);
        }
    }
}
