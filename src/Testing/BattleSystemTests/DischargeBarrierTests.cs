namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>
    /// The Discharge's stage-3 refund: a share of the blow comes back as barrier. Until CL-3b it was a
    /// direct write to the field, which meant the ability wore the "recovery" tag, offered every
    /// effectiveness record that tag carries, took the payment and refunded exactly the same amount.
    /// The refund travels as an <c>EffectValue</c> now, and both halves are measured here rather than
    /// inferred from a decorator having been seated.
    /// </summary>
    [TestClass]
    public class DischargeBarrierTests
    {
        private const string AbilityId = "Ability_Discharge";
        private const string RecoveryRecord = "Augment_Recovery_Effectiveness";

        /// <summary>What the shipped record says the stage refunds, and what the shipped record adds.</summary>
        private const float Refund = 0.45f;
        private const float AddedEffectiveness = 0.35f;

        private const float Vitals = 100000f;
        private const float Barrier = 1000f;

        [TestMethod]
        public async Task TheThirdStageGivesBackItsShareOfTheBlowAsBarrier()
        {
            var owner = Fighter();
            var target = Fighter();
            (float dealt, float restored) = await Discharge(owner, target, wearing: null);

            Assert.IsTrue(dealt > 0, "the discharge dealt nothing, so the share below is a share of nothing");
            Assert.AreEqual(dealt * Refund, restored, 0.01f,
                "the third stage no longer refunds the share of the blow the record names");
        }

        [TestMethod]
        public async Task AnEffectivenessRecordRaisesTheRefundItUsedToLeaveAlone()
        {
            var plainOwner = Fighter();
            (float plainDealt, float plainRestored) = await Discharge(plainOwner, Fighter(), wearing: null);

            var owner = Fighter();
            (float dealt, float restored) = await Discharge(owner, Fighter(), wearing: RecoveryRecord);

            Assert.AreEqual(plainDealt, dealt, 0.01f, "the record moved the blow as well, so the refund below is not the only thing measured");
            Assert.AreEqual(dealt * Refund * (1f + AddedEffectiveness), restored, 0.01f,
                $"'{RecoveryRecord}' did not scale the refund");
            Assert.IsTrue(restored > plainRestored,
                $"'{RecoveryRecord}' is worn on an ability that wears its tag and the refund did not move at all");
        }

        /// <summary>One stage-3 discharge; returns what the target lost and what the caster got back.</summary>
        private static async Task<(float Dealt, float Restored)> Discharge(ConditionOwner owner, ConditionOwner target, string? wearing)
        {
            using var rolls = new CombatRandomScope(new ThirdStageNoCrit());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            var discharge = (Ability)registry.CreateAbility(AbilityId);
            if (wearing != null)
            {
                AbilityAugmentData? record = catalog.Find(wearing);
                Assert.IsNotNull(record, $"the shipped data declares no '{wearing}'");
                IAbilityAugment? upgrade = registry.CreateUpgrade(record);
                Assert.IsNotNull(upgrade, $"the registry builds nothing for '{wearing}'");
                discharge.InstallUpgrades(new Dictionary<string, IAbilityAugment> { ["socket_recovery"] = upgrade });
            }

            discharge.SetOwner(owner);
            owner.CurrentBarrier = Barrier;

            float healthBefore = target.CurrentHealth;
            await discharge.Execute([target], FieldOf(owner, target));

            // The cast eats the whole barrier before it strikes, so whatever stands afterwards is refund.
            return (healthBefore - target.CurrentHealth, owner.CurrentBarrier);
        }

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Vitals);
            fighter.SetMaximum(EntityParameter.Mana, Vitals);
            fighter.SetMaximum(EntityParameter.Barrier, Vitals);
            fighter.SetMaximum(EntityParameter.SpellDamage, 400f);
            fighter.CurrentHealth = Vitals;
            fighter.CurrentMana = Vitals;
            fighter.TakesDamageForReal = true;
            return fighter;
        }

        private static IBattleField FieldOf(IFightable owner, params IFightable[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns(enemies);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner, .. enemies]);
            field.Setup(battlefield => battlefield.GetRandomEntity(It.IsAny<IFightable>())).Returns(owner);
            return field.Object;
        }

        /// <summary>Above the stage-4 chance and below the stage-3 one, so the cast lands on stage 3 —
        /// the only stage that refunds — and the same figure fails the crit roll.</summary>
        private sealed class ThirdStageNoCrit : IRandomNumberGenerator
        {
            public float RandFloat() => 0.1f;

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => min;

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
