namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            List<AbilityAugmentData> declaring = [.. catalog.All.Where(record => !string.IsNullOrWhiteSpace(record.Behaviour))];

            Assert.IsTrue(declaring.Count > 0, "no record declares a behaviour, so the walks prove nothing");

            foreach (AbilityAugmentData record in declaring)
            {
                Assert.IsTrue(AbilityProvider.KnownBehaviours.Contains(record.Behaviour, StringComparer.Ordinal),
                    $"'{record.Id}' names behaviour '{record.Behaviour}' the registry does not know");
                Assert.IsNotNull(registry.CreateUpgrade(record), $"'{record.Id}' declares a behaviour and builds nothing");
                Assert.IsTrue(registry.BuildableAugmentIds.Contains(record.Id, StringComparer.Ordinal),
                    $"'{record.Id}' is buildable and the registry does not say so");
            }
        }

        /// <summary>Every record that reads a number off its host: the property it fills, the shared key
        /// it points at, and the ability that has to own that key. Literal, because a ref pointing at a
        /// key the host never declared reads the record's own fallback and says nothing about it.</summary>
        private static readonly (string Record, string Property, string Key, string AbilityId)[] s_propertyRefs =
        [
            ("Ability_Porc_Augment_Armor_Buff", "duration", AbilityParameter.Duration, "Ability_Porcupine"),
            ("Ability_Porc_Augment_Incoming_Reduction", "duration", AbilityParameter.Duration, "Ability_Porcupine"),
            ("Ability_Porc_Augment_Crit_Mitigation", "duration", AbilityParameter.Duration, "Ability_Porcupine"),
            ("Ability_Ar_Augment_Incoming_Reduction", "duration", AbilityParameter.Duration, "Ability_Ares_Blessing"),
            ("Ability_Ar_Augment_Turn_End_Heal", "duration", AbilityParameter.Duration, "Ability_Ares_Blessing"),
            ("Ability_Ar_Augment_Damage_Buff", "duration", AbilityParameter.Duration, "Ability_Ares_Blessing"),
            ("Ability_Ia_Augment_Crit_Mitigation", "duration", AbilityParameter.Duration, "Ability_Ice_Aegis"),
        ];

        [TestMethod]
        public void EveryPropertyRefPointsAtAKeyItsHostActuallyOwns()
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var written = s_propertyRefs.ToDictionary(row => (row.Record, row.Property), row => (row.Key, row.AbilityId));

            foreach (AbilityAugmentData record in catalog.All.Where(entry => entry.PropertyRefs.Count > 0))
                foreach ((string property, string key) in record.PropertyRefs)
                {
                    Assert.IsTrue(written.TryGetValue((record.Id, property), out (string Key, string AbilityId) row),
                        $"'{record.Id}' reads '{property}' off its host and the table does not name it");
                    Assert.AreEqual(row.Key, key, $"'{record.Id}' points '{property}' somewhere else now");

                    // The host has to DECLARE the key, or the ref silently reads the record's fallback.
                    IAbility host = registry.CreateAbility(row.AbilityId);
                    Assert.AreNotEqual(Probe, host.ValueOr(key, Probe),
                        $"'{row.AbilityId}' does not own '{key}', so '{record.Id}' quietly reads its own number");
                }

            Assert.AreEqual(s_propertyRefs.Length, catalog.All.Sum(entry => entry.PropertyRefs.Count),
                "the shipped data reads a different number of host values than the table names");
        }

        /// <summary>A figure no ability would ever carry, so reading it back means nobody answered.</summary>
        private const float Probe = -12345f;

        [TestMethod]
        public async Task APropertyRefTakesTheHostsDecoratedNumberAndNotTheRecordsOwn()
        {
            // The claim the table above cannot make: that the number actually COMES from the host, and
            // decorated. The record carries a figure of its own as a fallback, so a ref that quietly
            // stopped working would read that one and every walk would stay green — which is exactly how
            // the mechanism was measured to fail.
            using var rolls = new CombatRandomScope(new SteadyRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            const string BuffRecord = "Ability_Porc_Augment_Armor_Buff";
            const string PorcupineId = "Ability_Porcupine";

            Ability bare = Seated(registry, catalog, PorcupineId, BuffRecord);
            int hostDuration = (int)bare[AbilityParameter.Duration];
            Assert.AreEqual(hostDuration, await ArmorBuffTurns(bare),
                "the buff was not laid for the host's own duration");

            // The other half: a longer buff on the ability has to be a longer buff in what it lays. The
            // record's own figure never moves, so this can only come from reading the decorated key.
            Ability longer = Seated(registry, catalog, PorcupineId, BuffRecord, "Augment_Buff_Duration");

            Assert.AreEqual(hostDuration + 1, await ArmorBuffTurns(longer),
                "an augment lengthening the ability's buff was not felt by what the record lays");
        }

        /// <summary>Turns the armor buff the porcupine laid on its caster came out with.</summary>
        private static async Task<int> ArmorBuffTurns(Ability porcupine)
        {
            var owner = Fighter();
            porcupine.SetOwner(owner);
            await porcupine.Execute([owner], FieldOf(owner, Fighter()));

            IEffect? buff = owner.Effects.Effects.FirstOrDefault(effect => effect.Id == "Effect_Armor_Buff");
            Assert.IsNotNull(buff, "the porcupine laid no armor buff at all, so the turns below prove nothing");

            return buff.Duration;
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
            Dictionary<string, IAbilityAugment> seated = [];

            for (int slot = 0; slot < augmentIds.Length; slot++)
            {
                IAbilityAugment? upgrade = registry.CreateUpgrade(Shipped(augmentIds[slot]));
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

        private static AbilityAugmentData Shipped(string augmentId)
        {
            AbilityAugmentData? record = ShippedAbilityData.Augments().Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            return record;
        }

        private static IAbilityAugment? Built(AbilityAugmentData record) => ShippedAbilityData.Abilities().CreateUpgrade(record);
    }
}
