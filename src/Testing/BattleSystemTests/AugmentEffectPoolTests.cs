namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Moq;

    /// <summary>
    /// A record that offers a POOL of effects instead of naming one. What such a record says is the
    /// genus — "applies a buff", "applies a debuff" — and WHICH effect is a property of the copy,
    /// drawn once at the mint beside the rarity and kept for good. Everything downstream reads the
    /// copy: the behaviour it installs, the tag it teaches its ability, the line it prints and the
    /// file it is written into. Two copies of one pool record are two different augments, which is a
    /// state no other record in the catalog can be in, so it is walked here from every side.
    /// </summary>
    [TestClass]
    public class AugmentEffectPoolTests
    {
        private const string BuffPool = "Augment_Apply_Buff";
        private const string DebuffPool = "Augment_Apply_Debuff";

        /// <summary>An effect of the buff pool that is a regeneration, and so teaches its ability
        /// recovery on top of the umbrella; and one that is not.</summary>
        private const string RecoveryMember = "Effect_Regeneration";

        private const string PlainBuffMember = "Effect_Damage_Buff";

        /// <summary>
        /// What a copy of each pool record may come out laying, and what each member teaches the
        /// ability beyond the umbrella its record declares. Held literally for the same reason the
        /// granted-tag roster is: widening a pool is one line of json, and a line of json is exactly
        /// the kind of change that reaches a player without reaching a reviewer. The genus column is
        /// the load-bearing half — a member added with the wrong genus seats amplifiers the designer
        /// never meant to open, and nothing else in the build would say so.
        /// </summary>
        private static readonly (string Record, string Umbrella, (string Effect, string[] Genus)[] Pool)[] s_pools =
        [
            (BuffPool, AbilityTags.Buff,
            [
                ("Effect_Accuracy_Buff", []),
                ("Effect_Armor_Buff", []),
                ("Effect_Critical_Chance_Buff", []),
                ("Effect_Critical_Damage_Buff", []),
                ("Effect_Damage_Buff", []),
                ("Effect_Enhance_Defense", []),
                ("Effect_Evade_First_Death", []),
                ("Effect_Giants_Blessing", []),
                ("Effect_Incoming_Damage_Reduction", []),
                ("Effect_Light_Step", []),
                ("Effect_Lucky_Crit_Chance", []),
                ("Effect_Mana_Flow", [AbilityTags.Recovery]),
                ("Effect_Mana_Regeneration", [AbilityTags.Recovery]),
                ("Effect_Percent_Health_Regeneration", [AbilityTags.Recovery]),
                ("Effect_Regeneration", [AbilityTags.Recovery]),
                ("Effect_Sorcery_Gift", []),
                ("Effect_Spell_Surge", []),
                ("Effect_Weak_Regeneration", [AbilityTags.Recovery]),
            ]),
            (DebuffPool, AbilityTags.Debuff,
            [
                ("Effect_Armor_Reduction", []),
                ("Effect_Blind", []),
                ("Effect_Clouded_Mind", []),
                ("Effect_Clumsiness", []),
                ("Effect_Curse", []),
                ("Effect_Decay", []),
                ("Effect_Fatigue", []),
                ("Effect_Feebleness", []),
                ("Effect_Fragility", []),
                ("Effect_Frostbite", []),
                ("Effect_Heal_Reduction", []),
                ("Effect_Mind_Drain", []),
                ("Effect_Rot", []),
                ("Effect_Vulnerability", []),
                ("Effect_Weakness", []),
                ("Effect_Withering_Curse", []),
            ]),
        ];

        /// <summary>The two records the pools replaced. Named so their removal is a fact of the build
        /// and not of one reviewer's memory: a pinned applier quietly restored beside the general
        /// record would be a second stream of the same stacks on every ability that wears both.</summary>
        private static readonly string[] s_replaced = ["Augment_Clumsy_Blows", "Augment_Armor_Debuff_On_Hit"];

        [TestMethod]
        public void EveryPoolIsTheOneTheLedgerNamesAndSpeaksTheVocabulary()
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            var written = s_pools.ToDictionary(row => row.Record, row => row, StringComparer.Ordinal);

            foreach (AbilityAugmentData record in catalog.All)
            {
                if (record.EffectPool.Count == 0)
                {
                    Assert.IsFalse(written.ContainsKey(record.Id), $"'{record.Id}' is a pool record of the ledger and offers no pool any more");
                    continue;
                }

                Assert.IsTrue(written.TryGetValue(record.Id, out (string Record, string Umbrella, (string Effect, string[] Genus)[] Pool) row),
                    $"'{record.Id}' offers a pool the ledger does not name — a drawn effect goes unaudited");

                CollectionAssert.AreEqual(
                    row.Pool.Select(member => member.Effect).ToArray(),
                    record.PoolEffects.ToArray(),
                    $"'{record.Id}' offers a different set of effects than the ledger names");

                CollectionAssert.AreEquivalent(new[] { row.Umbrella }, record.GrantsTags,
                    $"'{record.Id}' declares an umbrella other than the genus its whole pool shares");

                foreach ((string effect, string[] genus) in row.Pool)
                    CollectionAssert.AreEquivalent(genus, record.EffectPool[effect],
                        $"'{record.Id}' teaches a different genus for '{effect}' than the ledger names");
            }
        }

        [TestMethod]
        public void ThePinnedAppliersThePoolsReplacedAreGoneFromTheCatalogAndTheText()
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            string localization = File.ReadAllText(Path.Combine(SharedData.Root(), "Localization", "en.po"));

            foreach (string record in s_replaced)
            {
                Assert.IsNull(catalog.Find(record), $"'{record}' is back in the catalog beside the general record that replaced it");
                Assert.IsFalse(localization.Contains($"msgid \"{record}\"", StringComparison.Ordinal),
                    $"'{record}' left the catalog and kept its text — a name and a line nothing can ever show");
            }
        }

        [TestMethod]
        public void ACopyLaysTheEffectItDrewAndNotTheOneItsRecordStandsFor()
        {
            // The whole of the mechanism, from the draw to the stack on the victim. The record's own
            // representative is the FIRST of its pool, so a copy drawing anything else is the only
            // proof that what runs is the copy's choice: a build that quietly fell back on the record
            // would lay the same effect for every copy and every walk below would still be green.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityAugmentData record = Record(catalog, DebuffPool);

            for (int index = 0; index < record.PoolEffects.Count; index++)
            {
                AugmentInstance copy = new AugmentMinter(catalog, new FixedChoice(index)).Mint(record);
                Assert.AreEqual(record.PoolEffects[index], copy.EffectId, "the mint drew an effect outside the record's pool");

                Assert.AreEqual(copy.EffectId, LaidBy(registry, copy),
                    $"a copy that drew '{copy.EffectId}' laid something else — its record's own effect, or nothing at all");
            }
        }

        [TestMethod]
        public void ACopysGrantIsTheGenusOfWhatItDrewAndNotOfItsWholePool()
        {
            // The gift is a property of the copy for the same reason the effect is: an applier teaches
            // its ability the genus of what IT lays. A copy that drew a regeneration opens the recovery
            // family; one that drew a damage buff does not, and both are copies of one record.
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            AbilityAugmentData record = Record(catalog, BuffPool);

            CollectionAssert.AreEquivalent(
                new[] { AbilityTags.Buff, AbilityTags.Recovery },
                Copy(record, RecoveryMember).Applied(record).GrantsTags,
                "a copy that lays a regeneration does not teach its ability recovery");

            CollectionAssert.AreEquivalent(
                new[] { AbilityTags.Buff },
                Copy(record, PlainBuffMember).Applied(record).GrantsTags,
                "a copy that lays a plain buff teaches recovery it does not give");
        }

        [TestMethod]
        public void TheBoardReadsTheGrantOffTheCopyAndNotOffTheRecord()
        {
            // The same claim where it is load-bearing: the fitting rule asks the BOARD what an ability
            // has been taught, and the board holds copies. Reading the record there would hand every
            // copy of a pool record the widest gift in its pool, and a plain buff would open the
            // recovery family it has nothing to do with.
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            AbilityAugmentData record = Record(catalog, BuffPool);
            AbilityAugmentData amplifier = Record(catalog, "Augment_Recovery_Effectiveness");
            const string Ability = "Ability_Series_Of_Attacks";

            Assert.AreEqual(AugmentFitResult.Fits, Verdict(catalog, Ability, record, amplifier, RecoveryMember),
                "the recovery amplifier stayed out beside a copy that lays a regeneration");
            Assert.AreEqual(AugmentFitResult.NoSharedTag, Verdict(catalog, Ability, record, amplifier, PlainBuffMember),
                "a copy laying a plain buff opened the recovery family — the board is reading the record");
        }

        [TestMethod]
        public void TheDrawnEffectSurvivesTheFileAndAFileWithoutOneDrawsItBack()
        {
            // Saves may break in this project, so what matters is that a copy already in the world is
            // not quietly turned into a different augment: the effect goes into the file and comes back
            // out of it. A file predating pools carries no effect at all, and that one is DRAWN rather
            // than left empty — an empty effect would fall back on the record's representative, which
            // is one particular member of the pool masquerading as everybody's.
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            AbilityAugmentData record = Record(catalog, DebuffPool);
            IAugmentItemMinter minter = new AugmentItemMinter(catalog, new AugmentMinter(catalog, new FixedChoice(0)));

            string written = record.PoolEffects[^1];
            Assert.AreEqual(written, minter.Remembered(DebuffPool, new Dictionary<string, float>(), Rarity.Epic, written)?.EffectId,
                "the effect a file remembers was re-drawn instead of carried");

            Assert.AreEqual(record.PoolEffects[0], minter.Remembered(DebuffPool, new Dictionary<string, float>(), Rarity.Epic)?.EffectId,
                "a file predating pools came back with no effect at all");

            // An effect the record no longer offers is not a memory this record can honour: it is
            // re-drawn like an unwritten one, rather than carried into a copy that lays nothing.
            Assert.AreEqual(record.PoolEffects[0], minter.Remembered(DebuffPool, new Dictionary<string, float>(), Rarity.Epic, "Effect_No_Pool_Offers")?.EffectId,
                "a file naming an effect the pool dropped was carried through as written");
        }

        [TestMethod]
        public void ARecordNamingBothAnEffectAndAPoolIsRefused()
        {
            // The one contradiction the pair can be in. Both fields parse, and whichever the code
            // happened to read would leave the other line quietly untrue — so neither is read.
            AbilityAugmentData record = Record(ShippedAbilityData.Augments(), DebuffPool);

            Assert.IsNull(ShippedAbilityData.Abilities().CreateUpgrade(record with { EffectId = "Effect_Rot" }),
                "a record saying twice what it lays was built anyway");
        }

        [TestMethod]
        public void APoolMemberTheRegistryCannotBuildTakesTheWholeRecordDown()
        {
            // Every member is validated and not only the representative: a stranger deep in the pool
            // would be a copy that lays nothing, and which copy that is would be decided by a draw.
            AbilityAugmentData record = Record(ShippedAbilityData.Augments(), DebuffPool);
            Dictionary<string, string[]> pool = new(record.EffectPool, StringComparer.Ordinal) { ["Effect_Nothing_Builds"] = [] };

            Assert.IsNull(ShippedAbilityData.Abilities().CreateUpgrade(record with { EffectPool = pool }),
                "a pool offering an effect nothing builds was accepted");
        }

        /// <summary>The effect the copy's upgrade actually lays on one touch, or null when it laid
        /// nothing. Built through <see cref="AbilityProvider.CreateUpgrade(AugmentInstance)"/> — the
        /// door every seated augment comes through.</summary>
        private static string? LaidBy(AbilityProvider registry, AugmentInstance copy)
        {
            IAbilityAugment? upgrade = registry.CreateUpgrade(copy);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for a copy of '{copy.AugmentId}'");

            var ability = (Ability)registry.CreateAbility("Ability_Series_Of_Attacks");
            ability.InstallUpgrades(new Dictionary<string, IAbilityAugment> { ["socket_pool"] = upgrade });

            var caster = new ConditionOwner();
            var victim = new ConditionOwner();
            victim.SetMaximum(EntityParameter.Health, 10000f);
            victim.CurrentHealth = 10000f;

            ability.ApplyImpactRiders(new AbilityImpact(caster, victim, Mock.Of<IBattleField>(), Damage: 100f)
            {
                Source = ability,
                Kind = ImpactKind.Attack
            }).GetAwaiter().GetResult();

            return victim.Effects.Effects.FirstOrDefault()?.Id;
        }

        /// <summary>How a board judges the amplifier once the pool copy laying <paramref name="member"/>
        /// is seated beside it.</summary>
        private static AugmentFitResult? Verdict(
            AbilityAugmentCatalog catalog, string abilityId, AbilityAugmentData donor, AbilityAugmentData amplifier, string member)
        {
            var board = new AbilitySocketBoard(catalog);
            board.Sync(
            [
                new AbilitySocketPlacement("socket_donor", abilityId, donor.Tier),
                new AbilitySocketPlacement("socket_amplifier", abilityId, amplifier.Tier)
            ]);

            Assert.IsTrue(board.Install(board.At("socket_donor"), Copy(donor, member)), "the pool copy never reached its slot");
            return board.Judge(board.At("socket_amplifier"), AugmentCopies.Copy(amplifier.Id));
        }

        private static AugmentInstance Copy(AbilityAugmentData record, string member) =>
            new(record.Id, new Dictionary<string, float>(), Rarity.Epic, member);

        private static AbilityAugmentData Record(AbilityAugmentCatalog catalog, string id)
        {
            AbilityAugmentData? record = catalog.Find(id);
            Assert.IsNotNull(record, $"the shipped data declares no '{id}'");
            return record;
        }

        /// <summary>A generator that always answers with one index of the range it is handed — the seam
        /// the pool draw goes through, pinned so a case names the effect it is about.</summary>
        private sealed class FixedChoice(int index) : IRandomNumberGenerator
        {
            public float RandFloat() => 0f;

            public float RandFloatRange(float min, float max) => min;

            public int RandIntRange(int min, int max) => Math.Clamp(min + index, min, max);

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }
    }
}
