namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>How a word stands where it is written, which is what says how it is rewritten: a value
    /// under a key of its own, an element of a list, or the key a map is written under.</summary>
    public enum ReferenceUseKind
    {
        Value,
        ListItem,
        MapKey
    }

    /// <summary>One place a reference into some catalog is written: the file holding it and the address
    /// inside that file, together with how the word stands there.</summary>
    /// <remarks><see cref="At"/> addresses the NODE, the key included: for a map key it is what stands
    /// under the key, which is the node whose key a rename gives another word to.</remarks>
    public sealed record ReferenceUse(CatalogFile File, JsonPointer At, ReferenceUseKind Kind);

    /// <summary>One reference as it was found: where it stands, the word written there, and everywhere
    /// that word may point. The targets travel with it because whether a use belongs to a record is a
    /// question about where the FIELD points and not about what the word happens to spell.</summary>
    public sealed record ReferenceMention(ReferenceUse Where, string Id, IReadOnlyList<ReferenceTarget> Targets);

    /// <summary>
    /// A finder of references the catalogs' own schemas cannot see. The narrative writes its conditions
    /// and actions as free json — the shapes they may take belong to the game's factories and not to the
    /// catalog — so nothing in a schema says that a key called <c>questId</c> inside one of them points
    /// anywhere at all, and the host that owns those words hands a finder over instead.
    /// </summary>
    /// <remarks>A source may hand back places the schemas already found. They are counted once: a place
    /// rewritten twice is a place counted twice, and the answer must not depend on how many finders
    /// happened to walk it.</remarks>
    public interface IReferenceUseSource
    {
        IEnumerable<ReferenceMention> Uses(CatalogWorkspace workspace);
    }

    /// <summary>
    /// Where the run writes each id: the question <see cref="ReferenceIndex"/> answers turned round.
    /// The index says which words a field may hold; this says which fields hold one word — what a rename
    /// has to carry with it, and what an author is about to break by retyping an id.
    /// <para>A use is a word WRITTEN and not a word answered: an id nothing in the run holds a record
    /// under is found here all the same, because a reference pointing at a record nobody wrote is exactly
    /// the place its author has to be able to find.</para>
    /// <para>A reference narrowed to a section answers only for records of that section, and one naming
    /// the whole of a catalog answers for any of them — the same rule the forward index states, asked
    /// from the other end. A question that names no section is answered by the whole catalog: the caller
    /// did not narrow it, and narrowing it here would be the tool deciding.</para>
    /// <para>Worked out again the first time it is asked for after anything changed, for the reason the
    /// forward index gives: a keystroke invalidates it, and a walk over every document per keystroke is a
    /// walk nobody reads.</para>
    /// </summary>
    public sealed class ReferenceUses
    {
        private readonly CatalogWorkspace _workspace;
        private readonly IReadOnlyList<IReferenceUseSource> _sources;

        /// <summary>Every use of the run, filed under the word it writes.</summary>
        private readonly Dictionary<string, List<ReferenceMention>> _written = new(StringComparer.Ordinal);

        /// <summary>Whether anything has been written since the uses were worked out.</summary>
        private readonly CatalogChanges _changes;

        public ReferenceUses(CatalogWorkspace workspace, IEnumerable<IReferenceUseSource>? sources = null)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            _workspace = workspace;
            _sources = sources is null ? [] : [.. sources];
            _changes = new CatalogChanges(workspace);
        }

        /// <summary>What a record answers to, as a reference names it: its catalog, narrowed to the
        /// section it stands in where the catalog writes its records under one. A record of a catalog
        /// written as one nameless section narrows nothing — there is no section for a field to have been
        /// pointed at.</summary>
        public static ReferenceTarget Naming(CatalogView view, CatalogRecord record)
        {
            ArgumentNullException.ThrowIfNull(view);
            ArgumentNullException.ThrowIfNull(record);

            return record.Section.Length > 0
                ? new ReferenceTarget(view.Catalog, record.Section)
                : ReferenceTarget.Whole(view.Catalog);
        }

        /// <summary>Every place the run writes <paramref name="id"/> where a record of
        /// <paramref name="named"/> is meant, in the order the catalogs are read in. An empty word is
        /// written nowhere: a reference left empty names nothing on purpose, and answering it with every
        /// unfinished field of the run would be answering another question.</summary>
        public IReadOnlyList<ReferenceUse> UsesOf(ReferenceTarget named, string id)
        {
            ArgumentNullException.ThrowIfNull(named);

            if (string.IsNullOrEmpty(id)) return [];

            Refreshed();

            if (!_written.TryGetValue(id, out List<ReferenceMention>? mentions)) return [];

            List<ReferenceUse> uses = [];

            foreach (ReferenceMention mention in mentions)
                if (Answers(mention.Targets, named))
                    uses.Add(mention.Where);

            return uses;
        }

        /// <summary>Whether a field pointing at these targets could be answered by a record of
        /// <paramref name="named"/>. A target naming a section answers that section alone; one naming the
        /// whole catalog answers any of them; and a question that named no section is answered by the
        /// whole catalog for the same reason.</summary>
        private static bool Answers(IReadOnlyList<ReferenceTarget> targets, ReferenceTarget named)
        {
            foreach (ReferenceTarget target in targets)
            {
                if (!string.Equals(target.Catalog, named.Catalog, StringComparison.Ordinal)) continue;
                if (target.Section is null || named.Section is null) return true;
                if (string.Equals(target.Section, named.Section, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        /// <summary>Every reference one catalog's files write, read out of the documents rather than out
        /// of the records the catalog was opened with: a record added, taken out or retyped since is
        /// exactly what this index exists to answer for.</summary>
        private static void Written(CatalogView catalog, ICollection<ReferenceMention> found)
        {
            List<string> unheard = [];

            foreach (CatalogFile file in catalog.Files)
                foreach (CatalogRecord record in CatalogRecords.Read(catalog.Schema, file, unheard))
                    ReferenceWalk.Record(file, record.Schema, record.Token, record.Pointer, vocabulary: null, found);
        }

        private void Refreshed()
        {
            if (!_changes.TakeChange()) return;

            _written.Clear();

            List<ReferenceMention> found = [];

            foreach (CatalogView catalog in _workspace.Catalogs) Written(catalog, found);
            foreach (IReferenceUseSource source in _sources)
                foreach (ReferenceMention mention in source.Uses(_workspace))
                    found.Add(mention);

            HashSet<ReferenceUse> seen = [];

            foreach (ReferenceMention mention in found)
            {
                // One place is one use however many finders walked it: the narrative's own finder walks
                // the records the schemas walk too, and a place taken down twice would be rewritten twice
                // and counted twice.
                if (!seen.Add(mention.Where)) continue;

                if (!_written.TryGetValue(mention.Id, out List<ReferenceMention>? mentions))
                    _written[mention.Id] = mentions = [];

                mentions.Add(mention);
            }
        }
    }
}
