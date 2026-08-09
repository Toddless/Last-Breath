namespace Core.Narrative.Actions
{
    using Battle;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Pays a quest reward in passive tree POINTS. Not in experience: experience buys a level, and a
    /// level costs more the further along the curve the character stands — the same trial would hand
    /// a novice a budget it would never hand a master. A point is a point at any level.
    /// <para>
    /// The entry names its own amount and nothing else knows a total: a reward ladder is worth
    /// exactly what its stages add up to, so the sum lives in the quest data and never in code.
    /// The points reach the wheel the moment they are granted, by the same road a level up takes.
    /// </para>
    /// <para>
    /// Nothing here is idempotent, exactly like every other narrative action: what keeps the reward
    /// to a single payment is the quest being unrepeatable, not the grant counting its callers.
    /// </para>
    /// </summary>
    public class GrantTreePointsAction(IMartialArtMastery mastery, int amount) : INarrativeAction
    {
        public void Execute(NarrativeContext context) => mastery.AddBonusPoints(amount);
    }

    public class GrantTreePointsActionFactory(IMartialArtMastery mastery) : INarrativeActionFactory
    {
        public string Type => "GrantTreePoints";

        /// <summary>The amount is read as a whole number and nothing else: a quoted or fractional
        /// amount is a typo in an entry that hands out a budget, and a budget silently rounded down
        /// (or converted from text) is a reward nobody can trace back to the file.</summary>
        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            if (json["amount"] is not JValue { Type: JTokenType.Integer } token)
            {
                Tracker.TrackError("GrantTreePoints action: amount is missing or is not a whole number of points");
                return null;
            }

            int amount = token.Value<int>();
            if (amount > 0) return new GrantTreePointsAction(mastery, amount);

            Tracker.TrackError($"GrantTreePoints action: amount must be positive but is {amount}");
            return null;
        }
    }
}
