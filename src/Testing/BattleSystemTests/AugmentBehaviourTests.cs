namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Data;
    using Core.Enums;
    using Moq;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;

    /// <summary>
    /// Records that carry their own behaviour instead of a factory. Two silences are walked: a record
    /// naming a behaviour nothing builds, and one writing a field its behaviour never reads — both
    /// leave an augment that parses, is offered, is seated and does nothing.
    /// </summary>
    [TestClass]
    public class AugmentBehaviourTests
    {
        /// <summary>The record written with no code at all — the whole point of the registry.</summary>
        private const string Codeless = "Augment_Clumsy_Blows";

        [TestMethod]
        public void EveryRecordThatDeclaresABehaviourIsBuiltByIt()
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            List<AbilityUpgradeData> declaring = [.. catalog.All.Where(record => !string.IsNullOrWhiteSpace(record.Behaviour))];

            Assert.IsTrue(declaring.Count > 0, "no record declares a behaviour, so the walks prove nothing");

            foreach (AbilityUpgradeData record in declaring)
            {
                Assert.IsTrue(AbilityProvider.KnownBehaviours.Contains(record.Behaviour, StringComparer.Ordinal),
                    $"'{record.Id}' names behaviour '{record.Behaviour}' the registry does not know");
                Assert.IsNotNull(registry.CreateUpgrade(record), $"'{record.Id}' declares a behaviour and builds nothing");
                Assert.IsTrue(registry.BuildableAugmentIds.Contains(record.Id, StringComparer.Ordinal),
                    $"'{record.Id}' is buildable and the registry does not say so");
            }
        }

        [TestMethod]
        public void ABehaviourNobodyKnowsIsRefused() =>
            Assert.IsNull(Built(Shipped(Codeless) with { Behaviour = "NoSuchBehaviour" }),
                "a behaviour nothing builds came back with an upgrade");

        [TestMethod]
        public void ABehaviourMissingTheFieldItReadsIsRefused() =>
            Assert.IsNull(Built(Shipped(Codeless) with { EffectId = string.Empty }),
                "an effect-laying behaviour was built without an effect to lay");

        [TestMethod]
        public void AFieldTheBehaviourNeverReadsIsRefused() =>
            Assert.IsNull(Built(Shipped(Codeless) with { AttackModifier = "Unevadable" }),
                "a field nothing reads was accepted, so a line of data stays invisible");

        [TestMethod]
        public void ARecordWithoutABehaviourStillGoesToItsOwnFactory() =>
            Assert.IsNotNull(Built(Shipped("Augment_Poison_On_Hit")),
                "a named augment stopped being answered by the factory written for it");

        [TestMethod]
        public async Task TheCodelessRecordLaysOneStackPerAttackAndNothingOnOtherTouches()
        {
            // The mechanism doing its work, not being built. Every attack of a series is one touch, so a
            // record that says "on attacks" has to come out at one stack per attack — the arithmetic the
            // whole wave rests on — and a touch of another kind has to leave the target alone.
            using var rolls = new CombatRandomScope(new SteadyRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var owner = Fighter();
            var target = Fighter();

            // The touches are handed over directly: resolving a real attack needs the engine's roll face,
            // which nothing outside Godot may build (the same wall AugmentSharedKeyReachTests names).
            // What the record owns is the per-touch arithmetic, and that is what is measured.
            Ability series = Seated(registry, catalog, "Ability_Series_Of_Attacks", Codeless);
            series.SetOwner(owner);
            IBattleField field = FieldOf(owner, target);

            // Below the record's own stack cap, so what is measured is one stack per touch, not the cap.
            const int Attacks = 2;
            for (int attack = 0; attack < Attacks; attack++)
                await series.ApplyImpactRiders(new AbilityImpact(owner, target, field) { Source = series, Kind = ImpactKind.Attack });

            Assert.AreEqual(Attacks, Stacks(target, ClumsinessId),
                "a series of attacks left something other than one stack per attack");

            // Asked here rather than at the end: with the target still BELOW the cap, a stack that slipped
            // through the filter would show. Past the cap the count could not move and the claim would
            // pass whatever the filter did.
            await series.ApplyImpactRiders(new AbilityImpact(owner, target, field) { Source = series, Kind = ImpactKind.Splash });

            Assert.AreEqual(Attacks, Stacks(target, ClumsinessId), "a record written for attacks fired on a splash");

            await series.ApplyImpactRiders(new AbilityImpact(owner, target, field) { Source = series, Kind = ImpactKind.Attack });

            Assert.AreEqual(Attacks + 1, Stacks(target, ClumsinessId), "one more attack did not become one more stack");

            // A hit is not an attack: the jar touches its victim once, by the other road entirely.
            var jarTarget = Fighter();
            Ability jar = Seated(registry, catalog, "Ability_Jar_Of_Poison", Codeless);
            jar.SetOwner(owner);
            await jar.Execute([jarTarget], FieldOf(owner, jarTarget));

            Assert.AreEqual(0, Stacks(jarTarget, ClumsinessId), "a record written for attacks fired on a hit");
        }

        [TestMethod]
        public async Task TwoRecordsLayingOneEffectAreTwoStreamsOfStacks()
        {
            // The axis a rider is deduplicated on is the RECORD, not the effect it lays: two records
            // that happen to lay the same effect are two purchases and both work. Written down because
            // the axis changed with the registry and the pair below is the first to feel it.
            using var rolls = new CombatRandomScope(new SteadyRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var owner = Fighter();
            var alone = Fighter();
            var both = Fighter();

            Ability one = Seated(registry, catalog, "Ability_Jar_Of_Poison", "Augment_Clumsiness");
            one.SetOwner(owner);
            await one.Execute([alone], FieldOf(owner, alone));

            Ability pair = Seated(registry, catalog, "Ability_Jar_Of_Poison", "Augment_Clumsiness", Codeless);
            pair.SetOwner(owner);
            await pair.Execute([both], FieldOf(owner, both));

            Assert.IsTrue(Stacks(alone, ClumsinessId) > 0, "the jar's own record laid nothing, so the pair proves nothing");
            Assert.AreEqual(Stacks(alone, ClumsinessId), Stacks(both, ClumsinessId),
                "the second record laid a stack on a hit it was never written for");
            Assert.AreEqual(2, pair.ImpactRiders.Count, "two records laying one effect were deduplicated into one rider");
        }

        private const string ClumsinessId = "Effect_Clumsiness";

        /// <summary>The ability as the game builds it, wearing the named shipped records.</summary>
        private static Ability Seated(AbilityProvider registry, AbilityAugmentCatalog catalog, string abilityId, params string[] augmentIds)
        {
            var ability = (Ability)registry.CreateAbility(abilityId);
            Dictionary<string, IAbilityUpgrade> seated = [];

            for (int slot = 0; slot < augmentIds.Length; slot++)
            {
                IAbilityUpgrade? upgrade = registry.CreateUpgrade(Shipped(augmentIds[slot]));
                Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentIds[slot]}'");
                seated[$"socket_{slot}"] = upgrade;
            }

            ability.InstallUpgrades(seated);
            return ability;
        }

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, 10000f);
            fighter.SetMaximum(EntityParameter.Mana, 10000f);
            fighter.CurrentHealth = 10000f;
            fighter.CurrentMana = 10000f;

            return fighter;
        }

        private static int Stacks(IFightable bearer, string effectId) =>
            bearer.Effects.Effects.Count(effect => effect.Id == effectId);

        private static IBattleField FieldOf(IFightable owner, params IFightable[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns(enemies);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner, .. enemies]);
            field.Setup(battlefield => battlefield.GetRandomEntity(It.IsAny<IFightable>())).Returns(owner);

            return field.Object;
        }

        /// <summary>Every gate open and every count at the top, so the series runs its full length.</summary>
        private sealed class SteadyRoll : IRandomNumberGenerator
        {
            public float RandFloat() => 0f;

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => max;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }

        private static AbilityUpgradeData Shipped(string augmentId)
        {
            AbilityUpgradeData? record = ShippedAbilityData.Augments().Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            return record;
        }

        private static IAbilityUpgrade? Built(AbilityUpgradeData record) => ShippedAbilityData.Abilities().CreateUpgrade(record);
    }
}
