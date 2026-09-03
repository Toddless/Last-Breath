namespace Core.Narrative
{
    using System.Collections.Generic;

    /// <summary>What one narrative parameter holds, as far as the game's own vocabulary needs to say it.
    /// An authoring tool reads it through an adapter of its own, so the game names no tool type.</summary>
    public enum NarrativeParameterKind
    {
        Text,
        Integer,
        Number,
        Boolean,

        /// <summary>One of the members named on the parameter.</summary>
        Choice,

        /// <summary>An id of a record in one of the catalogs named on the parameter.</summary>
        Reference,

        /// <summary>A condition entry of the same vocabulary; its own type key names the factory
        /// that reads it.</summary>
        NestedCondition,

        /// <summary>A list of condition entries.</summary>
        NestedConditions,

        /// <summary>A list of references into the catalogs named on the parameter.</summary>
        References
    }

    /// <summary>One key a narrative factory reads: the json name it is written under, what it holds, and
    /// whatever narrows that down — the members offered, the catalogs pointed into, the fallback.</summary>
    public sealed record NarrativeParameterSpec
    {
        public required string JsonName { get; init; }

        public required NarrativeParameterKind Kind { get; init; }

        /// <summary>The key must be written: the factory refuses the entry without it.</summary>
        public bool Required { get; init; }

        /// <summary>What the factory reads when the key is absent.</summary>
        public object? Default { get; init; }

        /// <summary>Members offered for <see cref="NarrativeParameterKind.Choice"/>; empty otherwise.</summary>
        public IReadOnlyList<string> Choices { get; init; } = [];

        /// <summary>Catalogs a reference points into: a record is named if any one of them knows it.</summary>
        public IReadOnlyList<string> Catalogs { get; init; } = [];

        /// <summary>The parameter was declared not to be a reference, whatever its name suggests: a
        /// decision told apart from a silence.</summary>
        public bool RefusedAsReference { get; init; }

        /// <summary>What the parameter means, for whoever draws it.</summary>
        public string? Documentation { get; init; }
    }

    /// <summary>The parameters one narrative type is written with, under the discriminator naming it.</summary>
    public sealed record NarrativeRecordSpec
    {
        public required string TypeName { get; init; }

        public required IReadOnlyList<NarrativeParameterSpec> Parameters { get; init; }
    }
}
