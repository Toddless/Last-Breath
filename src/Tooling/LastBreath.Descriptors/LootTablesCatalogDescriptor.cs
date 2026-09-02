namespace LastBreath.Descriptors
{
    using System;
    using System.Linq;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Enums;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the loot tables: four sections of tables, a table a list of tiers, a tier a
    /// list of positions. What a table and a tier are made of is read off the DTOs; the two things
    /// reflection cannot see are stated here — the shapes a position takes, and what a table's key names
    /// in each of the four sections.</summary>
    public sealed class LootTablesCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Tables every kill draws from, whoever died.</summary>
        public const string GeneralKey = "general";

        /// <summary>Tables of a fraction, keyed by the member of <see cref="Fractions"/> it belongs to.</summary>
        public const string FractionsKey = "fractions";

        /// <summary>Tables of a kind of foe, keyed by the member of <see cref="EntityType"/>.</summary>
        public const string TypesKey = "types";

        /// <summary>Tables of one npc, keyed by that npc's own id.</summary>
        public const string IndividualKey = "individual";

        /// <summary>Json name of the field naming a table — what <see cref="LootTableData.Key"/> is
        /// written as.</summary>
        public const string KeyField = "key";

        /// <summary>Key a position naming one thing carries, and the shape it picks.</summary>
        public const string IdField = "id";

        /// <summary>Key a position naming a set of augments carries, and the shape it picks.</summary>
        public const string AugmentsField = "augments";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "LootTables";

        public string Catalog => DataCatalog.LootTables;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Registered before the tables are read: a position sits two arrays below a table, and the
            // shapes are hung on it where the record holding it is built.
            builder.Polymorphic(typeof(TableRecord), new VariantSet(
            [
                Shape(IdField, builder.Record(typeof(LootPositionById))),
                Shape(AugmentsField, builder.Record(typeof(LootPositionByGroup)))
            ]));

            RecordSchema table = builder.Record(typeof(LootTableData)) with { IdField = KeyField };

            return new CatalogSchema(
                RootShape.SectionsOfArrays,
                [
                    Section(GeneralKey, table, Name()),
                    Section(FractionsKey, table, Choice(typeof(Fractions))),
                    Section(TypesKey, table, Choice(typeof(EntityType))),
                    Section(IndividualKey, table, Reference(DataCatalog.Npc))
                ],
                // Tables carry no author-facing text: what drops is named elsewhere and reads its own name there.
                [],
                new SingleFilePlacement { FileName = FileName });
        }

        private static VariantSchema Shape(string key, RecordSchema record) =>
            new() { DiscriminatorValue = key, Record = record };

        /// <summary>The members of an enum: the key of a section whose tables belong to something the
        /// game counts rather than something the author writes down.</summary>
        private static FieldSchema Choice(Type members) =>
            new() { JsonName = KeyField, Kind = FieldKind.Enum, EnumValues = Enum.GetNames(members) };

        private static FieldSchema Reference(string catalog) =>
            new() { JsonName = KeyField, Kind = FieldKind.Reference, RefCatalogs = [catalog] };

        /// <summary>A name the author picks himself, pointing at nothing — said out loud, because a key
        /// that names nothing and a key nobody has described yet read the same.</summary>
        private static FieldSchema Name() =>
            new() { JsonName = KeyField, Kind = FieldKind.String, RefusedAsReference = true };

        /// <summary>The tables of one section, with what its key names written onto the key. One DTO
        /// stands under all four sections, so the type cannot say four things about the one field.</summary>
        private static SectionSchema Section(string section, RecordSchema table, FieldSchema names) => new()
        {
            Key = section,
            Record = table with { Fields = [.. table.Fields.Select(field => Meaning(field, names))] }
        };

        /// <summary>What the section says its key names, written onto the field the DTO declares, so the
        /// rest of what was read off it — whether the key must be written, what it starts at — stays.</summary>
        private static FieldSchema Meaning(FieldSchema field, FieldSchema names)
        {
            if (!string.Equals(field.JsonName, names.JsonName, StringComparison.Ordinal)) return field;

            return field with
            {
                Kind = names.Kind,
                EnumValues = names.EnumValues,
                RefCatalogs = names.RefCatalogs,
                RefusedAsReference = names.RefusedAsReference
            };
        }
    }
}
