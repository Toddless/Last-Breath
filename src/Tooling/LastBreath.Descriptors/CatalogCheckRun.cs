namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using Core.Data.AbilityData;
    using Core.Enums;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Catalogs.Checks;
    using Tooling.Localization;
    using Tooling.Schema.Model;

    /// <summary>
    /// The catalog cross-checks over the documents a tool has open. The rules are the library's own
    /// (<see cref="CatalogChecks"/>); this only answers the one question they ask of the outside — which
    /// written words the game's own parsers do NOT read as ids, whatever the markup around them says.
    /// <para>The one place that answer is written. The authoring tool presses a button on it and the
    /// game's tests run it over the shipped data, so what an author sees in the Checks panel and what a
    /// run reports are one answer.</para>
    /// </summary>
    public static class CatalogCheckRun
    {
        /// <summary>What a requirement writes the kind of thing it asks for under.</summary>
        private const string RequirementTypeField = "type";

        /// <summary>What a grant writes the kind of behaviour it hands over under.</summary>
        private const string GrantKindField = "kind";

        /// <summary>Runs every rule over the catalogs as they are written this second.</summary>
        public static IReadOnlyList<CatalogFinding> Over(
            CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts) =>
            CatalogChecks.Run(workspace, references, texts, NamesNoRecord, ReadsInOrder);

        /// <summary>
        /// Records whose shapes the game reads IN ORDER rather than as an exclusive set. An augment binds
        /// by the first of the three keys it writes, in the order the reader takes them — the claim on
        /// every ability, then the ability it names, then its tags — and every record carries tags
        /// whichever way it binds, so a record answering by claim or by name and carrying tags beside it
        /// is written exactly as it is meant to be.
        /// <para>The one pair that is no order at all is refused rather than read first: a record claiming
        /// every ability WHILE naming one answers the question twice and differently, and the reader
        /// (<c>AugmentFit</c>) turns it down wherever it is held. That the shipped data writes no such
        /// record is pinned beside these rules and not answered here.</para>
        /// </summary>
        private static bool ReadsInOrder(RecordSchema record) =>
            string.Equals(record.TypeName, nameof(AbilityAugmentData), StringComparison.Ordinal);

        /// <summary>
        /// Records whose id is not a reference at all. Two cases, and both are the price of a markup that
        /// cannot see the field beside it: a requirement demanding a level of MASTERY writes a
        /// localization key where its siblings write a resource, and a grant handing over a MODIFIER names
        /// no record anywhere — its id is the label its own minted lines carry. The markup states the
        /// catalogs without regard to the field that says which, so the shipped word answering nothing
        /// there is the limit of the contract and not broken data.
        /// </summary>
        private static bool NamesNoRecord(JObject holder)
        {
            ArgumentNullException.ThrowIfNull(holder);

            return string.Equals(holder.Value<string>(RequirementTypeField), nameof(RequirementType.MasteryLevel), StringComparison.Ordinal)
                   || string.Equals(holder.Value<string>(GrantKindField), nameof(GrantKind.Modifier), StringComparison.Ordinal);
        }
    }
}
