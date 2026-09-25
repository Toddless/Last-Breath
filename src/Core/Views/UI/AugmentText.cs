namespace Core.Views.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Enums;
    using Godot;
    using Localization;

    /// <summary>
    /// The wording keys of the augment screens that no rule produces — the ones a window writes because
    /// it is a window. A refusal is not among them: that one is built from the gate's own verdict
    /// (<see cref="AugmentRefusalText"/>), and a second table of reasons beside the rule would be a
    /// second reading of it.
    /// <para>
    /// The CARD is assembled here for the same reason. One augment is shown in three places — carried in
    /// the tray, offered by the picker an empty slot opens, and seated in a socket cell — and three
    /// assemblies of one card is how the same copy ends up reading three different ways.
    /// </para>
    /// </summary>
    public static class AugmentText
    {
        /// <summary>The first tier there is. Anything below it is an unknown tier and not a small one.</summary>
        private const int LowestTier = 1;

        /// <summary>The tier line of an augment's tooltip. Templated — the value goes in under
        /// <see cref="TierValue"/>.</summary>
        public const string Tier = "UI_Augment_Tier";

        /// <summary>The placeholder <see cref="Tier"/> is filled by.</summary>
        public const string TierValue = "Value";

        /// <summary>Where a record written for ONE ability sits, worded — the ability's name goes in
        /// under <see cref="NameValue"/>.</summary>
        public const string Fits = "UI_Augment_Fits";

        /// <summary>Where a record claiming every ability sits, worded.</summary>
        public const string FitsAny = "UI_Augment_Fits_Any";

        /// <summary>The placeholder <see cref="Fits"/> names the ability in.</summary>
        public const string NameValue = "Name";

        /// <summary>Title of the picker an empty slot opens.</summary>
        public const string PickTitle = "UI_Augment_Pick_Title";

        /// <summary>Said instead of an empty picker: the bag holds nothing this slot would take.</summary>
        public const string NothingFits = "UI_Augment_Pick_Nothing";

        /// <summary>Said of a seated augment every move of which lost to a stronger one.</summary>
        public const string Dormant = "UI_Augment_Dormant";

        /// <summary>Said of a seated augment some of whose moves lost.</summary>
        public const string PartlyDormant = "UI_Augment_Partly";

        /// <summary>A carried copy: its own name, its own tier, where it may sit, and what it does in the
        /// numbers it rolled.</summary>
        public static AugmentCard Card(AugmentTrayTileView tile) =>
            new(
                tile.DisplayName,
                TierLine(tile.Tier),
                FitLine(tile.Tags, tile.AbilityId, tile.FitsAnyAbility),
                tile.Description,
                RarityColor(tile.Rarity));

        /// <summary>
        /// The same card for a copy already in a slot, plus what the SLOT makes of it: a card printing
        /// numbers the player is not getting — because every move of the copy lost to a stronger augment,
        /// or because the slot is closed and hands nothing out until it is emptied — describes an augment
        /// he does not have.
        /// <para>The tier printed is the AUGMENT's and never the slot's — the slot's is the badge on the
        /// cell — so the copy reads in the socket exactly as it read in the bag.</para>
        /// </summary>
        public static AugmentCard Card(AugmentCellView cell) =>
            new(
                cell.DisplayName,
                TierLine(cell.AugmentTier),
                FitLine(cell.Tags, cell.AbilityId, cell.FitsAnyAbility),
                Body(cell),
                RarityColor(cell.Rarity));

        /// <summary>
        /// Where the record declares it may sit, read in the order the fitting rule reads the same
        /// declaration: a claim on every ability, then a named ability, then the tags. Exactly ONE of the
        /// three is printed because exactly one of them decides — a record naming an ability is seated by
        /// that name alone, and the tags it happens to carry beside it would promise a family it never
        /// reaches. Nothing is judged here: the card reads out a declaration, it does not measure a slot.
        /// <para>The tags go out whole. The mechanical axes an ability's card leaves unsaid (cost,
        /// cooldown, scale) are the whole answer to "where does this augment go", so the sentence rule of
        /// <see cref="AbilityText.UnprintedTags"/> does not reach this line.</para>
        /// </summary>
        public static string FitLine(IReadOnlyList<string>? tags, string abilityId, bool fitsAnyAbility)
        {
            if (fitsAnyAbility) return Localization.Localize(FitsAny);

            return string.IsNullOrWhiteSpace(abilityId)
                ? TagText.Line(tags)
                : Localization.Render(Fits, new Dictionary<string, object?>
                {
                    [NameValue] = Localization.Localize(abilityId),
                });
        }

        /// <summary>The tier, worded — and nothing at all where there is no tier to word. Tiers start at
        /// one, so a zero is a composition that supplies no records rather than an augment of tier zero,
        /// and "Tier 0" would be a number the catalog never wrote.</summary>
        public static string TierLine(int tier) =>
            tier < LowestTier
                ? string.Empty
                : Localization.Render(Tier, new Dictionary<string, object?> { [TierValue] = tier });

        private static Color RarityColor(Rarity rarity) => Color.FromHtml(TextPalette.RarityColor(rarity));

        private static string Body(AugmentCellView cell) =>
            StateLine(cell) is { Length: > 0 } state ? $"{cell.Description}\n{state}" : cell.Description;

        /// <summary>
        /// Why the numbers above are not reaching the player, where they are not. Two ways they do not,
        /// and both are read off an answer somebody else gave: a slot the allocation no longer backs is
        /// named in the gate's own words, and a copy beaten on every parameter it moves is named by the
        /// verdict the ability reached (<see cref="AugmentActivity"/>). Nothing is worked out here — which
        /// of two augments wins is decided in one place, and this is a reading of it.
        /// </summary>
        private static string StateLine(AugmentCellView cell) => cell.Kind switch
        {
            AugmentCellKind.Held => Warning(AugmentRefusalText.KeyFor(AugmentInstallOutcome.SocketClosed)),
            AugmentCellKind.Filled => ActivityLine(cell.Activity),
            _ => string.Empty,
        };

        private static string ActivityLine(AugmentActivity activity) => activity switch
        {
            AugmentActivity.Dormant => Warning(Dormant),
            AugmentActivity.Partly => Warning(PartlyDormant),
            _ => string.Empty,
        };

        private static string Warning(string key) =>
            TextPalette.Colorize(Localization.Localize(key), TextPalette.Debuff);
    }

    /// <summary>
    /// One augment as a screen shows it: the copy's own name in its rarity's colour, its tier, where it
    /// may sit, and what it does in the numbers it rolled. Text only — every surface draws it with its
    /// own controls, and none of them assembles it.
    /// </summary>
    /// <param name="Name">The copy's name.</param>
    /// <param name="TierLine">The tier, worded.</param>
    /// <param name="FitLine">Where the record declares it may sit — the ability it was written for, the
    /// claim on every ability, or the tags an ability has to share with it.</param>
    /// <param name="Description">What this copy does, in its own numbers.</param>
    /// <param name="RarityColor">What the name is painted in.</param>
    public readonly record struct AugmentCard(
        string Name,
        string TierLine,
        string FitLine,
        string Description,
        Color RarityColor)
    {
        /// <summary>Everything under the name as one stretch of text, for a surface carrying a single
        /// text field — a picker row — rather than a title and a body of its own. A missing part leaves
        /// no blank line behind it.</summary>
        public string Body => Join(TierLine, FitLine, Description);

        /// <summary>The same without the tier, for a surface already showing that as its own subtitle —
        /// a hover tooltip, which prints the tier under the name.</summary>
        public string Details => Join(FitLine, Description);

        private static string Join(params string[] parts) =>
            string.Join('\n', parts.Where(part => !string.IsNullOrEmpty(part)));
    }
}
