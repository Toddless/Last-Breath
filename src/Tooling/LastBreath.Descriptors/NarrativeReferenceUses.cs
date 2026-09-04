namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using Core.Data.GameData;
    using Tooling.Catalogs;

    /// <summary>
    /// The ids the narrative writes inside its conditions and actions, which no catalog schema can see.
    /// The DTOs hold those keys as free json — the shapes an entry may take belong to the game's
    /// factories — so a quest id written into a <c>QuestStatus</c> clause of a dialogue is a word the
    /// schema walk reads straight past, and a record renamed without it leaves the conversation gating
    /// itself on a quest nobody has.
    /// <para>Which keys hold a vocabulary is stated once, in <see cref="NarrativeFieldVocabularies"/>, and
    /// the walk over them is the catalogs' own: the same reading the inspector draws those blocks by, so
    /// a nested condition is walked wherever it stands and no list of keys has to be kept in step.</para>
    /// </summary>
    /// <remarks>The two narrative catalogs and no others. The vocabulary answers a key by the name it is
    /// written under, and turning that loose over every catalog of the run would read some unrelated
    /// catalog's <c>actions</c> as a list of narrative clauses.
    /// <para>What the walk finds includes the references the catalogs' own schemas already find — the npc
    /// a dialogue belongs to, the npc a quest is taken from. They cost nothing: a place is one use however
    /// many finders walked it.</para></remarks>
    public sealed class NarrativeReferenceUses : IReferenceUseSource
    {
        private static readonly string[] s_catalogs = [DataCatalog.Dialogues, DataCatalog.Quests];

        public IEnumerable<ReferenceMention> Uses(CatalogWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            List<ReferenceMention> found = [];

            // What the reading of a document has to say is dropped: the run said it once when the catalog
            // was opened, and a finder of references is not the place to say it again.
            List<string> unheard = [];

            foreach (CatalogView view in workspace.Catalogs)
            {
                if (!Narrative(view.Catalog)) continue;

                foreach (CatalogFile file in view.Files)
                    foreach (CatalogRecord record in CatalogRecords.Read(view.Schema, file, unheard))
                        ReferenceWalk.Record(
                            file, record.Schema, record.Token, record.Pointer, NarrativeFieldVocabularies.Resolve, found);
            }

            return found;
        }

        private static bool Narrative(string catalog)
        {
            foreach (string named in s_catalogs)
                if (string.Equals(named, catalog, StringComparison.Ordinal))
                    return true;

            return false;
        }
    }
}
