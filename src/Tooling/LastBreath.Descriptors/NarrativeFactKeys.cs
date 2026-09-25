namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Narrative.Facts;
    using Tooling.Catalogs;
    using Tooling.Localization;

    /// <summary>What one reading of the fact keys came to: the registry itself, what the run could not read
    /// of the documents behind it, and what the checks the same run made have to say — nothing at all when
    /// they found nothing. The keys are read through the whole narrative run, so what that run knows about
    /// the documents travels with them instead of being read a second time to be told.</summary>
    public sealed record FactKeyReading(FactKeyRegistry Keys, IReadOnlyList<string> Notes, string? Said);

    /// <summary>
    /// The fact keys of the documents a tool has open: the families the game's own code keeps, and every
    /// word the dialogues and the quests write or ask about, each with the places that do so.
    /// <para>Read through the game's own narrative run and not through a walk of its own. Which key of a
    /// document holds a fact is the vocabulary's answer, and a tool asking it a second way would be
    /// offering an author words the game does not read.</para>
    /// </summary>
    public static class NarrativeFactKeys
    {
        /// <summary>The registry over the documents as they are written this second.</summary>
        public static FactKeyReading Over(CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts)
        {
            NarrativeCheckReport report = NarrativeCheckRun.Over(workspace, references, texts);

            return new FactKeyReading(report.Facts, report.Notes, NarrativeCheckRun.Said(report.Findings));
        }

        /// <summary>The keys a query names, in the registry's own order. An empty query names them all —
        /// whoever asked does not remember the word, which is exactly why he asked.</summary>
        public static IReadOnlyList<FactKeyEntry> Matching(FactKeyRegistry registry, string query)
        {
            ArgumentNullException.ThrowIfNull(registry);

            string needle = (query ?? string.Empty).Trim();

            return needle.Length == 0
                ? registry.Keys
                : [.. registry.Keys.Where(key => key.Key.Contains(needle, StringComparison.OrdinalIgnoreCase))];
        }
    }
}
