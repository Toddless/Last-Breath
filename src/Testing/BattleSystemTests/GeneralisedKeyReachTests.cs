namespace LastBreathTest.BattleSystemTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Battle.Source;
    using Battle.Source.Effects;
    using BerserkFuryCast = Battle.Source.Abilities.BerserkFury.BerserkFury;
    using Core.Battle;
    using Core.Events;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>
    /// The three keys generalised at CL-4, each asked the only question that matters about a shared key:
    /// a record moves it and the CONTENT the ability lays comes out moved. Declaring a key and reading it
    /// are separate acts, and the effectiveness family already shipped a wave where they came apart —
    /// there the type system caught it (<c>EffectValue</c>), but these three are plain floats and nothing
    /// structural holds them. So they are held behaviourally, bare cast against worn record.
    /// </summary>
    [TestClass]
    public class GeneralisedKeyReachTests
    {
        private const float Vitals = 10000f;

        [TestMethod]
        public async Task TheHealthRecordRaisesTheCeilingTheBlessingGrants()
        {
            AresBlessingEffect bare = await Blessing(wearing: null);
            AresBlessingEffect worn = await Blessing(wearing: "Augment_Health_Bonus");

            ParameterChange bareHealth = bare.Changes.First(change => change.Parameter == EntityParameter.Health);
            ParameterChange wornHealth = worn.Changes.First(change => change.Parameter == EntityParameter.Health);

            Assert.IsTrue(worn.ValueOf(wornHealth) > bare.ValueOf(bareHealth),
                "'Augment_Health_Bonus' moved the shared key and the blessing's health share came out the same");
            Assert.AreEqual(bare.ValueOf(bare.Changes.First(change => change.Parameter == EntityParameter.HealthRecovery)),
                worn.ValueOf(worn.Changes.First(change => change.Parameter == EntityParameter.HealthRecovery)), 0.0001f,
                "the health record moved the RECOVERY half as well — the two halves are separate axes");
        }

        [TestMethod]
        public async Task TheRegenRecordRaisesWhatTheShroudRestoresEachTurn()
        {
            float bare = await ShroudRegen(wearing: null);
            float worn = await ShroudRegen(wearing: "Augment_Additional_Health_Regen");

            Assert.IsTrue(worn > bare,
                "'Augment_Additional_Health_Regen' moved the shared key and the regeneration it lays came out the same");
        }

        [TestMethod]
        public async Task TheThresholdRecordDecidesWhetherTheExplosionKills()
        {
            // Not "the number went down" — that is what the ledger already says, and it stays true with
            // the execute condition reading a constant. What is asked is the only thing the player buys:
            // one victim, a stack count BETWEEN the worn threshold and the bare one, and the kill toggles.
            const int Stacks = 14; // bare threshold 15 spares him; worn (a tenth off, → 13) executes him.

            Assert.IsFalse(await Explodes(Stacks, wearing: null),
                $"the bare explosion executed a target on {Stacks} stacks — its threshold is supposed to be above that");
            Assert.IsTrue(await Explodes(Stacks, wearing: "Augment_Reduce_Execution_Threshold"),
                $"'Augment_Reduce_Execution_Threshold' cut the threshold and a target on {Stacks} stacks still survived — "
                + "the number moved and the condition that reads it did not");
        }

        [TestMethod]
        public void TheBurningFuryLaysTheCanonAndNotTheFallbackItUsedToCarry()
        {
            // The last records carrying effect figures of their own: typed factories, so CL-3b's gate
            // never saw them, and their fallbacks had drifted a wave behind the design list. What they
            // lay now is the canon — and the two numbers the ABILITY owns still arrive from the ability.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var fury = (BerserkFuryCast)registry.CreateAbility("Ability_Berserk_Fury");
            fury.InstallUpgrades(Seat(registry, catalog, "Augment_Berserk_Fury_Burning"));

            var laid = fury.FuryFactory(4, 0.07f) as BurningFuryEffect;

            Assert.IsNotNull(laid, "the variant record no longer swaps in the burning fury at all");
            Assert.AreEqual(0.75f, laid.BurnDamage, 0.0001f,
                "the burning fury is back on its own figure for the share of burned health it deals (canon says 0.75)");
            Assert.AreEqual(999, laid.BurningMaxStacks,
                "the burning it lays is capped by a figure of the augment's own instead of the canon's");
            Assert.AreEqual(4, laid.Duration, "the duration the ABILITY cast with did not reach the variant");
            Assert.AreEqual(0.07f, laid.HealthPercent, 0.0001f, "the health share the ABILITY burns did not reach the variant");
        }

        /// <summary>One poison explosion on a victim carrying the given stacks; true when he was executed.</summary>
        private static async Task<bool> Explodes(int stacks, string? wearing)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var explosion = (Ability)registry.CreateAbility("Ability_Poison_Explosion");
            if (wearing != null) explosion.InstallUpgrades(Seat(registry, catalog, wearing));

            var owner = Fighter();
            var victim = new Mock<IFightableNpc>();
            var effects = new EffectsComponent(victim.Object);
            bool executed = false;

            victim.SetupGet(npc => npc.Effects).Returns(effects);
            victim.SetupGet(npc => npc.EntityType).Returns(EntityType.Regular);
            victim.SetupGet(npc => npc.IsAlive).Returns(true);
            victim.SetupGet(npc => npc.InstanceId).Returns("victim");
            victim.SetupGet(npc => npc.ModifierHandler).Returns(new ModifierHandlerComponent());
            victim.SetupGet(npc => npc.CombatEvents).Returns(new CombatEventBus());
            victim.SetupGet(npc => npc.Parameters).Returns(new EntityParametersComponent());
            victim.Setup(npc => npc.TakeDamage(It.IsAny<IDamageContext>())).Returns(Task.CompletedTask);
            victim.Setup(npc => npc.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
            victim.Setup(npc => npc.Kill(It.IsAny<bool>())).Callback(() => executed = true);

            for (int stack = 0; stack < stacks; stack++)
                await new DamageOverTurnEffect(duration: 3, StatusEffects.Poison, maxStacks: 999, percentFromDamage: 0.35f)
                    .Apply(new EffectApplyingContext { Caster = owner, Target = victim.Object, Source = $"stack_{stack}", Damage = DamageSnapshot.Of(DamageType.Physical, 10f) });

            Assert.AreEqual(stacks, effects.GetBy(effect => effect.Status == StatusEffects.Poison).Count(),
                "the victim did not end up carrying the stacks the case is about");

            explosion.SetOwner(owner);
            await explosion.Execute([victim.Object], FieldOf(owner));
            return executed;
        }

        private static async Task<AresBlessingEffect> Blessing(string? wearing)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var blessing = (Ability)registry.CreateAbility("Ability_Ares_Blessing");
            if (wearing != null) blessing.InstallUpgrades(Seat(registry, catalog, wearing));

            var owner = Fighter();
            blessing.SetOwner(owner);
            await blessing.Execute([owner], FieldOf(owner));

            var laid = owner.Effects.GetBy(effect => effect is AresBlessingEffect).OfType<AresBlessingEffect>().FirstOrDefault();
            Assert.IsNotNull(laid, "the blessing laid nothing, so nothing below is measured");
            return laid;
        }

        private static async Task<float> ShroudRegen(string? wearing)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var shroud = (Ability)registry.CreateAbility("Ability_Dark_Shroud");
            if (wearing != null) shroud.InstallUpgrades(Seat(registry, catalog, wearing));

            var owner = Fighter();
            shroud.SetOwner(owner);
            await shroud.Execute([owner], FieldOf(owner));

            var laid = owner.Effects.GetBy(effect => effect is HealthRegenerationEffect)
                .OfType<HealthRegenerationEffect>().FirstOrDefault();
            Assert.IsNotNull(laid, "the shroud laid no regeneration, so nothing below is measured");
            return laid.PercentRegeneration;
        }

        private static Dictionary<string, IAbilityAugment> Seat(AbilityProvider registry, AbilityAugmentCatalog catalog, string augmentId)
        {
            AbilityAugmentData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            IAbilityAugment? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentId}'");
            return new Dictionary<string, IAbilityAugment> { ["socket_key"] = upgrade };
        }

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Vitals);
            fighter.SetMaximum(EntityParameter.Mana, Vitals);
            fighter.CurrentHealth = Vitals;
            fighter.CurrentMana = Vitals;
            return fighter;
        }

        private static IBattleField FieldOf(IFightable owner)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns([]);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner]);
            field.Setup(battlefield => battlefield.GetRandomEntity(It.IsAny<IFightable>())).Returns(owner);
            return field.Object;
        }
    }
}
