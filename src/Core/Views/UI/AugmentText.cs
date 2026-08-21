namespace Core.Views.UI
{
    /// <summary>
    /// The wording keys of the augment screens that no rule produces — the ones a window writes because
    /// it is a window. A refusal is not among them: that one is built from the gate's own verdict
    /// (<see cref="AugmentRefusalText"/>), and a second table of reasons beside the rule would be a
    /// second reading of it.
    /// </summary>
    public static class AugmentText
    {
        /// <summary>The tier line of an augment's tooltip. Templated — the value goes in under
        /// <see cref="TierValue"/>.</summary>
        public const string Tier = "UI_Augment_Tier";

        /// <summary>The placeholder <see cref="Tier"/> is filled by.</summary>
        public const string TierValue = "Value";

        /// <summary>Title of the picker an empty slot opens.</summary>
        public const string PickTitle = "UI_Augment_Pick_Title";

        /// <summary>Said instead of an empty picker: the bag holds nothing this slot would take.</summary>
        public const string NothingFits = "UI_Augment_Pick_Nothing";
    }
}
