namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Data.NpcBuffsData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the NpcBuffs catalog: one array under a key, each buff found by its own id.
    /// What a buff is made of is read off <see cref="NpcBuffData"/> and stated nowhere here.</summary>
    /// <remarks>The buff is the parameter half of an npc modifier: the modifier names it and the numbers
    /// live here. The grant beside them is the half a number cannot say, and it goes out through the very
    /// factory an item grant does — which is why its payload is a map of the factory's own keys and no
    /// catalog holds them.</remarks>
    public sealed class NpcBuffsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="NpcBuffsData.Buffs"/> is
        /// written as.</summary>
        public const string RecordsKey = "buffs";

        /// <summary>Json name of the field carrying a record's id — what <see cref="NpcBuffData.Id"/> is
        /// written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field holding the parameter lines — what
        /// <see cref="NpcBuffData.Modifiers"/> is written as.</summary>
        public const string ModifiersField = "modifiers";

        /// <summary>Json name of the parameter one line moves — what
        /// <see cref="NpcBuffModifierData.Parameter"/> is written as.</summary>
        public const string ParameterField = "parameter";

        /// <summary>Json name of the way a line's value is applied — what
        /// <see cref="NpcBuffModifierData.Type"/> is written as.</summary>
        public const string ValueTypeField = "type";

        /// <summary>Json name of the field holding what the buff hands over besides numbers — what
        /// <see cref="NpcBuffData.Grants"/> is written as.</summary>
        public const string GrantsField = "grants";

        /// <summary>Json name of the field saying which catalog a grant's id is answered from — what
        /// <see cref="NpcBuffGrantData.Kind"/> is written as.</summary>
        public const string KindField = "kind";

        /// <summary>Json name of the map of factory parameter to its number — what
        /// <see cref="NpcBuffGrantData.Properties"/> is written as.</summary>
        public const string PropertiesField = "properties";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "NpcBuffs";

        public string Catalog => DataCatalog.NpcBuffs;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema buff = builder.Record(typeof(NpcBuffData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = buff }],
                // Drawn from, never shown: what the player reads is the npc modifier that pulled the buff.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
