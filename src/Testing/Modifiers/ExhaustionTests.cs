namespace LastBreathTest.Modifiers
{
    using Battle.Source.Abilities.HeadButt;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.CombatRulesData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Moq;
    using Newtonsoft.Json;

    [TestClass]
    public class ExhaustionTests
    {
        private static readonly ExhaustionRules Rules = new(CostIncreasePerStack: 0.25f, DecayPerTurn: 3);

        [TestMethod]
        public void SurchargeGrowsWithEveryActivation()
        {
            var exhaustion = new ExhaustionModifier(Rules);

            Assert.AreEqual(100f, CostAfter(exhaustion, 100f), "no stacks — no surcharge");

            exhaustion.OnAbilityActivated();
            Assert.AreEqual(125f, CostAfter(exhaustion, 100f));

            exhaustion.OnAbilityActivated();
            exhaustion.OnAbilityActivated();
            Assert.AreEqual(175f, CostAfter(exhaustion, 100f));
        }

        [TestMethod]
        public void GenerousDecayForgivesATypicalTurn()
        {
            var exhaustion = new ExhaustionModifier(Rules);
            for (int i = 0; i < 3; i++) exhaustion.OnAbilityActivated();

            exhaustion.DecayTick();

            Assert.AreEqual(0, exhaustion.Stacks, "a typical turn must zero out — only a real burst carries over");
        }

        [TestMethod]
        public void BurstCarriesOverAndDecaysToZeroWithoutUnderflow()
        {
            var exhaustion = new ExhaustionModifier(Rules);
            for (int i = 0; i < 5; i++) exhaustion.OnAbilityActivated();

            exhaustion.DecayTick();
            Assert.AreEqual(2, exhaustion.Stacks);

            exhaustion.DecayTick();
            Assert.AreEqual(0, exhaustion.Stacks);
        }

        [TestMethod]
        public void ResetClearsStacks()
        {
            var exhaustion = new ExhaustionModifier(Rules);
            exhaustion.OnAbilityActivated();

            exhaustion.Reset();

            Assert.AreEqual(0, exhaustion.Stacks);
        }

        [TestMethod]
        public void AvailabilityCheckSeesTheSurcharge()
        {
            var ability = new HeadButt(new AbilityBaseData
            {
                Id = "Ability_Head_Butt",
                Cooldown = 2,
                CostValue = 60,
                CostsType = Costs.Mana,
                AbilityProperties = new() { ["stunDuration"] = 1, ["attacks"] = 2 }
            });
            var handler = new ModifierHandlerComponent();
            var owner = new Mock<IFightable>();
            owner.Setup(o => o.ModifierHandler).Returns(handler);
            owner.Setup(o => o.CombatEvents).Returns(Mock.Of<ICombatEventBus>());
            owner.Setup(o => o.CurrentMana).Returns(100f);
            ability.SetOwner(owner.Object);

            var exhaustion = new ExhaustionModifier(Rules);
            handler.Add(exhaustion);

            Assert.IsTrue(ability.IsEnoughResource());

            for (int i = 0; i < 3; i++) exhaustion.OnAbilityActivated();

            Assert.IsFalse(ability.IsEnoughResource(), "3 stacks price the cast at 105 mana — the button must not lie");
        }

        [TestMethod]
        public void ExhaustionSectionParsesFromJson()
        {
            const string json = """{ "exhaustion": { "costIncreasePerStack": 0.25, "decayPerTurn": 3 } }""";

            var data = JsonConvert.DeserializeObject<CombatRulesData>(json)!;

            Assert.AreEqual(0.25f, data.Exhaustion.CostIncreasePerStack);
            Assert.AreEqual(3, data.Exhaustion.DecayPerTurn);
        }

        private static float CostAfter(ExhaustionModifier exhaustion, float cost)
        {
            IAbilityActivationContext context = new AbilityActivationContext
            {
                Ability = Mock.Of<IAbility>(),
                Caster = Mock.Of<IFightable>(),
                Field = Mock.Of<IBattleField>(),
                Rnd = null!,
                Cost = cost,
                CostType = Costs.Mana
            };
            exhaustion.Apply(context);
            return context.Cost;
        }
    }
}
