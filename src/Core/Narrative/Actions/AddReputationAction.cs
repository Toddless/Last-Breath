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
        private const string TypeName = "AddReputation";
        private const string FactionKey = "faction";
        private const string DeltaKey = "delta";
        private const string ReasonKey = "reason";
        private const string DefaultReason = "Narrative";

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Enum<Fractions>(FactionKey),
            NarrativeParameterSchema.Integer(DeltaKey, required: true),
            NarrativeParameterSchema.Text(ReasonKey, DefaultReason));

        public string Type => TypeName;

        /// <summary>A delta of zero is refused along with an absent one: an entry that moves nothing is a
        /// number left unwritten, not a reward of none.</summary>
        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            int delta = json.Value<int?>(DeltaKey) ?? 0;
            if (delta == 0)
            {
                Tracker.TrackError($"{TypeName} action: {DeltaKey} is missing or zero");
                return null;
            }

            return new AddReputationAction(relations,
                EnumParser.ParseEnum<Fractions>(json.Value<string>(FactionKey) ?? string.Empty),
                delta,
                json.Value<string>(ReasonKey) ?? DefaultReason);
        }
    }
}
