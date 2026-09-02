namespace Tooling.Catalogs
{
    using System.Collections.Generic;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>One catalog as the tool holds it: what describes it, the schema built from that, the
    /// files it is written across, its records in the order they stand in those files, and everything
    /// that could not be answered while it was read.</summary>
    public sealed class CatalogView
    {
        public required ICatalogDescriptor Descriptor { get; init; }

        public required CatalogSchema Schema { get; init; }

        public required IReadOnlyList<CatalogFile> Files { get; init; }

        public required IReadOnlyList<CatalogRecord> Records { get; init; }

        /// <summary>Notes of this catalog alone, unnamed: the reflector's, the schema checks', and
        /// whatever a file refused to answer. Named by catalog in
        /// <see cref="CatalogWorkspace.Report"/>.</summary>
        public required IReadOnlyList<string> Notes { get; init; }

        public string Catalog => Descriptor.Catalog;
    }
}
