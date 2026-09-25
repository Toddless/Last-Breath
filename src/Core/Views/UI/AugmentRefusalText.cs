namespace Core.Views.UI
{
    using Battle.Abilities;

    /// <summary>
    /// Why an augment did not move, in one localisation key — both directions of the journey, because
    /// both are the same job: a refusal named by the gate that answered. The interface's own table and
    /// only its own: a player reads a short line under the cursor or in a toast, while the developer
    /// console answers the same question in prose with the commands to try next. Two audiences, two
    /// vocabularies — what they must not have is two readings of the RULE, and neither has one: both
    /// are handed the verdict the gate produced.
    /// </summary>
    public static class AugmentRefusalText
    {
        private const string InstallPrefix = "UI_Augment_Refused_";

        private const string ExtractPrefix = "UI_Augment_Extract_";

        /// <summary>The key naming the reason. The fitting rule's own verdict wins where the rule is
        /// what answered — the outcome only says that it did — so the two enums never need a shared
        /// vocabulary between them.</summary>
        public static string KeyFor(AugmentInstallResult result) =>
            result.Outcome == AugmentInstallOutcome.DoesNotFit
                ? $"{InstallPrefix}{result.Fit?.ToString() ?? "UnknownRecord"}"
                : KeyFor(result.Outcome);

        /// <summary>The same wording named rather than answered, for the screens that state a slot's
        /// standing before anything is dropped on it — the card of a slot that can only be emptied says
        /// what the gate would say to a drop on it. Named out of this table and not beside it: two
        /// wordings of one fact are how they drift apart.</summary>
        public static string KeyFor(AugmentInstallOutcome outcome) => $"{InstallPrefix}{outcome}";

        /// <summary>The same for the way back out. Its own prefix rather than a shared one: an
        /// extraction fails for reasons an install has no word for — the bag is full — and one table of
        /// wordings for both would leave the player reading about slots when the trouble is his pack.</summary>
        public static string KeyFor(AugmentExtractResult result) => $"{ExtractPrefix}{result}";
    }
}
