namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.EffectsData;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Effects catalog: one array under a key, each effect found by its own id
    /// and named, described and carded in the localization by it. What a record is made of is read off
    /// <see cref="EffectDefinitionData"/> and stated nowhere here.</summary>
    /// <remarks>The record is the BALANCE of an effect and not its behaviour: what an effect does is built
    /// by a registry that lives battle-side, and a row naming an id no registry answers to is still read.
    /// Hence the numbers are a free map — the keys are the building factory's own, the way a grant's
    /// payload is — and nothing here can offer the author a list of them.</remarks>
    public sealed class EffectsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="EffectCatalogData.Effects"/> is
        /// written as.</summary>
        public const string RecordsKey = "effects";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="EffectDefinitionData.Id"/> is written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field naming how hard the effect is to dispel — what
        /// <see cref="EffectDefinitionData.Power"/> is written as. Absent means the weakest.</summary>
        public const string PowerField = "power";

        /// <summary>Json name of the map of factory parameter to its number — what
        /// <see cref="EffectDefinitionData.Properties"/> is written as.</summary>
        public const string PropertiesField = "properties";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the folder is named for the records it holds and the file for the document, and they are two
        /// different facts.</summary>
        public const string FileName = "EffectsData";

        public string Catalog => DataCatalog.Effects;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema effect = builder.Record(typeof(EffectDefinitionData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = effect }],
                // A name, the rule text a bearer reads, and the standing card a keyword link opens: an
                // effect is the one thing in the game written about in all three places.
                [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix, LocalizationService.TooltipSuffix],
                new SingleFilePlacement { FileName = FileName })
            {
                // The card is the one of the three that is owed to nobody: it opens from a description
                // writing {@Effect_X} and from nowhere else, so most effects are never linked to and a
                // card written for each of them would be text no player can reach.
                RequiredSuffixes = [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix]
            };
        }
    }
}
