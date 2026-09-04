namespace LastBreath.Descriptors.Preview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Localization;
    using Core.Modifiers;
    using Core.Views.UI;
    using Newtonsoft.Json;
    using Tooling.Catalogs;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>
    /// What a record of the data would read as in the game, built by the game's own readers, minters and
    /// formatters over the documents and the .po files a tool has OPEN. A number retyped a keystroke ago
    /// and a description retyped in the block beside it both reach the next preview: the whole point is
    /// that the author reads what he is writing instead of guessing at it and starting the game.
    /// <para>Nothing here decides how anything is worded. Every line comes out of the class the game
    /// words it with — a second wording beside the first is how the same item ends up reading two ways,
    /// which is exactly the drift this panel exists to catch.</para>
    /// </summary>
    public sealed class TooltipPreview
    {
        /// <summary>The draw of every preview. Fixed rather than random: the author reads a template
        /// twice while editing it, and two different sets of rolled lines would read as his edit having
        /// done something it did not.</summary>
        public const int Seed = 20260902;

        private const string UnknownCatalogFormat = "'{0}' is a catalog this panel does not read";

        private const string NoRecordText = "the record carries no id yet";

        private const string NoBlueprintFormat = "'{0}' has no equip template the reader could take";

        private const string NoItemFormat = "'{0}' is not a record this catalog's reader kept";

        private const string NoAugmentFormat = "'{0}' is neither an augment nor an ability of this catalog";

        private const string NoCanonFormat = "'{0}' has no canonical row, so the line prints what it can";

        private const string BuildFailedFormat = "the preview could not be built: {0}";

        /// <summary>Said under an ability's own line: the numbers a cast actually carries are decorated
        /// by the ability book, which is built by the battle module and is not here.</summary>
        private const string AbilityNoteText = "the record's own numbers; a live cast is read through its book";

        /// <summary>Said under an item's granted behaviour: what a grant DOES is built by the effect
        /// registry of the battle module, so a preview names the grant and cannot word it.</summary>
        private const string GrantNoteText = "granted behaviours are worded by the battle module and stay unworded here";

        /// <summary>Catalogs this panel can read a record of. Asked before a record is drawn so a
        /// catalog nothing previews shows no panel at all rather than a note per record.</summary>
        public static readonly IReadOnlyList<string> Catalogs =
        [
            DataCatalog.EquipItems, DataCatalog.Abilities, DataCatalog.Effects,
            DataCatalog.Resources, DataCatalog.Recipes,
        ];

        /// <summary>The catalogs the item reader is fed — everything an equip template's lines, pools
        /// and costs are resolved out of. The legacy Items catalog is deliberately not among them.</summary>
        private static readonly string[] s_itemCatalogs =
        [
            DataCatalog.EquipItems, DataCatalog.Recipes, DataCatalog.Resources,
            DataCatalog.ModifierPools, DataCatalog.UpgradeCosts,
        ];

        private readonly ItemDataProvider _items;
        private readonly ParameterFormatProvider _formats;
        private readonly EffectCanonCatalog _canon;
        private readonly AbilityAugmentCatalog _abilities;
        private readonly ILocalizationService _localization;
        private readonly PreviewItemFactory _factory = new();
        private readonly List<string> _notes = [];

        private TooltipPreview(CatalogWorkspace workspace, LocalizedTexts? texts)
        {
            Wording = new PreviewWording(texts);

            _formats = new ParameterFormatProvider();
            _canon = new EffectCanonCatalog();
            _abilities = new AbilityAugmentCatalog();
            _items = new ItemDataProvider(new DataParser(_factory), s_itemCatalogs);

            var modifiers = new ModifierFormatter(Wording, _formats);
            var contexts = new ContextModifierFormatter(Wording);

            _localization = new LocalizationService(Wording, modifiers, contexts,
            [
                new ModifierTextFormatter(modifiers),
                new StatPassiveLineTextFormatter(modifiers),
                new ContextModifierTextFormatter(contexts),
                new ModifierDescriptorTextFormatter(modifiers, contexts),
            ]);

            Read(workspace, DataCatalog.Formatting, _formats);
            Read(workspace, DataCatalog.Effects, _canon);
            Read(workspace, DataCatalog.Abilities, _abilities);

            foreach (string catalog in s_itemCatalogs) Read(workspace, catalog, _items);
        }

        /// <summary>Which locale a preview is read in, and the .po files it is read out of.</summary>
        public PreviewWording Wording { get; }

        /// <summary>What this reading of the documents could not do — a catalog the run never opened, a
        /// file the game's own reader refused. About THIS reading and nothing else.</summary>
        public IReadOnlyList<string> Notes => _notes;

        /// <summary>Opens a preview over the documents a tool has open. Read afresh whenever the tool
        /// asks: a template edited a keystroke ago is the template the next preview mints.</summary>
        public static TooltipPreview Load(CatalogWorkspace workspace, LocalizedTexts? texts)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            return new TooltipPreview(workspace, texts);
        }

        /// <summary>Whether a record of this catalog can be previewed at all.</summary>
        public static bool Reads(string catalog) => Catalogs.Contains(catalog, StringComparer.Ordinal);

        /// <summary>
        /// One record as the game would show it. <paramref name="rarity"/> is what an equip template is
        /// minted AT — a template is not an item, and which lines it comes out with is the rarity's
        /// answer; every other catalog ignores it.
        /// </summary>
        public PreviewText Describe(string catalog, string recordId, Rarity rarity)
        {
            if (string.IsNullOrWhiteSpace(recordId)) return PreviewText.Says(NoRecordText);
            if (!Reads(catalog)) return PreviewText.Says(Text(UnknownCatalogFormat, catalog));

            // The formatters reach the wording through the static facade, which is what the game's own
            // line builders read: pinning it here is how a preview in ru is a preview of ru.
            Localization.Override(_localization);

            try
            {
                return Card(catalog, recordId, rarity);
            }
            catch (Exception failure) when (failure is ArgumentException or InvalidOperationException
                                               or NotSupportedException or FormatException or JsonException)
            {
                return PreviewText.Says(Text(BuildFailedFormat, failure.Message));
            }
        }

        private PreviewText Card(string catalog, string recordId, Rarity rarity) => catalog switch
        {
            DataCatalog.EquipItems => Equip(recordId, rarity),
            DataCatalog.Abilities => Ability(recordId),
            DataCatalog.Effects => Effect(recordId),
            _ => Item(recordId),
        };

        /// <summary>An equip template as a piece of that rarity: minted the way every equip is born, then
        /// given the affix lines a drop of that rarity rolls. Both halves are the game's own — the mint
        /// rolls the authored ranges, the roll draws from the same union of pools a drop draws from.</summary>
        private PreviewText Equip(string recordId, Rarity rarity)
        {
            if (_items.GetBlueprint(recordId) is null) return PreviewText.Says(Text(NoBlueprintFormat, recordId));

            var rnd = new DefaultRandomNumberGenerator(Seed);
            var materializer = new ModifierMaterializer(rnd);
            var minter = new EquipItemMinter(_items, _factory, Grants(), materializer, rnd);

            IEquipItem item = minter.Mint(recordId);

            RollLines(item, rarity, materializer, rnd);

            ItemCardText card = ItemTooltipText.Card(item, _formats);
            List<string> lines = [.. card.Lines];

            if (item.Grants.Count > 0) lines.Add(GrantNoteText);

            return new PreviewText(card.Title, lines);
        }

        /// <summary>
        /// The affix lines a drop of this rarity rolls onto a freshly minted piece — slot split, pool and
        /// draw all taken from the game's own rules. No difficulty multiplier: a preview is of the
        /// template and not of the kill that would drop it.
        /// </summary>
        /// <remarks>Authored rarities are left alone for the reason the drop pipeline leaves them alone:
        /// a unique and a mythic ARE their lines, and rolling over them would describe a piece the game
        /// never makes.</remarks>
        private void RollLines(IEquipItem item, Rarity rarity, IModifierMaterializer materializer, IRandomNumberGenerator rnd)
        {
            if (item.Rarity is Rarity.Mythic or Rarity.Unique) return;

            item.Rarity = rarity;

            List<IModifierDescriptor> pool = [.. _items.GetGenerationPool(item.Id)];
            (int prefixes, int suffixes) = AffixRules.SlotsFor(rarity, rnd);
            var sink = new CollectingSink();

            foreach (IModifierDescriptor descriptor in AffixRoller.Roll(pool, prefixes, suffixes, rnd))
                materializer.Materialize(descriptor, sink, item.InstanceId);

            foreach (IModifierInstance entity in sink.Entities) item.AddAdditionalModifier(entity);
            foreach (ContextModifierEntry context in sink.Contexts) item.AddAdditionalContextModifier(context);
            foreach (IItemGrant grant in sink.Grants) item.AddGrant(grant);
        }

        /// <summary>An augment's card as every surface showing an augment assembles it; failing that, an
        /// ability's own line in the numbers it writes.</summary>
        private PreviewText Ability(string recordId)
        {
            if (_abilities.Find(recordId) is { } augment)
                return new PreviewText(Localization.Localize(recordId), Augment(augment));

            if (_abilities.FindAbility(recordId) is { } ability)
                return new PreviewText(
                    Localization.Localize(recordId),
                    [Localization.RenderDescription(recordId, AbilityValues(ability)), AbilityNoteText]);

            return PreviewText.Says(Text(NoAugmentFormat, recordId));
        }

        /// <summary>The card of one augment record, in the parts the game's own card carries: its tier,
        /// where its record declares it may sit, and what it does in the numbers it declares. Read
        /// through the assembly the screens read it through — a second reading of a tier or of a binding
        /// is how the tool ends up promising a fit the game refuses. A part the record leaves unsaid
        /// leaves no blank line behind it, the way the carried card joins its own.</summary>
        private IReadOnlyList<string> Augment(AbilityAugmentData augment) =>
        [
            .. new[]
            {
                AugmentText.TierLine(augment.Tier),
                AugmentText.FitLine(augment.Tags, augment.AbilityId, augment.FitsAnyAbility),
                Localization.RenderDescription(augment.Id, AugmentDescription.Values(augment, _canon)),
            }.Where(part => part.Length > 0)
        ];

        /// <summary>What an ability record can answer about itself: the numbers it writes, under the
        /// keys the ability book registers them as. Whatever a cast's own parameters add beyond these
        /// stays a visible placeholder — the book that would fill it is not in a tool.</summary>
        private static Dictionary<string, object?> AbilityValues(AbilityBaseData ability)
        {
            Dictionary<string, object?> values = new(StringComparer.Ordinal)
            {
                [AbilityParameter.Damage] = ability.Damage,
                [AbilityParameter.Cooldown] = ability.Cooldown,
                [AbilityParameter.CostValue] = ability.CostValue,
                [AbilityParameter.WeaponDamageScale] = ability.WeaponDamageScale,
                [AbilityParameter.SpellDamageScale] = ability.SpellDamageScale,
            };

            // The record's own properties last: a key an ability writes for itself is the ability's
            // answer, and the five above are only what every record carries whether it says so or not.
            foreach ((string key, float figure) in ability.AbilityProperties) values[key] = figure;

            return values;
        }

        /// <summary>An effect's rule text and its keyword card, both filled from the canonical row the
        /// balance file writes — the same numbers the game builds the effect with.</summary>
        private PreviewText Effect(string recordId)
        {
            IReadOnlyDictionary<string, float>? canon = _canon.CanonOf(recordId);
            Dictionary<string, object?> values = canon is null
                ? []
                : canon.ToDictionary(figure => figure.Key, figure => (object?)figure.Value, StringComparer.Ordinal);

            List<string> lines = [Localization.RenderDescription(recordId, values)];

            if (Rendered(recordId + LocalizationService.TooltipSuffix, values) is { } card) lines.Add(card);
            if (canon is null) lines.Add(Text(NoCanonFormat, recordId));

            return new PreviewText(Localization.Localize(recordId), lines);
        }

        /// <summary>A resource or a recipe: what the reader made of the record, worded as the bag words
        /// it. There are no lines to roll and no stats to fold — a plain item is its name and its text.</summary>
        private PreviewText Item(string recordId)
        {
            IItem item;

            try
            {
                item = _items.CopyItem(recordId);
            }
            catch (ArgumentException)
            {
                return PreviewText.Says(Text(NoItemFormat, recordId));
            }

            ItemCardText card = ItemTooltipText.Card(item, _formats);

            return new PreviewText(card.Title, card.Lines);
        }

        /// <summary>A templated key as it reads, or null when no locale writes it — the provider echoes
        /// the key back on a miss, and a card printing "Effect_Poison_Tooltip" is printing its absence.</summary>
        private string? Rendered(string key, IReadOnlyDictionary<string, object?> values)
        {
            string text = Localization.Render(key, values);

            return text.Length > 0 && !string.Equals(text, key, StringComparison.Ordinal) ? text : null;
        }

        /// <summary>The grant factory a preview mints with: it builds the grants an equip template
        /// declares, and the providers that would word them live in the battle module.</summary>
        private static GrantFactory Grants() => new(static () => null, static () => null, static () => null);

        /// <summary>Hands one catalog's open documents to a reader of the game's, and keeps whatever it
        /// could not read as a note of this run.</summary>
        private void Read(CatalogWorkspace workspace, string catalog, IGameDataParticipant participant) =>
            WorkspaceDocuments.Read(workspace, catalog, participant, _notes);
    }
}
