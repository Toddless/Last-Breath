namespace Tooling.Schema
{
    using System;
    using Tooling.Schema.Model;

    /// <summary>What one catalog looks like, written by the side that owns the data: the shape of the
    /// file around the records, which reflection cannot see, and the shapes of polymorphic ones.</summary>
    public interface ICatalogDescriptor
    {
        /// <summary>The catalog this describes, by the name its folder is known under.</summary>
        string Catalog { get; }

        CatalogSchema Describe(ISchemaBuilder builder);
    }

    /// <summary>Turns DTO types into record schemas, so a descriptor states the shape of a file and
    /// never repeats what the types already say.</summary>
    public interface ISchemaBuilder
    {
        /// <summary>Reads a DTO into a record schema: json names, kinds, and the schema attributes on
        /// its properties, nested records included.</summary>
        RecordSchema Record(Type dtoType);

        /// <summary>The shapes a DTO takes wherever it appears. Registered against the type rather
        /// than handed back on a record, because the polymorphic thing is usually buried in the tree —
        /// a loot position sits two arrays down — and only the reflector walking there can reach it.</summary>
        void Polymorphic(Type dtoType, VariantSet variants);
    }
}
