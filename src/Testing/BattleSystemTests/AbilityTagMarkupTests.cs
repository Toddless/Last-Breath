namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;

    /// <summary>
    /// The tags an ability carries are not decoration: an augment that names no ability and claims no
    /// book reaches that ability through one shared tag and through nothing else. So a tag dropped in
    /// a data pass takes augments off an ability without a word — the file still parses, the vocabulary
    /// audit still passes, the registry still builds everything, and the only thing that changed is
    /// that a socket the player could fill is now offered less.
    ///
    /// What is written out below is every pair the markup leans on: an ability and a tag of its own
    /// that is the ONLY thing carrying at least one augment onto it. Pairs where the augment would
    /// arrive by another tag anyway are not here — losing such a tag costs nothing and holding it
    /// would make this a copy of the file rather than a claim about it.
    ///
    /// The table shrinks when a record stops being judged by its tags. Thirteen pairs went that way in
    /// the catalogue pass of 2026-08-09, which named an ability on every record whose upgrade type names
    /// one ability AND whose tags carried it to a stranger or to nowhere — not on every record written
    /// for one ability's members. The ones whose tags happened to land right were left judged by their
    /// tags, and they are only as safe as that accident: 'Augment_Lucky_Crit' still travels by 'crit'
    /// because 'crit' is today worn by Critical Calculation alone, and the day a second ability claims
    /// it the augment throws on arrival there. So an augment that used to reach a stranger through
    /// 'buff', 'hit' or 'evasion' now goes where it works, those three tags carry nothing of their own
    /// meanwhile, and the accidents stay accidents until the augments meant to be general are written
    /// to be general (waves A–C of PLAN-Augments).
    /// </summary>
    [TestClass]
    public class AbilityTagMarkupTests
    {
        /// <summary>The tier every fit below is judged at — the deepest a socket goes, so a refusal is
        /// the tags talking and never the slot being too small for the record.</summary>
        private const int DeepestSocket = 3;

        private static readonly (string AbilityId, string Tag)[] s_loadBearing =
        [
            ("Ability_Series_Of_Attacks", AbilityTags.Attack),
            ("Ability_Series_Of_Attacks", AbilityTags.Damage),

            ("Ability_Increasing_Pressure", AbilityTags.Attack),
            ("Ability_Increasing_Pressure", AbilityTags.Damage),

            ("Ability_Jar_Of_Poison", AbilityTags.Poison),
            ("Ability_Jar_Of_Poison", AbilityTags.Stacks),
            ("Ability_Jar_Of_Poison", AbilityTags.Duration),
            ("Ability_Jar_Of_Poison", AbilityTags.Projectile),

            // Load-bearing again since CL-4b: the fury-burn records wear "health" by the owner's markup
            // and reach every health-wearer through it — silently, but the tag is what carries them.
            ("Ability_Dark_Shroud", AbilityTags.Health),
            ("Ability_Dark_Shroud", AbilityTags.Stacks),
            ("Ability_Dark_Shroud", AbilityTags.Duration),
            ("Ability_Dark_Shroud", AbilityTags.Buff),
            ("Ability_Dark_Shroud", AbilityTags.Recovery),

            ("Ability_Critical_Calculation", AbilityTags.Stacks),
            ("Ability_Critical_Calculation", AbilityTags.Duration),
            ("Ability_Critical_Calculation", AbilityTags.Buff),

            ("Ability_Poison_Explosion", AbilityTags.Poison),
            ("Ability_Poison_Explosion", AbilityTags.Execute),

            ("Ability_Poison_Coating", AbilityTags.Poison),
            ("Ability_Poison_Coating", AbilityTags.Stacks),
            ("Ability_Poison_Coating", AbilityTags.Duration),
            ("Ability_Poison_Coating", AbilityTags.Buff),

            ("Ability_Double_Strike", AbilityTags.Attack),
            ("Ability_Double_Strike", AbilityTags.Damage),
            ("Ability_Double_Strike", AbilityTags.Stacks),
            ("Ability_Double_Strike", AbilityTags.Duration),
            ("Ability_Double_Strike", AbilityTags.Debuff),

            ("Ability_Ares_Blessing", AbilityTags.Health),
            ("Ability_Ares_Blessing", AbilityTags.Duration),
            ("Ability_Ares_Blessing", AbilityTags.Buff),
            ("Ability_Ares_Blessing", AbilityTags.Recovery),

            ("Ability_Porcupine", AbilityTags.Armor),
            ("Ability_Porcupine", AbilityTags.Stacks),
            ("Ability_Porcupine", AbilityTags.Duration),
            ("Ability_Porcupine", AbilityTags.Buff),

            ("Ability_Berserk_Fury", AbilityTags.Attack),
            ("Ability_Berserk_Fury", AbilityTags.Health),
            ("Ability_Berserk_Fury", AbilityTags.Damage),
            ("Ability_Berserk_Fury", AbilityTags.Stacks),
            ("Ability_Berserk_Fury", AbilityTags.Duration),
            ("Ability_Berserk_Fury", AbilityTags.Debuff),

            ("Ability_Head_Butt", AbilityTags.Attack),
            ("Ability_Head_Butt", AbilityTags.Control),
            ("Ability_Head_Butt", AbilityTags.Damage),

            ("Ability_Sacrifice", AbilityTags.Health),
            ("Ability_Sacrifice", AbilityTags.Buff),

            ("Ability_Armageddon", AbilityTags.Health),
            ("Ability_Armageddon", AbilityTags.Damage),
            ("Ability_Armageddon", AbilityTags.Control),

            ("Ability_Ice_Block", AbilityTags.Control),
            ("Ability_Ice_Block", AbilityTags.Damage),
            ("Ability_Ice_Block", AbilityTags.Debuff),

            ("Ability_Ice_Aegis", AbilityTags.Stacks),
            ("Ability_Ice_Aegis", AbilityTags.Duration),
            ("Ability_Ice_Aegis", AbilityTags.Buff),
            ("Ability_Ice_Aegis", AbilityTags.Debuff),

            ("Ability_Ice_Shards", AbilityTags.Damage),
            ("Ability_Ice_Shards", AbilityTags.Projectile),

            ("Ability_Deep_Freeze", AbilityTags.Stacks),
            ("Ability_Deep_Freeze", AbilityTags.Duration),
            ("Ability_Deep_Freeze", AbilityTags.Debuff),
            ("Ability_Deep_Freeze", AbilityTags.Damage),

            ("Ability_Discharge", AbilityTags.Damage),
            ("Ability_Discharge", AbilityTags.Recovery),

            ("Ability_Overload", AbilityTags.Buff),

            ("Ability_Static_Armor", AbilityTags.Duration),
            ("Ability_Static_Armor", AbilityTags.Damage),
            ("Ability_Static_Armor", AbilityTags.Recovery),

            ("Ability_Twin_Assist_Attack", AbilityTags.Attack),

            // Load-bearing since CL-7: the seven records unbound there travel by these tags and by
            // nothing else — 'critical' carries the two crit-bonus records, 'hit' the two on-hit
            // debuff appliers, 'activation' the enhanced-defence applier.
            ("Ability_Armageddon", AbilityTags.Activation),
            ("Ability_Armageddon", AbilityTags.Critical),
            ("Ability_Berserk_Fury", AbilityTags.Critical),
            ("Ability_Deep_Freeze", AbilityTags.Critical),
            ("Ability_Deep_Freeze", AbilityTags.Hit),
            ("Ability_Discharge", AbilityTags.Activation),
            ("Ability_Discharge", AbilityTags.Critical),
            ("Ability_Discharge", AbilityTags.Hit),
            ("Ability_Double_Strike", AbilityTags.Critical),
            ("Ability_Head_Butt", AbilityTags.Critical),
            ("Ability_Ice_Block", AbilityTags.Activation),
            ("Ability_Ice_Block", AbilityTags.Critical),
            ("Ability_Ice_Block", AbilityTags.Hit),
            ("Ability_Ice_Shards", AbilityTags.Activation),
            ("Ability_Ice_Shards", AbilityTags.Critical),
            ("Ability_Ice_Shards", AbilityTags.Hit),
            ("Ability_Increasing_Pressure", AbilityTags.Critical),
            ("Ability_Jar_Of_Poison", AbilityTags.Hit),
            ("Ability_Overload", AbilityTags.Critical),
            ("Ability_Poison_Explosion", AbilityTags.Activation),
            ("Ability_Poison_Explosion", AbilityTags.Hit),
            ("Ability_Series_Of_Attacks", AbilityTags.Critical),
            ("Ability_Static_Armor", AbilityTags.Critical),
            ("Ability_Static_Armor", AbilityTags.Hit),

            // Load-bearing since CL-7c, where the last seven pinned records went out on the tags of
            // their cards: 'activation' now carries the cooldown reset, 'stage' and 'spell' the stage-four
            // damage, 'empowered' the harvest heal, 'shield' the pain-driven cooldown chance.
            ("Ability_Ares_Blessing", AbilityTags.Activation),
            ("Ability_Chain_Lightning", AbilityTags.Spell),
            ("Ability_Critical_Calculation", AbilityTags.Activation),
            ("Ability_Dark_Shroud", AbilityTags.Activation),
            ("Ability_Deep_Freeze", AbilityTags.Activation),
            ("Ability_Ice_Aegis", AbilityTags.Activation),
            ("Ability_Ice_Aegis", AbilityTags.Stage),
            ("Ability_Overload", AbilityTags.Activation),
            ("Ability_Overload", AbilityTags.Empowered),
            ("Ability_Overload", AbilityTags.Stage),
            ("Ability_Poison_Coating", AbilityTags.Activation),
            ("Ability_Porcupine", AbilityTags.Activation),
            ("Ability_Sacrifice", AbilityTags.Activation),
            ("Ability_Static_Armor", AbilityTags.Activation),
            ("Ability_Twin_Assist_Shield", AbilityTags.Shield),

            // Load-bearing since E-2a, where the base contract stopped being universal-only: the design
            // list gives its records tags of their own, so 'scale', 'cost', 'cooldown', 'consume' and
            // 'empowered' now carry augments onto the families that own those concepts.
            ("Ability_Ares_Blessing", AbilityTags.Cooldown),
            ("Ability_Ares_Blessing", AbilityTags.Cost),
            ("Ability_Armageddon", AbilityTags.Scale),
            ("Ability_Berserk_Fury", AbilityTags.Cooldown),
            ("Ability_Berserk_Fury", AbilityTags.Cost),
            ("Ability_Critical_Calculation", AbilityTags.Cooldown),
            ("Ability_Critical_Calculation", AbilityTags.Cost),
            ("Ability_Dark_Shroud", AbilityTags.Cooldown),
            ("Ability_Dark_Shroud", AbilityTags.Cost),
            ("Ability_Deep_Freeze", AbilityTags.Scale),
            ("Ability_Discharge", AbilityTags.Scale),
            ("Ability_Double_Strike", AbilityTags.Scale),
            ("Ability_Head_Butt", AbilityTags.Scale),
            ("Ability_Ice_Aegis", AbilityTags.Scale),
            ("Ability_Ice_Block", AbilityTags.Scale),
            ("Ability_Ice_Shards", AbilityTags.Scale),
            ("Ability_Increasing_Pressure", AbilityTags.Scale),
            ("Ability_Jar_Of_Poison", AbilityTags.Scale),
            ("Ability_Overload", AbilityTags.Consume),
            ("Ability_Overload", AbilityTags.Cooldown),
            ("Ability_Overload", AbilityTags.Cost),
            ("Ability_Poison_Coating", AbilityTags.Cooldown),
            ("Ability_Poison_Coating", AbilityTags.Cost),
            ("Ability_Poison_Explosion", AbilityTags.Scale),
            ("Ability_Porcupine", AbilityTags.Cooldown),
            ("Ability_Porcupine", AbilityTags.Cost),
            ("Ability_Sacrifice", AbilityTags.Consume),
            ("Ability_Sacrifice", AbilityTags.Cooldown),
            ("Ability_Sacrifice", AbilityTags.Cost),
            ("Ability_Sacrifice", AbilityTags.Empowered),
            ("Ability_Series_Of_Attacks", AbilityTags.Scale),
            ("Ability_Static_Armor", AbilityTags.Scale),

            // Load-bearing since E-2b: 'effect' was in the vocabulary and worn by nobody until the umbrella
            // record arrived. It now marks every ability that LAYS something — the appliers, and only them:
            // the four that lay nothing (both series, the explosion, the shards) do not carry it.
            ("Ability_Ares_Blessing", AbilityTags.Effect),
            ("Ability_Armageddon", AbilityTags.Effect),
            ("Ability_Berserk_Fury", AbilityTags.Effect),
            ("Ability_Critical_Calculation", AbilityTags.Effect),
            ("Ability_Dark_Shroud", AbilityTags.Effect),
            ("Ability_Deep_Freeze", AbilityTags.Effect),
            ("Ability_Discharge", AbilityTags.Effect),
            ("Ability_Double_Strike", AbilityTags.Effect),
            ("Ability_Head_Butt", AbilityTags.Effect),
            ("Ability_Ice_Aegis", AbilityTags.Effect),
            ("Ability_Ice_Block", AbilityTags.Effect),
            ("Ability_Jar_Of_Poison", AbilityTags.Effect),
            ("Ability_Overload", AbilityTags.Effect),
            ("Ability_Poison_Coating", AbilityTags.Effect),
            ("Ability_Porcupine", AbilityTags.Effect),
            ("Ability_Sacrifice", AbilityTags.Effect),
            ("Ability_Static_Armor", AbilityTags.Effect),
            ("Ability_Twin_Assist_Attack", AbilityTags.Effect),
            ("Ability_Twin_Assist_Shield", AbilityTags.Effect)
        ];

        [TestMethod]
        public void EveryTagTheMarkupLeansOnIsStillDeclaredAndStillCarriesItsAugments()
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();

            foreach ((string abilityId, string tag) in s_loadBearing)
            {
                List<string> tags = [.. catalog.TagsOf(abilityId)];
                Assert.IsTrue(tags.Contains(tag, StringComparer.OrdinalIgnoreCase),
                    $"'{abilityId}' no longer declares '{tag}', and the augments that reached it through that tag reach nothing now");

                List<string> lost = Lost(catalog, abilityId, tags, tag);
                Assert.IsTrue(lost.Count > 0,
                    $"'{tag}' carries no augment onto '{abilityId}' any more, so this pair says nothing and the markup it stood for is gone");
            }
        }

        [TestMethod]
        public void TheTableNamesEveryTagTheMarkupLeansOn()
        {
            // The other direction. A pair the table does not name is a tag holding augments nobody
            // wrote down, which is the same silence one edit later — the walk above would keep passing
            // while the markup it was written for is no longer the markup that ships.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var written = s_loadBearing.Select(pair => $"{pair.AbilityId} / {pair.Tag}").ToHashSet(StringComparer.Ordinal);
            var carrying = new HashSet<string>(StringComparer.Ordinal);

            foreach (string abilityId in book.KnownAbilityIds)
            {
                List<string> tags = [.. catalog.TagsOf(abilityId)];
                foreach (string tag in tags)
                    if (Lost(catalog, abilityId, tags, tag).Count > 0)
                        carrying.Add($"{abilityId} / {tag}");
            }

            var unwritten = carrying.Except(written).OrderBy(pair => pair, StringComparer.Ordinal).ToList();
            var stale = written.Except(carrying).OrderBy(pair => pair, StringComparer.Ordinal).ToList();

            Assert.AreEqual(0, unwritten.Count, $"tags carrying augments that the table does not name:\n  {string.Join("\n  ", unwritten)}");
            Assert.AreEqual(0, stale.Count, $"the table names tags that carry nothing:\n  {string.Join("\n  ", stale)}");
        }

        /// <summary>The augments an ability would stop being offered if it dropped that one tag — the
        /// whole meaning of a tag being load-bearing, asked of the rule that actually seats them.</summary>
        private static List<string> Lost(AbilityAugmentCatalog catalog, string abilityId, IReadOnlyCollection<string> tags, string dropped)
        {
            string[] without = [.. tags.Where(tag => !string.Equals(tag, dropped, StringComparison.OrdinalIgnoreCase))];
            return [.. Offered(catalog, abilityId, tags).Except(Offered(catalog, abilityId, without), StringComparer.Ordinal)];
        }

        /// <summary>Every tag-judged augment the fitting rule lets onto an ability carrying those tags.
        /// Records naming an ability or claiming the whole book are left out: neither of them ever asks
        /// about a tag, so neither can be lost by dropping one.</summary>
        private static IEnumerable<string> Offered(AbilityAugmentCatalog catalog, string abilityId, IReadOnlyCollection<string> tags) =>
            catalog.All
                .Where(TagJudged)
                .Where(record => AugmentFit.Check(
                    new AbilitySocketPlacement(string.Empty, abilityId, DeepestSocket), tags, record, []) == AugmentFitResult.Fits)
                .Select(record => record.Id);

        private static bool TagJudged(AbilityAugmentData record) =>
            !record.FitsAnyAbility && string.IsNullOrWhiteSpace(record.AbilityId);
    }
}
