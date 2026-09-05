namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Data.NpcModifiersData;
    using Core.Data.Schema;
    using Core.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the NpcModifiers catalog: one file of seven sections, one per kind of
    /// modifier, each holding the records of that kind found by their own ids. What a record is made of is
    /// read off the DTOs and stated nowhere here.</summary>
    /// <remarks>The key of a section is the KIND: it decides the shape the entries under it are read in,
    /// and it becomes the group a modifier's uniqueness is weighed within. So the sections are not one
    /// record split up — they are seven records, and the descriptor names each with the type the game
    /// parses it into.
    /// <para>How far that uniqueness reaches is not here: it is a rule about composing a spawn, written in
    /// the spawn-rolls document beside the ladders that say how many modifiers a spawn gets at all.</para>
    /// <para>An id is unique across the WHOLE file and not merely within a section: the game keys every
    /// modifier of the catalog in one dictionary, and a narrative action names one out of any section.</para></remarks>
    public sealed class NpcModifiersCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Section of the modifiers that strengthen the rest — what
        /// <see cref="ModifiersData.Scale"/> is written as.</summary>
        public const string ScaleKey = ModifiersData.ScaleSection;

        /// <summary>Section of the modifiers that push a drop up the tiers — what
        /// <see cref="ModifiersData.TierUpgrade"/> is written as.</summary>
        public const string TierUpgradeKey = ModifiersData.TierUpgradeSection;

        /// <summary>Section of the modifiers that promise items outright — what
        /// <see cref="ModifiersData.GuaranteedItems"/> is written as.</summary>
        public const string GuaranteedItemsKey = ModifiersData.GuaranteedItemsSection;

        /// <summary>Section of the modifiers that weight the tier chances — what
        /// <see cref="ModifiersData.TierMultiplier"/> is written as.</summary>
        public const string TierMultiplierKey = ModifiersData.TierMultiplierSection;

        /// <summary>Section of the modifiers that grant an effect to every drop — what
        /// <see cref="ModifiersData.ItemEffects"/> is written as.</summary>
        public const string ItemEffectsKey = ModifiersData.ItemEffectsSection;

        /// <summary>Section of the modifiers that weight the rarity chances — what
        /// <see cref="ModifiersData.RarityUpgrade"/> is written as.</summary>
        public const string RarityUpgradeKey = ModifiersData.RarityUpgradeSection;

        /// <summary>Section of the modifiers that floor a drop's rarity — what
        /// <see cref="ModifiersData.MinRarity"/> is written as.</summary>
        public const string MinRarityKey = ModifiersData.MinRaritySection;

        /// <summary>Json name of the field carrying a record's id, which every section writes.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the buff a modifier pulls — what
        /// <see cref="NpcModifierData.NpcBuffId"/> is written as.</summary>
        public const string NpcBuffIdField = "npcBuffId";

        /// <summary>Json name of the grant an item-effect modifier lays on every drop — what
        /// <see cref="ItemEffectData.EffectId"/> is written as.</summary>
        public const string EffectIdField = "effectId";

        /// <summary>Json name of the list a guaranteed-items modifier promises — what
        /// <see cref="GuaranteedItemsData.Items"/> is written as.</summary>
        public const string ItemsField = "items";

        /// <summary>Json name of the floor a min-rarity modifier sets — what
        /// <see cref="MinRarityModifierData.Rarity"/> is written as.</summary>
        public const string RarityField = "rarity";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "NpcModifiers";

        public string Catalog => DataCatalog.NpcModifiers;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return new CatalogSchema(
                RootShape.SectionsOfArrays,
                [
                    Section(ScaleKey, builder.Record(typeof(ScaleModifierData))),
                    Section(TierUpgradeKey, builder.Record(typeof(TierUpgradeData))),
                    Section(GuaranteedItemsKey, builder.Record(typeof(GuaranteedItemsData))),
                    Section(TierMultiplierKey, builder.Record(typeof(TierMultiplierData))),
                    Section(ItemEffectsKey, builder.Record(typeof(ItemEffectData))),
                    Section(RarityUpgradeKey, builder.Record(typeof(RarityUpgradeModifierData))),
                    Section(MinRarityKey, builder.Record(typeof(MinRarityModifierData)))
                ],
                // A modifier is read by the player twice over: it is named on the foe wearing it and its
                // line explains what wearing it costs him and pays the killer.
                [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix],
                new SingleFilePlacement { FileName = FileName });
        }

        /// <summary>The records of one section, all seven found by the same field.</summary>
        private static SectionSchema Section(string key, RecordSchema record) =>
            new() { Key = key, Record = record with { IdField = IdField } };
    }
}
