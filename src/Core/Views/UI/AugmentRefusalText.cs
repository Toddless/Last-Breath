namespace Core.Views.UI
{
    using Battle.Abilities;

    /// <summary>
    /// Why an augment did not go into a slot, in one localisation key. The interface's own table and
    /// only its own: a player reads a short line under the cursor or in a toast, while the developer
    /// console answers the same question in prose with the commands to try next. Two audiences, two
    /// vocabularies — what they must not have is two readings of the RULE, and neither has one: both
    /// are handed the verdict the gate produced.
    /// </summary>
    public static class AugmentRefusalText
    {
        private const string Prefix = "UI_Augment_Refused_";

        /// <summary>The key naming the reason. The fitting rule's own verdict wins where the rule is
        /// what answered — the outcome only says that it did — so the two enums never need a shared
        /// vocabulary between them.</summary>
        public static string KeyFor(AugmentInstallResult result) =>
            result.Outcome == AugmentInstallOutcome.DoesNotFit
                ? $"{Prefix}{result.Fit?.ToString() ?? "UnknownRecord"}"
                : $"{Prefix}{result.Outcome}";
    }
}
