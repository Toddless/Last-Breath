namespace Core.Narrative.Validation
{
    using System;

    /// <summary>Which record ids a run knows. The checks state where a reference may point — the targets
    /// the vocabulary and the data markup declare — and this answers whether anything is written there.
    /// The game asks its providers, an authoring tool asks the documents it has open, and neither reading
    /// belongs in the rules.</summary>
    public interface INarrativeIdSource
    {
        /// <summary>Whether this run can answer for the target at all. A catalog nobody read is not an
        /// empty catalog: every id pointing into it would otherwise be reported broken.</summary>
        bool Describes(NarrativeReferenceTarget target);

        /// <summary>Whether the target holds a record under this id.</summary>
        bool Knows(NarrativeReferenceTarget target, string id);
    }

    /// <summary>An id source made of the two questions themselves, for a caller who has the answers but no
    /// type to hang them on.</summary>
    public sealed class NarrativeIdSource(
        Func<NarrativeReferenceTarget, bool> describes,
        Func<NarrativeReferenceTarget, string, bool> knows) : INarrativeIdSource
    {
        public bool Describes(NarrativeReferenceTarget target) => describes(target);

        public bool Knows(NarrativeReferenceTarget target, string id) => knows(target, id);
    }
}
