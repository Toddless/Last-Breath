namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Data;
    using Core.Enums;
    using Moq;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;

    /// <summary>
    /// Records that carry their own behaviour instead of a factory. Three silences are walked: a record
    /// naming a behaviour nothing builds, one writing a field its behaviour never reads, and one whose
    /// mechanism needs something of the ability that the tag carrying it there never promised — all
    /// three leave an augment that parses, is offered, is seated and does nothing.
    /// </summary>
    [TestClass]
    public class AugmentBehaviourTests
    {
        /// <summary>A record written with no code at all — the whole point of the registry. One of a
        /// family now: whichever is named here, what the walks measure is the registry and not the
        /// record, so the constant moves whenever the catalog does.</summary>
        private const string Codeless = "Augment_Attacks_Reduce_Armor";

        /// <summary>Attacks the walks below deal — under every canonical stack cap in play, so what is
        /// measured is one stack per touch rather than a cap being hit.</summary>
        private const int SeriesAttacks = 4;

        /// <summary>
        /// A class of inertness the key ledgers cannot see: a behaviour record has no host key to be
        /// missing, so <see cref="AugmentSharedKeyReachTests"/> writes it down as self-contained and
        /// stops — yet its MECHANISM can still need something of the ability that the tag carrying it
        /// there does not promise. Where that happens the record fits, is bought, is seated and does
        /// nothing, and no other table in the build would say so.
        /// <para>Only one mechanism is in that position today: <c>AttackModifier</c> installs a
        /// pre-attack mutator through <see cref="IAttackModifierHost"/>, which an ability implements
        /// only if it builds attack contexts of its own. The <c>attack</c> tag is worn more widely than
        /// that interface is implemented, so the pairs below are the seatings where the two diverge.</para>
        /// <para>Legal by "a tag promises fitting, not work" and left as it is — narrowing the tag is a
        /// design decision, not a repair. Written per pair so a NEW divergence still fails.</para>
        /// </summary>
        private static readonly (string Record, string[] InertOn, string Reason)[] s_mechanismNeedsAHost =
        [
            ("Augment_Attacks_Cannot_Be_Evaded", ["Ability_Head_Butt"],
                "the head butt strikes without building an attack context of its own, so there is no attack pipeline for a pre-attack mutator to sit in"),
        ];

        [TestMethod]
        public void ABehaviourWhoseMechanismNeedsAHostLandsOnlyWhereTheLedgerSaysItIdles()
        {
            // Computed against the literal table from both ends: every ability a host-needing record is
            // carried onto without the interface is a written row, and every written row is a seating
            // that still happens. A row left behind by a re-markup is as wrong as a missing one — it
            // would be a warning about a case the catalog no longer has.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var written = s_mechanismNeedsAHost.ToDictionary(row => row.Record, row => row.InertOn, StringComparer.Ordinal);
            Dictionary<string, List<string>> actual = [];

            foreach (AbilityAugmentData record in catalog.All.Where(entry => string.Equals(entry.Behaviour, "AttackModifier", StringComparison.Ordinal)))
                foreach (string abilityId in registry.KnownAbilityIds.Where(id => !registry.IsHidden(id)).Order(StringComparer.Ordinal))
                {
                    if (!Fits(catalog, abilityId, record)) continue;
                    if (registry.CreateAbility(abilityId) is IAttackModifierHost) continue;

                    if (!actual.TryGetValue(record.Id, out List<string>? inert)) actual[record.Id] = inert = [];
                    inert.Add(abilityId);
                }

            Assert.AreEqual(
                string.Join("\n", written.OrderBy(row => row.Key, StringComparer.Ordinal).Select(row => $"{row.Key}: [{string.Join(", ", row.Value)}]")),
                string.Join("\n", actual.OrderBy(row => row.Key, StringComparer.Ordinal).Select(row => $"{row.Key}: [{string.Join(", ", row.Value)}]")),
                "a behaviour whose mechanism needs a host lands somewhere the ledger does not name — rewrite s_mechanismNeedsAHost to the seatings printed above");

            foreach ((string record, _, string reason) in s_mechanismNeedsAHost)
                Assert.IsFalse(string.IsNullOrWhiteSpace(reason), $"'{record}' is written down as idling with no reason given");
        }

        private static bool Fits(AbilityAugmentCatalog catalog, string abilityId, AbilityAugmentData record) =>
            AugmentFit.Check(
                new AbilitySocketPlacement("socket_host_probe", abilityId, record.Tier),
                catalog.TagsOf(abilityId),
                record,
                [],
                null) == AugmentFitResult.Fits;

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
            ("Augment_Porcupine_Incoming_Damage_Reduction", "duration", AbilityParameter.Duration, "Ability_Porcupine"),
            ("Augment_Porcupine_Crit_Mitigation", "duration", AbilityParameter.Duration, "Ability_Porcupine"),
            ("Augment_Ice_Aegis_Crit_Mitigation_Under_Shield", "duration", AbilityParameter.Duration, "Ability_Ice_Aegis"),
            // The mythic reading of the calculation lasts exactly as long as the buff it reads from — the
            // design line says "while the Calculation holds", and the ref is what makes that literal.
            ("Augment_Critical_Calculation_Mythic", "duration", AbilityParameter.Duration, "Ability_Critical_Calculation"),
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
            const string BuffRecord = "Augment_Porcupine_Incoming_Damage_Reduction";
            const string PorcupineId = "Ability_Porcupine";

            Ability bare = Seated(registry, catalog, PorcupineId, BuffRecord);
            int hostDuration = (int)bare[AbilityParameter.Duration];
            Assert.AreEqual(hostDuration, await ReductionBuffTurns(bare),
                "the buff was not laid for the host's own duration");

            // The other half: a longer buff on the ability has to be a longer buff in what it lays. The
            // record's own figure never moves, so this can only come from reading the decorated key.
            // No shipped record lengthens the shared duration since the catalog cleanup, so the key is
            // decorated directly — the mechanism under test is the ref, not any particular record.
            Ability longer = Seated(registry, catalog, PorcupineId, BuffRecord);
            new AbilityAugmentParameterSet("Augment_Duration_Probe", [], 3,
                [(AbilityParameter.Duration, OperationType.Add, 1f)]).Apply(longer);

            Assert.AreEqual(hostDuration + 1, await ReductionBuffTurns(longer),
                "an augment lengthening the ability's buff was not felt by what the record lays");
        }

        /// <summary>Turns the incoming-damage-reduction buff the porcupine laid on its caster came out with.</summary>
        private static async Task<int> ReductionBuffTurns(Ability porcupine)
        {
            var owner = Fighter();
            porcupine.SetOwner(owner);
            await porcupine.Execute([owner], FieldOf(owner, Fighter()));

            IEffect? buff = owner.Effects.Effects.FirstOrDefault(effect => effect.Id == "Effect_Incoming_Damage_Reduction");
            Assert.IsNotNull(buff, "the porcupine laid no reduction buff at all, so the turns below prove nothing");

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
            Assert.IsNotNull(Built(Shipped("Augment_Poison_Attack_Series")),
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

            Assert.AreEqual(Attacks, Stacks(target, CodelessEffectId),
                "a series of attacks left something other than one stack per attack");

            // Asked here rather than at the end: with the target still BELOW the cap, a stack that slipped
            // through the filter would show. Past the cap the count could not move and the claim would
            // pass whatever the filter did.
            await series.ApplyImpactRiders(new AbilityImpact(owner, target, field) { Source = series, Kind = ImpactKind.Splash });

            Assert.AreEqual(Attacks, Stacks(target, CodelessEffectId), "a record written for attacks fired on a splash");

            await series.ApplyImpactRiders(new AbilityImpact(owner, target, field) { Source = series, Kind = ImpactKind.Attack });

            Assert.AreEqual(Attacks + 1, Stacks(target, CodelessEffectId), "one more attack did not become one more stack");

            // A hit is not an attack: the jar touches its victim once, by the other road entirely.
            var jarTarget = Fighter();
            Ability jar = Seated(registry, catalog, "Ability_Jar_Of_Poison", Codeless);
            jar.SetOwner(owner);
            await jar.Execute([jarTarget], FieldOf(owner, jarTarget));

            Assert.AreEqual(0, Stacks(jarTarget, CodelessEffectId), "a record written for attacks fired on a hit");
        }

        [TestMethod]
        public async Task BothDamageOverTurnSeriesLayOneStackPerAttackAndNothingOnAHit()
        {
            // Two legendary records that exist as data and nothing else: the canon and the factory were
            // already there, so what shipped is a line of json apiece. Nothing about them is visible in
            // code to read, which is why the promise on the card — "every attack of the ability applies
            // it" — is measured here on both, from both sides of the kind filter.
            using var rolls = new CombatRandomScope(new SteadyRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var owner = Fighter();

            foreach ((string record, string effect) in s_dotSeries)
            {
                var target = Fighter();
                Ability series = Seated(registry, catalog, "Ability_Series_Of_Attacks", record);
                series.SetOwner(owner);
                IBattleField field = FieldOf(owner, target);

                // The attacks land a real blow: a damaging effect with nothing to tick with is not laid
                // at all, so impacts reporting no damage would prove the records silent rather than working.
                for (int attack = 0; attack < SeriesAttacks; attack++)
                    await series.ApplyImpactRiders(new AbilityImpact(owner, target, field, Damage: Blow) { Source = series, Kind = ImpactKind.Attack });

                Assert.AreEqual(SeriesAttacks, Stacks(target, effect),
                    $"'{record}' left something other than one stack of '{effect}' per attack");

                // The other side of the filter, asked while the target is still below the cap: a stack that
                // slipped through would show. The record says Attack and a hit is the other road entirely.
                await series.ApplyImpactRiders(new AbilityImpact(owner, target, field, Damage: Blow) { Source = series, Kind = ImpactKind.Hit });

                Assert.AreEqual(SeriesAttacks, Stacks(target, effect), $"'{record}' fired on a hit");
            }
        }

        [TestMethod]
        public async Task TwoRecordsLayingOneEffectAreTwoStreamsOfStacks()
        {
            // The axis a rider is deduplicated on is the RECORD, not the effect it lays: two records
            // that happen to lay the same effect are two purchases and both work. The second purchase
            // is the same record under a second id — a shipped pair laying one effect would prove the
            // same thing and would tie the walk to a pairing the catalog is free to break.
            using var rolls = new CombatRandomScope(new SteadyRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var owner = Fighter();
            var target = Fighter();

            var ability = (Ability)registry.CreateAbility("Ability_Series_Of_Attacks");
            IAbilityAugment? first = registry.CreateUpgrade(Shipped(Codeless));
            IAbilityAugment? second = registry.CreateUpgrade(Shipped(Codeless) with { Id = "Augment_Attacks_Reduce_Armor_Twin" });
            Assert.IsNotNull(first, $"the registry builds nothing for '{Codeless}'");
            Assert.IsNotNull(second, "the registry builds nothing for the twin of the codeless record");
            ability.InstallUpgrades(new Dictionary<string, IAbilityAugment> { ["socket_0"] = first, ["socket_1"] = second });
            ability.SetOwner(owner);

            Assert.AreEqual(2, ability.ImpactRiders.Count, "two records laying one effect were deduplicated into one rider");

            await ability.ApplyImpactRiders(new AbilityImpact(owner, target, FieldOf(owner, target)) { Source = ability, Kind = ImpactKind.Attack });

            Assert.AreEqual(2, Stacks(target, CodelessEffectId),
                "two records laying one effect did not come out as two stacks on one touch");
        }

        [TestMethod]
        public async Task ARecordMayCarryALeverOfItsOwnBesideTheEffectItNames()
        {
            // The claim CL-3b makes about what a record may still hold. The effect registry judges a key
            // it does not read as a typo and refuses the WHOLE effect, so a record carrying one of its
            // own levers used to lay nothing at all — it passed the behaviour gate, passed every walk,
            // was seated and paid for, and the stack count was the only place it showed. The lever is
            // filtered out of what the registry is handed; it stays on the record for its own reader.
            using var rolls = new CombatRandomScope(new SteadyRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var owner = Fighter();
            var target = Fighter();

            AbilityAugmentData carrying = Shipped(Codeless) with
            {
                UpgradeProperties = new Dictionary<string, float>(StringComparer.Ordinal) { ["reviewProbeLever"] = 0.42f }
            };

            IAbilityAugment? upgrade = registry.CreateUpgrade(carrying);
            Assert.IsNotNull(upgrade, "a record carrying a lever of its own was refused outright");

            var ability = (Ability)registry.CreateAbility("Ability_Series_Of_Attacks");
            ability.InstallUpgrades(new Dictionary<string, IAbilityAugment> { ["socket_lever"] = upgrade });
            await ability.ApplyImpactRiders(new AbilityImpact(owner, target, FieldOf(owner, target)) { Source = ability, Kind = ImpactKind.Attack });

            Assert.AreEqual(1, Stacks(target, CodelessEffectId),
                "a key the effect does not read took the whole effect down with it — the record laid nothing");
            Assert.AreEqual(0.42f, carrying.UpgradeProperties["reviewProbeLever"], 0.0001f,
                "the lever was filtered off the record itself instead of only off what the registry is handed");
        }

        private const string CodelessEffectId = "Effect_Armor_Reduction";

        /// <summary>A landed physical swing — what a series of attacks reports to its riders. Physical
        /// alone on purpose: the bleeding record feeds on that component, and the burning one says it
        /// pools the whole hit, so one blow measures both records the way a weapon without fire would.</summary>
        private static readonly DamageSnapshot Blow = DamageSnapshot.Of(DamageType.Physical, 100f);

        /// <summary>The two damage-over-turn series of the design list and the effects they lay: records
        /// and no code at all, which is exactly why the promise needs measuring rather than inferring.</summary>
        private static readonly (string Record, string Effect)[] s_dotSeries =
        [
            ("Augment_Bleeding_Attack_Series", "Effect_Damage_Over_Turn_Bleed"),
            ("Augment_Burning_Attack_Series", "Effect_Damage_Over_Turn_Burning")
        ];

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
