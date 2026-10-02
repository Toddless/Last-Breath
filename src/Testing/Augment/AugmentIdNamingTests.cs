namespace LastBreathTest.Augment
{
    using Ability;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;

    /// <summary>
    /// What a record's ID is allowed to say about where the record belongs. An id is not decoration:
    /// it is the first thing a reader — a designer, a loot table, a localization key, the next pass
    /// over the catalog — uses to decide whether an augment is written for one ability or offered to a
    /// family. When the id says one thing and the record says another, every one of those readers is
    /// wrong in a different way, and none of them fails loudly.
    ///
    /// So the two are held together here, in the owner's own rule:
    /// <list type="number">
    /// <item>No id wears the <c>Ability_</c> prefix. That prefix belongs to the book of abilities, and
    /// a record wearing it reads as an ability in every list the two share.</item>
    /// <item>A record NAMING an ability is called after it — <c>Augment_&lt;Ability&gt;_&lt;what it
    /// does&gt;</c> — so a binding that is dropped in a data pass leaves an id that no longer matches
    /// and is caught here rather than in a build nobody expected to widen.</item>
    /// <item>A record naming NO ability is called after no ability, which is the same rule read
    /// backwards and the one that catches the drift the other direction: a record generalised onto its
    /// tags but still carrying the old ability's name in its id claims a binding it does not have.</item>
    /// </list>
    ///
    /// Rule three is deliberately the EXACT prefix <c>Augment_&lt;Ability&gt;_</c> and never a looser
    /// resemblance. A record may legitimately share a word with an ability — the poison series
    /// (<c>Augment_Poison_Attack_Series</c>) is not the poison explosion's, the scales-for-cooldown
    /// record (<c>Augment_Increasing_Scales_Add_Cooldown</c>) is not Increasing Pressure's — and a
    /// substring test would refuse both while proving nothing about either. What the prefix catches is
    /// the whole ability name followed by a separator, which is exactly what rule two writes.
    /// </summary>
    [TestClass]
    public class AugmentIdNamingTests
    {
        private const string AbilityPrefix = "Ability_";
        private const string AugmentPrefix = "Augment_";

        [TestMethod]
        public void NoRecordWearsTheAbilityPrefix()
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            List<string> wrong = [.. catalog.All
                .Select(record => record.Id)
                .Where(id => id.StartsWith(AbilityPrefix, StringComparison.Ordinal))
                .OrderBy(id => id, StringComparer.Ordinal)];

            Assert.AreEqual(0, wrong.Count,
                $"records wearing the prefix of the ability book:\n  {string.Join("\n  ", wrong)}");

            List<string> unprefixed = [.. catalog.All
                .Select(record => record.Id)
                .Where(id => !id.StartsWith(AugmentPrefix, StringComparison.Ordinal))
                .OrderBy(id => id, StringComparer.Ordinal)];

            Assert.AreEqual(0, unprefixed.Count,
                $"records that are not called augments at all:\n  {string.Join("\n  ", unprefixed)}");
        }

        [TestMethod]
        public void ARecordNamingAnAbilityIsCalledAfterIt()
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            List<string> wrong = [];

            foreach (AbilityAugmentData record in catalog.All.Where(record => !string.IsNullOrWhiteSpace(record.AbilityId)))
            {
                string expected = $"{AugmentPrefix}{Bare(record.AbilityId)}_";
                if (!record.Id.StartsWith(expected, StringComparison.Ordinal))
                    wrong.Add($"'{record.Id}' is bound to '{record.AbilityId}' and is not called '{expected}…'");
            }

            Assert.AreEqual(0, wrong.Count,
                $"bound records whose id does not name the ability they are bound to:\n  {string.Join("\n  ", wrong)}");
        }

        [TestMethod]
        public void ARecordNamingNoAbilityIsCalledAfterNoAbility()
        {
            // The guard against the drift back. A record unbound onto its tags keeps working under its
            // old name, and the name is the only thing left claiming it belongs to one ability — so it
            // goes on being read as that ability's private property by everyone but the fitting rule.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            string[] abilities = [.. book.KnownAbilityIds.Select(Bare)];
            List<string> wrong = [];

            foreach (AbilityAugmentData record in catalog.All.Where(record => string.IsNullOrWhiteSpace(record.AbilityId)))
                foreach (string ability in abilities)
                    if (record.Id.StartsWith($"{AugmentPrefix}{ability}_", StringComparison.Ordinal))
                        wrong.Add($"'{record.Id}' is named after '{AbilityPrefix}{ability}' and is bound to nothing");

            Assert.AreEqual(0, wrong.Count,
                $"unbound records still carrying an ability's name:\n  {string.Join("\n  ", wrong)}");
        }

        private static string Bare(string abilityId) =>
            abilityId.StartsWith(AbilityPrefix, StringComparison.Ordinal) ? abilityId[AbilityPrefix.Length..] : abilityId;
    }
}
