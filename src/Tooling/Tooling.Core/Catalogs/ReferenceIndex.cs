namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// Which ids the run knows, by the catalog and section that write them: what a reference field may be
    /// answered with, whether the word standing in one answers to anything, and which of them a query
    /// names. Godot-free like the rest of what the host draws from — the ranking is a function of the ids
    /// and of the query, and nothing else.
    /// <para>A target naming a section is answered by that section alone: the whole of a catalog whose
    /// sections answer to nothing each other is not what such a field may hold, and offering it would be
    /// the tool suggesting the ids the game is about to drop.</para>
    /// <para>A catalog nobody described is not an empty catalog: it is a catalog this build cannot read,
    /// and it is named as such rather than counted as zero ids — or every reference into it would be
    /// reported broken. A section no described catalog writes is named the same way and for the same
    /// reason: a narrowing nothing answers is the tool's own mistake, not the author's.</para>
    /// <para>The ids are worked out again the first time they are asked for after anything changed. An
    /// id is a field like any other and an author retypes them, so an index built once at load would
    /// start answering for a catalog nobody has any more.</para>
    /// </summary>
    public sealed class ReferenceIndex
    {
        /// <summary>Ranks, best first: the whole id, the start of it, somewhere inside it.</summary>
        private const int Exact = 0;
        private const int Prefix = 1;
        private const int Inside = 2;
        private const int NoMatch = int.MaxValue;

        private readonly CatalogWorkspace _workspace;

        /// <summary>The ids of each described catalog, in the order the catalog writes them, with the
        /// sections they stand in.</summary>
        private readonly Dictionary<string, CatalogIds> _ids = new(StringComparer.Ordinal);

        /// <summary>Something in some document moved, so what is held answers for a run that no longer
        /// exists. Rebuilt when it is next asked for and not when it is invalidated: a keystroke
        /// invalidates it, and a walk over every catalog per keystroke is a walk nobody reads.</summary>
        private bool _stale = true;

        public ReferenceIndex(CatalogWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            _workspace = workspace;

            foreach (CatalogView catalog in workspace.Catalogs)
            {
                foreach (CatalogFile file in catalog.Files) Watch(file);

                catalog.FileAdded += Watch;
            }
        }

        /// <summary>Every id the named targets write, as one list: a field naming several of them is
        /// answered by any one, and an author picking an id does not care which. An id written by two of
        /// them is offered once. A target naming a section is answered by that section's ids alone.</summary>
        public IReadOnlyList<string> IdsOf(IEnumerable<ReferenceTarget> targets)
        {
            ArgumentNullException.ThrowIfNull(targets);

            Refreshed();

            List<string> ids = [];
            HashSet<string> seen = new(StringComparer.Ordinal);

            foreach (ReferenceTarget target in targets)
            {
                if (!_ids.TryGetValue(target.Catalog, out CatalogIds? written)) continue;

                foreach (string id in written.Under(target.Section))
                    if (seen.Add(id))
                        ids.Add(id);
            }

            return ids;
        }

        /// <summary>The named targets this build cannot answer for — a catalog it has no describer for,
        /// or a section the described catalog does not hold. What a field pointing at one holds cannot be
        /// judged at all, and saying so is the only honest answer.</summary>
        public IReadOnlyList<string> Undescribed(IEnumerable<ReferenceTarget> targets)
        {
            ArgumentNullException.ThrowIfNull(targets);

            Refreshed();

            List<string> missing = [];

            foreach (ReferenceTarget target in targets)
                if (!_ids.TryGetValue(target.Catalog, out CatalogIds? written) || !written.Holds(target.Section))
                    missing.Add(target.ToString());

            return missing;
        }

        /// <summary>Whether one of the named targets writes a record under this id. False for a target
        /// nobody described too — ask <see cref="Undescribed"/> before reading that as a broken word.</summary>
        public bool Exists(IEnumerable<ReferenceTarget> targets, string id)
        {
            ArgumentNullException.ThrowIfNull(targets);

            if (string.IsNullOrEmpty(id)) return false;

            Refreshed();

            foreach (ReferenceTarget target in targets)
            {
                if (!_ids.TryGetValue(target.Catalog, out CatalogIds? written)) continue;

                foreach (string known in written.Under(target.Section))
                    if (string.Equals(known, id, StringComparison.Ordinal))
                        return true;
            }

            return false;
        }

        /// <summary>
        /// The ids of the named targets a query names, best first, at most <paramref name="limit"/> of
        /// them. An empty query names them all — a picker opens on what there is to pick, and an author
        /// who does not remember the word is exactly the one who opened it.
        /// </summary>
        public IReadOnlyList<string> Search(IEnumerable<ReferenceTarget> targets, string query, int limit)
        {
            ArgumentNullException.ThrowIfNull(targets);

            IReadOnlyList<string> ids = IdsOf(targets);
            string needle = (query ?? string.Empty).Trim();

            List<string> hits = [];

            if (needle.Length == 0)
            {
                for (int index = 0; index < ids.Count && hits.Count < limit; index++) hits.Add(ids[index]);

                return hits;
            }

            List<(string Id, int Rank)> ranked = [];

            foreach (string id in ids)
            {
                int rank = Score(id, needle);

                if (rank != NoMatch) ranked.Add((id, rank));
            }

            ranked.Sort(Order);

            for (int index = 0; index < ranked.Count && hits.Count < limit; index++) hits.Add(ranked[index].Id);

            return hits;
        }

        /// <summary>Better rank first; between equal ranks the order is the ids' own, so two searches for
        /// the same word list the same rows in the same places.</summary>
        private static int Order((string Id, int Rank) first, (string Id, int Rank) second) =>
            first.Rank != second.Rank
                ? first.Rank.CompareTo(second.Rank)
                : string.CompareOrdinal(first.Id, second.Id);

        /// <summary>How well an id answers a query. Case is not part of the question: an author types
        /// what he remembers, and the catalogs spell their ids in a case of their own.</summary>
        private static int Score(string id, string needle)
        {
            if (id.Length == 0) return NoMatch;
            if (string.Equals(id, needle, StringComparison.OrdinalIgnoreCase)) return Exact;
            if (id.StartsWith(needle, StringComparison.OrdinalIgnoreCase)) return Prefix;

            return id.Contains(needle, StringComparison.OrdinalIgnoreCase) ? Inside : NoMatch;
        }

        private void Watch(CatalogFile file)
        {
            file.Document.Changed += Touched;
            _stale = true;
        }

        private void Touched(JsonPointer pointer) => _stale = true;

        private void Refreshed()
        {
            if (!_stale) return;

            _stale = false;
            _ids.Clear();

            foreach (CatalogView catalog in _workspace.Catalogs) _ids[catalog.Catalog] = Written(catalog);
        }

        /// <summary>The ids one catalog writes now, read out of its documents rather than out of the list
        /// of records it was opened with: a record added, taken out or renamed since is exactly what this
        /// index exists to answer for.
        /// <para>A record of a catalog that names none of its records is passed over: it is listed under
        /// the place it stands in, and offering that as an id would answer a reference with a word no
        /// file holds.</para></summary>
        private static CatalogIds Written(CatalogView catalog)
        {
            CatalogIds ids = new(catalog.Schema);
            List<string> unheard = [];

            foreach (CatalogFile file in catalog.Files)
                foreach (CatalogRecord record in CatalogRecords.Read(catalog.Schema, file, unheard))
                    if (CatalogRecords.Names(catalog.Schema, record.Schema) && record.CurrentId is { Length: > 0 } id)
                        ids.Add(record.Section, id);

            return ids;
        }

        /// <summary>What one described catalog answers with: every id it writes, and the same ids split
        /// by the section they stand in. Both are held, because a reference is answered by the whole
        /// catalog or by one section of it and neither answer is the other's subset to work out.</summary>
        private sealed class CatalogIds
        {
            private readonly Ids _all = new();
            private readonly Dictionary<string, Ids> _bySection = new(StringComparer.Ordinal);
            private readonly HashSet<string> _sections = new(StringComparer.Ordinal);

            public CatalogIds(CatalogSchema schema)
            {
                foreach (SectionSchema section in schema.Sections) _sections.Add(section.Key);
            }

            /// <summary>Whether the catalog holds the named section. True for the whole of it, which
            /// every catalog holds.</summary>
            public bool Holds(string? section) => section is null || _sections.Contains(section);

            /// <summary>The ids under one section, or every id the catalog writes when none is named.
            /// A section the catalog does not hold answers with nothing.</summary>
            public IReadOnlyList<string> Under(string? section) =>
                section is null ? _all.Written : _bySection.GetValueOrDefault(section)?.Written ?? [];

            /// <summary>Takes one id in, under the section that wrote it. An id two sections write is
            /// held once for the whole catalog and once under each of them: which section a reference
            /// was narrowed to is what decides whether it is an answer at all.</summary>
            public void Add(string section, string id)
            {
                if (!_bySection.TryGetValue(section, out Ids? written)) _bySection[section] = written = new Ids();

                written.Add(id);
                _all.Add(id);
            }
        }

        /// <summary>Ids in the order they were met, each of them once.</summary>
        private sealed class Ids
        {
            private readonly List<string> _order = [];
            private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

            public IReadOnlyList<string> Written => _order;

            public void Add(string id)
            {
                if (_seen.Add(id)) _order.Add(id);
            }
        }
    }
}
