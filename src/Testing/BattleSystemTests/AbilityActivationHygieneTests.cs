namespace LastBreathTest.BattleSystemTests
{
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.HeadButt;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Moq;

    [TestClass]
    public class AbilityActivationHygieneTests
    {
        [TestMethod]
        public void CostDecorators_FromDifferentUpgrades_BothApply()
        {
            // Both upgrades below RAISE what the cast costs, which is a bill and never an offer: each of
            // them is the price of something its own record does, so both are charged. Two records that
            // CUT one parameter are the opposite case and do not add up (AugmentRivalryTests). The two
            // are flat here on purpose: what a share does to a parameter another augment already touched
            // is its own question, answered in AugmentShareReductionTests.
            var ability = CreateAbility(cost: 100);

            new AbilityAugmentParameterSet("Upgrade_A", [], 1, [(AbilityParameter.CostValue, OperationType.Add, 20f)]).Apply(ability);
            new AbilityAugmentParameterSet("Upgrade_B", [], 2, [(AbilityParameter.CostValue, OperationType.Add, 30f)]).Apply(ability);

            Assert.AreEqual(150, ability.CostValue, "one of the two records had its bill waived by the other");
        }

        [TestMethod]
        public void CooldownDecorators_FromDifferentUpgrades_BothApply()
        {
            var ability = CreateAbility(cooldown: 10);

            new AbilityAugmentParameterSet("Upgrade_A", [], 1, [(AbilityParameter.Cooldown, OperationType.Add, 2f)]).Apply(ability);
            new AbilityAugmentParameterSet("Upgrade_B", [], 2, [(AbilityParameter.Cooldown, OperationType.Add, 3f)]).Apply(ability);

            Assert.AreEqual(15f, ability.Cooldown, "one of the two records had its longer wait waived by the other");
        }

        [TestMethod]
        public void IsEnoughResource_AccountsForEntityCastMutators()
        {
            var ability = CreateAbility(cost: 50);
            var handler = new ModifierHandlerComponent();
            var owner = CreateOwner(handler, mana: 100);
            ability.SetOwner(owner.Object);

            Assert.IsTrue(ability.IsEnoughResource());

            handler.Add(new FlatCostActivationContextModifier(100));

            Assert.IsFalse(ability.IsEnoughResource(), "the availability check must see the mutated cost the cast will actually pay");
        }

        [TestMethod]
        public void IsEnoughResource_ChecksTheSwappedResource()
        {
            var ability = CreateAbility(cost: 50);
            var handler = new ModifierHandlerComponent();
            var owner = CreateOwner(handler, mana: 0, health: 100);
            ability.SetOwner(owner.Object);

            Assert.IsFalse(ability.IsEnoughResource());

            handler.Add(new CostTypeActivationContextModifier(Costs.Health));

            Assert.IsTrue(ability.IsEnoughResource(), "a cost-type override (Seal of Blood) must point the check at the swapped resource");
        }

        [TestMethod]
        public void Preview_IsPessimistic_ChanceMutatorsDoNotRoll()
        {
            var ability = CreateAbility(cost: 50);
            var handler = new ModifierHandlerComponent();
            var owner = CreateOwner(handler, mana: 10);
            ability.SetOwner(owner.Object);
            handler.Add(new ChanceFreeCastActivationContextModifier(() => 1f));

            // The real-cast roll is not exercised here: rolling touches the native Godot RNG,
            // which is fatal outside the engine (see the 0xC0000005 rule in CLAUDE.md).
            Assert.IsFalse(ability.IsEnoughResource(), "a guaranteed free-cast chance must not make the preview optimistic");
        }

        [TestMethod]
        public async Task OneShotCastMutator_IsNotSpentByPreview()
        {
            var ability = CreateAbility(cooldown: 4);
            var handler = new ModifierHandlerComponent();
            var target = CreateOwner(handler, mana: 0);
            var caster = CreateOwner(new ModifierHandlerComponent(), mana: 0);
            var effect = new NextAbilityCooldownEffect(duration: 2, amount: 3f);

            await effect.Apply(new EffectApplyingContext { Caster = caster.Object, Target = target.Object, Source = "test" });

            IAbilityActivationContext preview = CreateContext(ability, cooldown: 4, isPreview: true);
            handler.Apply(preview);
            Assert.AreEqual(4f, preview.Cooldown, "preview must not trigger the one-shot");

            IAbilityActivationContext first = CreateContext(ability, cooldown: 4);
            handler.Apply(first);
            Assert.AreEqual(7f, first.Cooldown, "the first real cast after the preview still pays the debt");

            IAbilityActivationContext second = CreateContext(ability, cooldown: 4);
            handler.Apply(second);
            Assert.AreEqual(4f, second.Cooldown, "the one-shot is spent");
        }

        [TestMethod]
        public void Copy_WearsTheSameAugmentsThroughUpgradeInstancesOfItsOwn()
        {
            // A copy is the same ability wearing the same augments. The upgrade OBJECTS have to be its
            // own: they carry applied state (Learned, the decorators they laid on), and one shared
            // between the two would come off the original the next time the copy's slots were rebuilt.
            var ability = CreateAbility(cost: 100);
            var upgrade = new AbilityAugmentReduceCost("Upgrade_A", [], 1, 0.2f);
            ability.InstallUpgrades(new Dictionary<string, IAbilityAugment> { ["socket_one"] = upgrade });

            var copy = (Ability)ability.Copy();

            Assert.AreEqual(80, copy.CostValue, "the copy is not wearing what the original wears");
            Assert.IsFalse(ReferenceEquals(upgrade, copy.InstalledUpgrades["socket_one"]));

            copy.InstallUpgrades(new Dictionary<string, IAbilityAugment>());

            Assert.AreEqual(100, copy.CostValue, "the copy kept the augment it was told to take off");
            Assert.IsTrue(upgrade.Learned, "emptying the copy's slot took the original's augment off with it");
            Assert.AreEqual(80, ability.CostValue, "the original lost its own augment when the copy lost one");
        }

        [TestMethod]
        public void UnboundedParameters_AreFlooredAtZero()
        {
            var parameters = new EntityParametersComponent();

            parameters.SetBaseValueForParameter(EntityParameter.Evade, -5f);

            Assert.AreEqual(0f, parameters.GetValueForParameter(EntityParameter.Evade));
        }

        private static HeadButt CreateAbility(int cost = 10, int cooldown = 2) => new(new AbilityBaseData
        {
            Id = "Ability_Head_Butt",
            Cooldown = cooldown,
            CostValue = cost,
            CostsType = Costs.Mana,
            Damage = 40f,
            AbilityProperties = new() { ["stunDuration"] = 1, ["attacks"] = 2 }
        });

        private static Mock<IFightable> CreateOwner(ModifierHandlerComponent handler, float mana, float health = 1000, float barrier = 0)
        {
            var owner = new Mock<IFightable>();
            owner.Setup(o => o.ModifierHandler).Returns(handler);
            owner.Setup(o => o.CombatEvents).Returns(Mock.Of<ICombatEventBus>());
            owner.Setup(o => o.Effects.AddEffect(It.IsAny<IEffect>())).Returns(true);
            owner.Setup(o => o.CurrentMana).Returns(mana);
            owner.Setup(o => o.CurrentHealth).Returns(health);
            owner.Setup(o => o.CurrentBarrier).Returns(barrier);
            return owner;
        }

        // Rnd stays null: constructing the Godot generator is a native call — fatal outside the
        // engine — and no scenario here rolls (preview guards; the one-shot is deterministic).
        private static AbilityActivationContext CreateContext(IAbility ability, float cost = 0, float cooldown = 0, bool isPreview = false) => new()
        {
            Ability = ability,
            Caster = Mock.Of<IFightable>(),
            Field = Mock.Of<IBattleField>(),
            Rnd = null!,
            IsPreview = isPreview,
            Cost = cost,
            CostType = Costs.Mana,
            Cooldown = cooldown
        };
    }
}
