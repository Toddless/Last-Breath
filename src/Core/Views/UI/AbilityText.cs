namespace Core.Views.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Enums;
    using Godot;
    using Localization;

    /// <summary>
    /// One ability as a screen prints it. The same cast is shown in three places — the row of the socket
    /// sheet, the button of the battle bar and the popup of its node on the wheel — and three assemblies
    /// of one card is how the same cast ends up reading three different ways.
    /// <para>The price and the wait are worded HERE and nowhere else, which is what lets a surface holding
    /// a live ability (the battle bar) print the very line a surface holding only the sheet's answer
    /// prints.</para>
    /// </summary>
    public static class AbilityText
    {
        /// <summary>The cast price, templated.</summary>
        public const string CostKey = "UI_AbilityCost";

        /// <summary>The wait between casts, templated.</summary>
        public const string CooldownKey = "UI_AbilityCooldown";

        /// <summary>The cast price as a bare value ("30 Mana") — the wording of a card that already
        /// carries its own "cost" caption and must not say the word twice.</summary>
        public const string CostValueKey = "UI_AbilityCostValue";

        /// <summary>The wait as a bare value ("3 turns") — the twin of <see cref="CostValueKey"/> for a
        /// card whose caption already says "cooldown".</summary>
        public const string CooldownValueKey = "UI_AbilityCooldownValue";

        /// <summary>The placeholder both templates put their number in.</summary>
        public const string ValuePlaceholder = "Value";

        /// <summary>The placeholder <see cref="CostKey"/> names the resource in.</summary>
        public const string ResourcePlaceholder = "Resource";

        /// <summary>What separates two readings sharing one line.</summary>
        private const string MetaSeparator = " · ";

        /// <summary>
        /// The tags an ability's card does not print. They are true tags and the fitting rule reads them
        /// exactly as it reads the rest — this is a rule about a SENTENCE, not about the system: an axis
        /// almost every cast stands on says nothing about what this cast is, and a row of such words in
        /// front of the ones that do is how a reader stops reading the line at all.
        /// <para>Cost and cooldown are the pure case (twenty-one of the twenty-five abilities), scale is
        /// the coefficient every measured cast carries, activation names how a cast is reached rather than
        /// what it does, and the effect umbrella always rides beside the genus already printed next to it.
        /// An augment's card inherits none of this: there the mechanical axis is the whole answer to
        /// "where does this thing fit".</para>
        /// </summary>
        public static readonly IReadOnlySet<string> UnprintedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AbilityTags.Cost, AbilityTags.Cooldown, AbilityTags.Scale, AbilityTags.Effect, AbilityTags.Activation,
        };

        /// <summary>The two genus tags a card's subtitle is allowed to read as "what this cast IS", in
        /// the order they win when an ability carries both.</summary>
        private static readonly string[] s_kindTags = [AbilityTags.Spell, AbilityTags.Attack];

        /// <summary>The whole card of one ability, read off the answer the socket sheet gives — the one
        /// place cost, cooldown, tags and the live description are decided.</summary>
        public static AbilityCard Card(AbilitySocketRowView view) =>
            new(view.DisplayName, MetaLine(view.Cost, view.Cooldown), TagText.Line(Printed(view.Tags)), view.Description)
            {
                Kind = KindOf(view.Tags),
                Target = view.Target,
                CostValue = view.CostValue,
                CostPool = view.CostPool,
                CooldownTurns = view.CooldownTurns,
                Tags = TagText.Words(Printed(view.Tags)),
                Icon = view.Icon,
            };

        /// <summary>The same card read off a LIVE ability — the battle bar holds the instance itself and
        /// has no sheet answer to go through. The two assemblies word every field through the same lines,
        /// so the bar's tooltip and the sheet's cannot drift apart.</summary>
        /// <param name="target">Whom the cast lands on, already worded — the wording knows the concrete
        /// targeting strategies, which live beside them and not here. Empty hides the line.</param>
        public static AbilityCard Card(IAbility ability, string target = "") =>
            new(ability.DisplayName,
                MetaLine(CostLine(ability.CostValue, ability.CostType), CooldownLine(ability.Cooldown)),
                TagText.Line(Printed(ability.Tags)),
                ability.Description)
            {
                Kind = KindOf(ability.Tags),
                Target = target,
                CostValue = ability.CostValue,
                CostPool = ability.CostType,
                CooldownTurns = ability.Cooldown,
                Tags = TagText.Words(Printed(ability.Tags)),
                Icon = ability.Icon,
            };

        /// <summary>What the cast IS, in one word — the genus the card prints as its subtitle. Read off
        /// the tags rather than declared anew: an ability already says whether it is a spell or an
        /// attack, and a second declaration would be free to disagree. Scanned in the author's tag order
        /// so an ability carrying both genera is what its author put first; nothing at all for a cast
        /// that is neither, and the subtitle then hides.</summary>
        public static string KindOf(IReadOnlyList<string>? tags)
        {
            if (tags is not { Count: > 0 }) return string.Empty;

            foreach (string tag in tags)
                foreach (string kind in s_kindTags)
                    if (string.Equals(tag, kind, StringComparison.OrdinalIgnoreCase))
                        return Localization.Localize(TagText.KeyOf(kind));

            return string.Empty;
        }

        /// <summary>The cast price, worded.</summary>
        public static string CostLine(int value, Costs resource) =>
            Localization.Render(CostKey, new Dictionary<string, object?>
            {
                [ValuePlaceholder] = value,
                [ResourcePlaceholder] = Localization.Localize(resource.ToString()),
            });

        /// <summary>The wait between casts, worded — and nothing at all for a cast that has none, because
        /// "Cooldown: 0 turns" is a line about a rule the ability does not obey. The number goes in raw and
        /// the template counts turns with it: every road that moves a cooldown moves it by whole turns, so
        /// there is no fraction here for the counting to lose.</summary>
        public static string CooldownLine(float turns) =>
            turns <= 0f
                ? string.Empty
                : Localization.Render(CooldownKey, new Dictionary<string, object?>
                {
                    [ValuePlaceholder] = turns,
                });

        /// <summary>The cast price as a bare value ("30 Mana"), for the card that captions the line
        /// itself.</summary>
        public static string CostValueText(int value, Costs resource) =>
            Localization.Render(CostValueKey, new Dictionary<string, object?>
            {
                [ValuePlaceholder] = value,
                [ResourcePlaceholder] = Localization.Localize(resource.ToString()),
            });

        /// <summary>The wait as a bare value ("3 turns"), for the card that captions the line itself —
        /// and nothing for a cast that has none, same as <see cref="CooldownLine"/>.</summary>
        public static string CooldownValueText(float turns) =>
            turns <= 0f
                ? string.Empty
                : Localization.Render(CooldownValueKey, new Dictionary<string, object?>
                {
                    [ValuePlaceholder] = turns,
                });

        /// <summary>Price and wait as the one line a surface has room for. A missing half leaves no
        /// separator dangling behind it.</summary>
        public static string MetaLine(string cost, string cooldown) =>
            string.IsNullOrEmpty(cost) || string.IsNullOrEmpty(cooldown)
                ? $"{cost}{cooldown}"
                : $"{cost}{MetaSeparator}{cooldown}";

        /// <summary>The tags this card has anything to say with — see <see cref="UnprintedTags"/>. The list
        /// itself is untouched: what is filtered is one sentence on one screen.</summary>
        private static IReadOnlyList<string> Printed(IReadOnlyList<string>? tags) =>
            tags is not { Count: > 0 } ? [] : [.. tags.Where(tag => !UnprintedTags.Contains(tag))];

    }

    /// <summary>
    /// A list of tags in words. Tags are the compatibility axis of the whole augment system — an augment
    /// fits an ability sharing one — so both sides of that rule get read out to the player, which is why
    /// this knows about neither of them and takes the bare list.
    /// </summary>
    public static class TagText
    {
        /// <summary>What a tag's wording is filed under: the vocabulary's own spelling of the tag,
        /// prefixed. Derived rather than tabled — a table beside the vocabulary would be a second list of
        /// tags, free to fall behind it.</summary>
        public const string KeyPrefix = "Tag_";

        private const string Separator = ", ";

        /// <summary>The key one tag is worded under.</summary>
        public static string KeyOf(string tag) => KeyPrefix + tag;

        /// <summary>The tags, worded and in the order they were given — the author's order carries what
        /// the alphabet does not. Nothing at all where there are no tags.</summary>
        public static string Line(IReadOnlyList<string>? tags) =>
            tags is not { Count: > 0 }
                ? string.Empty
                : string.Join(Separator, tags.Select(tag => Localization.Localize(KeyOf(tag))));

        /// <summary>The same words as a list — for a surface drawing each tag as a plate of its own
        /// rather than gluing them into a sentence.</summary>
        public static IReadOnlyList<string> Words(IReadOnlyList<string>? tags) =>
            tags is not { Count: > 0 } ? [] : [.. tags.Select(tag => Localization.Localize(KeyOf(tag)))];
    }

    /// <summary>
    /// One ability as a screen shows it: its name, what it costs and how long it makes the caster wait,
    /// what it counts as, and what it does in the numbers it is wearing right now. Text only — every
    /// surface draws it with its own controls, and none of them assembles it.
    /// </summary>
    /// <param name="Name">The ability's name.</param>
    /// <param name="MetaLine">Price and wait, on one line.</param>
    /// <param name="TagsLine">What the cast counts as, worded.</param>
    /// <param name="Description">What it does, with everything seated on it already counted in.</param>
    public readonly record struct AbilityCard(string Name, string MetaLine, string TagsLine, string Description)
    {
        /// <summary>What the cast IS in one word ("Spell", "Attack"), for the subtitle under the name.
        /// Empty where the tags name no genus — the subtitle then hides. The glued lines above do not
        /// carry it and did not change.</summary>
        public string Kind { get; init; } = string.Empty;

        /// <summary>Whom the cast lands on, worded. Empty where nobody worded it (a surface built off
        /// the sheet's answer before the answer learned targets, or a strategy the wording does not
        /// know), and the line then hides.</summary>
        public string Target { get; init; } = string.Empty;

        /// <summary>The bare cast price. Meaningless without <see cref="CostPool"/> — a card with no
        /// pool has no price to print.</summary>
        public int CostValue { get; init; } = 0;

        /// <summary>What the price is paid in, or null for a card that knows no price (an unowned row
        /// with no instance behind it). The pool is what tints the printed value.</summary>
        public Costs? CostPool { get; init; } = null;

        /// <summary>The wait between casts in whole turns; zero for a cast that has none, and the line
        /// then hides.</summary>
        public float CooldownTurns { get; init; } = 0f;

        /// <summary>The printed tags as worded WORDS, one per plate — the same set
        /// <see cref="TagsLine"/> glues into its sentence.</summary>
        public IReadOnlyList<string> Tags { get; init; } = [];

        /// <summary>The staged readings of a cast that escalates by stages, one line per stage, in
        /// stage order. Empty until the domain words stages structurally — no ability does today, and
        /// the block then hides.</summary>
        public IReadOnlyList<string> Tiers { get; init; } = [];

        /// <summary>The ability's art; null where it has none — the frame is drawn either way.</summary>
        public Texture2D? Icon { get; init; } = null;

        /// <summary>Everything under the name as one stretch of text, for a surface carrying a single text
        /// field — the popup of a node on the wheel, whose subtitle is already spoken for. A missing part
        /// leaves no blank line behind it.</summary>
        public string Body => Join(MetaLine, TagsLine, Description);

        /// <summary>The same without the price and the wait, for a surface already showing those as its
        /// own subtitle — the row of the socket sheet, which prints the meta beside the name.</summary>
        public string Details => Join(TagsLine, Description);

        private static string Join(params string[] parts) =>
            string.Join('\n', parts.Where(part => !string.IsNullOrEmpty(part)));
    }
}
