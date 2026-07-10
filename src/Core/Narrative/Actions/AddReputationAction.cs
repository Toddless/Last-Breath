namespace Core.Narrative.Actions
{
    using Data;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Direct faction reputation delta, bypassing the deed pipeline on purpose: quest rewards
    /// must not decay with repetition and need no witness. The reason lands in
    /// ReputationChangedArgs (and is not DirectSet, so the toast still fires).
    /// </summary>
    public class AddReputationAction(IFactionRelationService relations, Fractions faction, int delta, string reason) : INarrativeAction
    {
        public void Execute(NarrativeContext context) => relations.AddReputation(faction, delta, reason);
    }

    public class AddReputationActionFactory(IFactionRelationService relations) : INarrativeActionFactory
    {
        public string Type => "AddReputation";

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            int delta = json.Value<int?>("delta") ?? 0;
            if (delta == 0)
            {
                Tracker.TrackError("AddReputation action: delta is missing or zero");
                return null;
            }

            return new AddReputationAction(relations,
                EnumParser.ParseEnum<Fractions>(json.Value<string>("faction") ?? string.Empty),
                delta,
                json.Value<string>("reason") ?? "Narrative");
        }
    }
}
