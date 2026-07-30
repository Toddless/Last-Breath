namespace Core.PassiveTree
{
    using System.Collections.Generic;

    /// <summary>
    /// Source of the martial-arts passive tree; the data side loads it from the PassiveTree catalog.
    /// One catalog holds one tree, so consumers get the document itself rather than a lookup by name.
    /// </summary>
    public interface IPassiveTreeProvider
    {
        /// <summary>The loaded tree. An empty document until the data pipeline has run.</summary>
        PassiveTreeDocument Tree { get; }

        /// <summary>Records the reader skipped, in load order. Empty means a clean file.</summary>
        IReadOnlyList<string> Issues { get; }
    }
}
