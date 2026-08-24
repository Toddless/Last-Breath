namespace Core.Views.UI
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Battle.Abilities;
    using Enums;
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

        /// <summary>The placeholder both templates put their number in.</summary>
        public const string ValuePlaceholder = "Value";

        /// <summary>The placeholder <see cref="CostKey"/> names the resource in.</summary>
        public const string ResourcePlaceholder = "Resource";

        /// <summary>What separates two readings sharing one line.</summary>
        private const string MetaSeparator = " · ";

        /// <summary>How a wait is written: the real number, and the fraction of a turn only where there is
        /// one. An augment that shaves a fifth off a cooldown has to be visible in the line the player
        /// bought it for.</summary>
        private const string TurnsFormat = "0.#";

        /// <summary>
        /// The tags an ability's card does not print. They are true tags and the fitting rule reads them
        /// exactly as it reads the rest — this is a rule about a SENTENCE, not about the system: an axis
        /// almost every cast stands on says nothing about what this cast is, and four such words in front
        /// of the ones that do is how a reader stops reading the line at all.
        /// <para>Cost and cooldown are the pure case (twenty-one of the twenty-five abilities), scale is
        /// the coefficient every measured cast carries, and the effect umbrella always rides beside the
        /// genus already printed next to it. An augment's card inherits none of this: there the mechanical
        /// axis is the whole answer to "where does this thing fit".</para>
        /// </summary>
        public static readonly IReadOnlySet<string> UnprintedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AbilityTags.Cost, AbilityTags.Cooldown, AbilityTags.Scale, AbilityTags.Effect,
        };

        /// <summary>The whole card of one ability, read off the answer the socket sheet gives — the one
        /// place cost, cooldown, tags and the live description are decided.</summary>
        public static AbilityCard Card(AbilitySocketRowView view) =>
            new(view.DisplayName, MetaLine(view.Cost, view.Cooldown), TagText.Line(Printed(view.Tags)), view.Description);

        /// <summary>The cast price, worded.</summary>
        public static string CostLine(int value, Costs resource) =>
            Localization.Render(CostKey, new Dictionary<string, object?>
            {
                [ValuePlaceholder] = value,
                [ResourcePlaceholder] = Localization.Localize(resource.ToString()),
            });

        /// <summary>The wait between casts, worded — and nothing at all for a cast that has none. "Cooldown:
        /// 0 turns" is a line about a rule the ability does not obey. The number goes in already written,
        /// fraction and all: rounded to whole turns, a cooldown cut by a fifth would read as the cooldown
        /// the player had before he paid for the cut.</summary>
        public static string CooldownLine(float turns) =>
            turns <= 0f
                ? string.Empty
                : Localization.Render(CooldownKey, new Dictionary<string, object?>
                {
                    [ValuePlaceholder] = turns.ToString(TurnsFormat, CultureInfo.InvariantCulture),
                });

        /// <summary>The tags this card has anything to say with — see <see cref="UnprintedTags"/>. The list
        /// itself is untouched: what is filtered is one sentence on one screen.</summary>
        private static IReadOnlyList<string> Printed(IReadOnlyList<string>? tags) =>
            tags is not { Count: > 0 } ? [] : [.. tags.Where(tag => !UnprintedTags.Contains(tag))];

        /// <summary>Price and wait as the one line a surface has room for. A missing half leaves no
        /// separator dangling behind it.</summary>
        public static string MetaLine(string cost, string cooldown) =>
            string.IsNullOrEmpty(cost) || string.IsNullOrEmpty(cooldown)
                ? $"{cost}{cooldown}"
                : $"{cost}{MetaSeparator}{cooldown}";
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
