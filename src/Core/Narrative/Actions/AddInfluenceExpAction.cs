namespace Core.Narrative.Actions
{
    using Influence;
    using Newtonsoft.Json.Linq;

    public class AddInfluenceExpAction(IInfluenceMastery mastery, int amount) : INarrativeAction
    {
        public void Execute(NarrativeContext context) => mastery.AddExperience(amount);
    }

    public class AddInfluenceExpActionFactory(IInfluenceMastery mastery) : INarrativeActionFactory
    {
        private const string TypeName = "AddInfluenceExp";
        private const string AmountKey = "amount";

        private static readonly NarrativeRecordSpec s_parameters = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Integer(AmountKey, required: true));

        public string Type => TypeName;

        /// <summary>Only a positive amount is read: a missing one and a zero are the same entry, one
        /// that hands out nothing.</summary>
        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            int amount = json.Value<int?>(AmountKey) ?? 0;
            if (amount > 0) return new AddInfluenceExpAction(mastery, amount);

            Tracker.TrackError($"{TypeName} action: {AmountKey} must be positive");
            return null;
        }
    }
}
