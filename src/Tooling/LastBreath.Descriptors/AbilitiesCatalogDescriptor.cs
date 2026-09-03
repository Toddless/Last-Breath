namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Abilities catalog: one file holding two sections that answer to nothing
    /// each other — the abilities a fighter casts and the augments that improve them — each record found
    /// by its own id and named and described in the localization by it. What a record is made of is read
    /// off <see cref="AbilityBaseData"/> and <see cref="AbilityAugmentData"/> and stated nowhere here.</summary>
    /// <remarks>
    /// An augment answers three questions at once — which ability it binds to, how its numbers are
    /// written, and what it lays — and the contract tells shapes apart by ONE thing. The binding is the
    /// axis stated as shapes, because it is the one that decides whether an id has to name an ability at
    /// all; the other two are described flat, every key optional, and which of them a record writes is
    /// left to the record.
    /// </remarks>
    public sealed class AbilitiesCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Section of the abilities themselves — what <see cref="AbilityDataRoot.Abilities"/> is
        /// written as.</summary>
        public const string AbilitiesKey = "abilities";

        /// <summary>Section of the augments, which are nobody's property: a record says which sockets take
        /// it and is written beside the abilities rather than inside one. What
        /// <see cref="AbilityDataRoot.Augments"/> is written as.</summary>
        public const string AugmentsKey = "augments";

        /// <summary>Json name of the field carrying a record's id, which both sections write.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field naming the one ability an augment is written for, and the shape
        /// its presence picks — what <see cref="AbilityAugmentData.AbilityId"/> is written as.</summary>
        public const string AbilityField = "abilityId";

        /// <summary>Json name of the field claiming every ability there is, and the shape its presence
        /// picks — what <see cref="AbilityAugmentData.FitsAnyAbility"/> is written as.</summary>
        public const string AnyAbilityField = "fitsAnyAbility";

        /// <summary>Json name of the field an augment carries the tags it travels by under, and the shape
        /// its presence picks when neither of the other two keys is there — what
        /// <see cref="AbilityAugmentData.Tags"/> is written as.</summary>
        public const string TagsField = "tags";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the folder is named for the records it holds and the file for one of the two sections in it,
        /// and they are two different facts.</summary>
        public const string FileName = "BaseAbilityData";

        public string Catalog => DataCatalog.Abilities;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Registered before the augments are read: shapes are applied where the record is BUILT.
            // The order is the order the rule reads the declaration in, narrowest first — a record naming
            // an ability is that ability's alone, a record claiming the book is everything's, and a record
            // saying neither travels by its tags. Which matters because every record writes tags, so the
            // last shape is the one nothing else claimed rather than one of three exclusive keys.
            builder.Polymorphic(typeof(AbilityAugmentData), new VariantSet(
            [
                Shape(AbilityField, builder.Record(typeof(AugmentBoundToAbility))),
                Shape(AnyAbilityField, builder.Record(typeof(AugmentForAnyAbility))),
                Shape(TagsField, builder.Record(typeof(AugmentBoundByTags)))
            ]));

            return new CatalogSchema(
                RootShape.SectionsOfArrays,
                [
                    Section(AbilitiesKey, builder.Record(typeof(AbilityBaseData))),
                    Section(AugmentsKey, builder.Record(typeof(AbilityAugmentData)))
                ],
                // A name and a description: both a cast and an augment are read in a tooltip, not only listed.
                [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix],
                new SingleFilePlacement { FileName = FileName });
        }

        private static VariantSchema Shape(string key, RecordSchema record) =>
            new() { DiscriminatorValue = key, Record = record };

        /// <summary>The records of one section, both found by the same field.</summary>
        private static SectionSchema Section(string key, RecordSchema record) =>
            new() { Key = key, Record = record with { IdField = IdField } };
    }
}
