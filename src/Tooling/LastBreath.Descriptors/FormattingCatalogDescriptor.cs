namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.FormattingData;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Formatting catalog: one array under a key, one file, and a row per
    /// parameter saying how its value LOOKS. What a row is made of is read off
    /// <see cref="ParameterFormatEntry"/> and stated nowhere here.</summary>
    /// <remarks>A row is found by the parameter it formats rather than by an id of its own: the file
    /// holds no second word to address a row by, and the provider keys the units by that very member.
    /// The parameter is also its own name in the localization, which is what the tool shows the row as.</remarks>
    public sealed class FormattingCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the rows sit under — what <see cref="ParameterFormatsData.Parameters"/>
        /// is written as.</summary>
        public const string RecordsKey = "parameters";

        /// <summary>Json name of the field a row is addressed by — what
        /// <see cref="ParameterFormatEntry.Parameter"/> is written as.</summary>
        public const string IdField = "parameter";

        /// <summary>Json name of the field naming the unit a value is shown in — what
        /// <see cref="ParameterFormatEntry.Unit"/> is written as.</summary>
        public const string UnitField = "unit";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the folder is named for what formatting is, the file for what it formats.</summary>
        public const string FileName = "ParameterFormats";

        public string Catalog => DataCatalog.Formatting;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema row = builder.Record(typeof(ParameterFormatEntry)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = row }],
                // The parameter a row is found by is named in the localization by that same word.
                [LocalizedKeyAttribute.NoSuffix],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
