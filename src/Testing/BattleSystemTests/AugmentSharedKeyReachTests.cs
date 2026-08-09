namespace LastBreathTest.BattleSystemTests
{
    using System.Text;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Enums;

    /// <summary>
    /// Where every record standing on a shared key actually lands, written out. A record reaches an
    /// ability through the fitting rule (tags, or a named ability) and WORKS on it through the key —
    /// two questions answered in two places, and nothing anywhere holds them against each other. The
    /// gap between them is legal and deliberate: a tag promises a fit, not a result. It is also where
    /// the ugliest thing in the catalog lives — a record that fits an ability, charges it (fewer
    /// stacks, more mana) and delivers nothing, which is not an improvement the player declined but a
    /// straight loss he cannot see.
    ///
    /// So the split is written down rather than computed: for each record, the abilities it fits and
    /// works on, and the abilities it fits and is inert on. The table is literal for the same reason
    /// <see cref="AugmentParameterTableTests"/> is — read off production it would agree with whatever
    /// production became. A new tag on an ability, a new tag on a record or a key registered one place
    /// fewer all change these lists, and every one of those changes has to be looked at and written
    /// down here, which is the whole point: inertness stays allowed and stops being invisible.
    ///
    /// Hidden abilities are out of the walk. They are never socketed, so no augment is ever offered to
    /// one and where a record would land on them is not a fact about the catalog.
    /// </summary>
    [TestClass]
    public class AugmentSharedKeyReachTests
    {
        /// <summary>The move a probe makes on the key to find out whether anything reads it. Any figure
        /// would do — what is measured is that the number moved at all.</summary>
        private const float Probe = 7f;

        /// <summary>
        /// Every move a shipped record makes on a shared key, and the two lists that move comes to:
        /// the abilities that declared the concept and the abilities that merely let the record in.
        /// </summary>
        private static readonly (string Augment, string Parameter, string[] Works, string[] Inert)[] s_reach =
        [
            ("Augment_More_Attack_Damage", AbilityParameter.DamageMultiplier,
                ["Ability_Double_Strike", "Ability_Series_Of_Attacks"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Discharge", "Ability_Head_Butt",
                 "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Static_Armor"]),

            // The same key from a second record: on either owner the two are one offer and the better
            // of them works, which is the rivalry rule reading a concept rather than a spelling.
            ("Augment_Damage_Multiplier", AbilityParameter.DamageMultiplier,
                ["Ability_Double_Strike", "Ability_Series_Of_Attacks"],
                ["Ability_Armageddon", "Ability_Berserk_Fury", "Ability_Discharge", "Ability_Head_Butt",
                 "Ability_Ice_Block", "Ability_Ice_Shards", "Ability_Increasing_Pressure", "Ability_Static_Armor"]),

            // Series of Attacks and Berserk Fury are inert by DESIGN: their series is a range and a
            // health roll, not a count, so neither declares the concept (AbilityParameter.Attacks).
            ("Augment_Additional_Amount_Attacks", AbilityParameter.Attacks,
                ["Ability_Head_Butt", "Ability_Increasing_Pressure"],
                ["Ability_Berserk_Fury", "Ability_Double_Strike", "Ability_Series_Of_Attacks"]),

            ("Augment_Additional_Lunges", AbilityParameter.Attacks,
                ["Ability_Head_Butt", "Ability_Increasing_Pressure"],
                ["Ability_Berserk_Fury", "Ability_Double_Strike", "Ability_Series_Of_Attacks"]),

            // Nine of the eleven inert abilities are reached through the bare tag "duration" alone.
            ("Augment_Poison_Duration", AbilityParameter.PoisonDuration,
                ["Ability_Jar_Of_Poison", "Ability_Poison_Coating"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Ice_Aegis", "Ability_Poison_Explosion",
                 "Ability_Porcupine", "Ability_Sacrifice", "Ability_Static_Armor"]),

            // The worst record in the catalog and the reason this walk exists: where it is inert it
            // still charges its fifty mana, so every name in the second list is a straight loss the
            // player cannot see. Armageddon stuns and is NOT reached — its tags carry neither "control"
            // nor "duration" (a hole for wave E), while Deep Freeze is reached and freezes rather than
            // stuns. Neither is fixed here: the record's tags are wave C's.
            ("Augment_Extend_Stun_Add_Cost", AbilityParameter.StunDuration,
                ["Ability_Head_Butt", "Ability_Ice_Aegis", "Ability_Ice_Block"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Jar_Of_Poison", "Ability_Poison_Coating",
                 "Ability_Porcupine", "Ability_Sacrifice", "Ability_Static_Armor"]),

            // The berserker joined the buff durations: his fury is a buff on his own caster, so "your
            // buff lasts longer" reaches it and lengthens the health it burns along with it.
            ("Augment_Buff_Duration", AbilityParameter.Duration,
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Ice_Aegis", "Ability_Poison_Coating", "Ability_Porcupine", "Ability_Static_Armor"],
                ["Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Jar_Of_Poison", "Ability_Sacrifice"]),

            ("Augment_Increased_Buff_Duration", AbilityParameter.Duration,
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Critical_Calculation", "Ability_Dark_Shroud",
                 "Ability_Ice_Aegis", "Ability_Poison_Coating", "Ability_Porcupine", "Ability_Static_Armor"],
                ["Ability_Deep_Freeze", "Ability_Double_Strike", "Ability_Jar_Of_Poison", "Ability_Sacrifice"]),

            // A record naming its ability reaches that one and no other, shared key or not.
            ("Ability_Sa_Augment_Buff_Duration", AbilityParameter.Duration, ["Ability_Static_Armor"], []),

            // The one record in the book that SHORTENS a buff, and the reason it names its ability: a
            // shorter fury burns less health, and there is no such trade anywhere else — on any other
            // buff the same move is a loss with nothing bought. Left on its tag it reached eight.
            ("Augment_Fury_Duration", AbilityParameter.Duration, ["Ability_Berserk_Fury"], []),

            ("Augment_Additional_Projectiles", AbilityParameter.ProjectileCount, ["Ability_Ice_Shards"], []),

            // Both effectiveness records stand on one key in one direction, so where they meet they are
            // one offer and the better works. What separates them is which abilities are offered the
            // deal — tags, not keys, which is the whole segmentation of the family.
            ("Augment_Buff_Effectiveness", AbilityParameter.Effectiveness,
                ["Ability_Critical_Calculation", "Ability_Dark_Shroud"],
                ["Ability_Ares_Blessing", "Ability_Ice_Aegis", "Ability_Poison_Coating", "Ability_Porcupine",
                 "Ability_Sacrifice", "Ability_Static_Armor"]),

            ("Augment_Recovery_Effectiveness", AbilityParameter.Effectiveness,
                ["Ability_Dark_Shroud"], ["Ability_Ares_Blessing"]),

            ("Augment_More_Stacks_More_Cost", AbilityParameter.Stacks,
                ["Ability_Critical_Calculation", "Ability_Dark_Shroud"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Double_Strike",
                 "Ability_Ice_Aegis", "Ability_Jar_Of_Poison", "Ability_Poison_Coating", "Ability_Porcupine",
                 "Ability_Static_Armor"]),

            // Both halves of one bargain, so both lists have to be the SAME list: an ability where the
            // stacks come off and the effectiveness does not is charged for nothing.
            ("Augment_Add_Effectiveness_Reduce_Stacks", AbilityParameter.Stacks,
                ["Ability_Critical_Calculation", "Ability_Dark_Shroud"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Double_Strike",
                 "Ability_Ice_Aegis", "Ability_Jar_Of_Poison", "Ability_Poison_Coating", "Ability_Porcupine",
                 "Ability_Static_Armor"]),

            ("Augment_Add_Effectiveness_Reduce_Stacks", AbilityParameter.Effectiveness,
                ["Ability_Critical_Calculation", "Ability_Dark_Shroud"],
                ["Ability_Ares_Blessing", "Ability_Berserk_Fury", "Ability_Deep_Freeze", "Ability_Double_Strike",
                 "Ability_Ice_Aegis", "Ability_Jar_Of_Poison", "Ability_Poison_Coating", "Ability_Porcupine",
                 "Ability_Static_Armor"]),
        ];

        [TestMethod]
        public void EveryRecordOnASharedKeyReachesExactlyTheAbilitiesWrittenDown()
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var report = new StringBuilder();

            foreach ((string augmentId, string parameter, string[] works, string[] inert) in s_reach)
            {
                AbilityUpgradeData? record = catalog.Find(augmentId);
                Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");

                (List<string> reached, List<string> ignored) = Landing(registry, record, parameter);

                Disagreement(report, $"{augmentId} on {parameter} — works on", works, reached);
                Disagreement(report, $"{augmentId} on {parameter} — inert on", inert, ignored);
            }

            Assert.AreEqual(0, report.Length,
                $"the reach of a shared-key record no longer matches what is written down:\n{report}");
        }

        [TestMethod]
        public void ARecordThatMovesSeveralSharedKeysWorksOnTheSameAbilitiesThroughAllOfThem()
        {
            // A record made of several moves is one bargain, and an ability where only some of them
            // land gets the parts of that bargain the concepts happen to give it. When the missing part
            // is what the record CHARGES, the leftover is a bill with nothing behind it — which is how
            // "+35% effectiveness for two stacks" spent a wave taking two stacks off an ability that
            // never read effectiveness. The claim below is not about any one record: any record whose
            // moves come apart between abilities is the same bug wearing a different id.
            var byRecord = s_reach.GroupBy(row => row.Augment, StringComparer.Ordinal).Where(group => group.Count() > 1);

            foreach (IGrouping<string, (string Augment, string Parameter, string[] Works, string[] Inert)> record in byRecord)
            {
                (_, string firstKey, string[] first, _) = record.First();

                foreach ((_, string parameter, string[] works, _) in record.Skip(1))
                    Assert.IsTrue(first.OrderBy(id => id, StringComparer.Ordinal)
                            .SequenceEqual(works.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal),
                        $"'{record.Key}' arrives in halves: it moves '{firstKey}' on [{string.Join(", ", first)}] "
                        + $"but '{parameter}' on [{string.Join(", ", works)}]");
            }
        }

        /// <summary>Which abilities the record both fits and moves, and which it fits and does not. The
        /// slot is given the record's own tier, so the tier never decides the answer — what is asked is
        /// which abilities the record BELONGS to, not which of the player's sockets can hold it.</summary>
        private static (List<string> Works, List<string> Inert) Landing(
            AbilityProvider registry, AbilityUpgradeData record, string parameter)
        {
            List<string> works = [];
            List<string> inert = [];

            foreach (string abilityId in registry.KnownAbilityIds.Where(id => !registry.IsHidden(id)).OrderBy(id => id, StringComparer.Ordinal))
            {
                IAbility ability = registry.CreateAbility(abilityId);
                var slot = new AbilitySocketPlacement("socket_reach_probe", abilityId, record.Tier);

                if (AugmentFit.Check(slot, ability.Tags, record, []) != AugmentFitResult.Fits) continue;

                (Moves(ability, parameter) ? works : inert).Add(abilityId);
            }

            return (works, inert);
        }

        /// <summary>Whether anything on the ability reads the key: a move is laid on it and the number
        /// is read back. An unregistered key answers the same nothing before and after, which is what
        /// being inert IS — the augment is worn and changes not one number of the cast.</summary>
        private static bool Moves(IAbility ability, string parameter)
        {
            float before = ability[parameter];
            new AbilityUpgradeParameterSet("Augment_Reach_Probe", [], 3, [(parameter, OperationType.Add, Probe)]).Apply(ability);
            return Math.Abs(ability[parameter] - before) > 0.0001f;
        }

        private static void Disagreement(StringBuilder report, string what, IReadOnlyCollection<string> written, IReadOnlyCollection<string> found)
        {
            if (written.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(found.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal))
                return;

            report.AppendLine($"  {what}");
            report.AppendLine($"    written: {string.Join(", ", written.OrderBy(id => id, StringComparer.Ordinal))}");
            report.AppendLine($"    found:   {string.Join(", ", found.OrderBy(id => id, StringComparer.Ordinal))}");
        }
    }
}
