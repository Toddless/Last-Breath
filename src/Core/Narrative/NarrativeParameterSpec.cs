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

        /// <summary>An id of a record in one of the targets named on the parameter.</summary>
        Reference,

        /// <summary>A condition entry of the same vocabulary; its own type key names the factory
        /// that reads it.</summary>
        NestedCondition,

        /// <summary>A list of condition entries.</summary>
        NestedConditions,

        /// <summary>A list of references into the targets named on the parameter.</summary>
        References
    }

    /// <summary>What a narrative parameter's value MEANS, where its shape does not say it. A fact key is
    /// text like any other text, and nothing about the shape tells a registry of the world's facts apart
    /// from a word the author invented — which of the two it is decides whether a key can be offered, and
    /// whether the entry writing it answers a key somebody else reads.</summary>
    public enum NarrativeParameterRole
    {
        None,

        /// <summary>A fact key the entry asks about.</summary>
        FactRead,

        /// <summary>A fact key the entry writes.</summary>
        FactWritten
    }

    /// <summary>Where a narrative reference may point: a catalog, and at most one section of it. A catalog
    /// whose sections answer to nothing each other — the material categories beside the materials — offers
    /// a parameter naming the whole of it far more ids than the game will resolve.</summary>
    /// <remarks>The game's own word for a target; an authoring tool reads it through an adapter, which is
    /// why no tool type is named here.</remarks>
    public sealed record NarrativeReferenceTarget(string Catalog, string? Section = null)
    {
        /// <summary>The whole of a catalog: every section of it answers. Written out rather than left to the
        /// second argument, so that "no section" reads as a decision wherever it is made.</summary>
        public static NarrativeReferenceTarget Whole(string catalog) => new(catalog);
    }

    /// <summary>One key a narrative factory reads: the json name it is written under, what it holds, and
    /// whatever narrows that down — the members offered, the targets pointed into, the fallback.</summary>
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

        /// <summary>Where a reference points: a record is named if any one of the targets knows it.</summary>
        public IReadOnlyList<NarrativeReferenceTarget> Targets { get; init; } = [];

        /// <summary>The parameter was declared not to be a reference, whatever its name suggests: a
        /// decision told apart from a silence.</summary>
        public bool RefusedAsReference { get; init; }

        /// <summary>What the value means beyond its shape; <see cref="NarrativeParameterRole.None"/> for
        /// the parameters that are only what they are written as.</summary>
        public NarrativeParameterRole Role { get; init; }

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
