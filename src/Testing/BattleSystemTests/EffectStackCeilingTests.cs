namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.StaticArmor;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// How many stacks of an effect a cast may lay. The number used to be whatever the ability handed the
    /// effect — the stacking rule reads <c>MaxStacks</c> off the instance being applied — so a record
    /// raising an ability's <see cref="AbilityParameter.Stacks"/> key did not fill a ceiling, it MOVED
    /// one, and "+2 stacks" was worth exactly two more stacks than the effect is balanced for.
    ///
    /// The canon is the authority instead: what an instance claims is the smaller of what was asked for
    /// and what <c>SharedData/Effects</c> balances that effect at. Asked once, at construction, so the
    /// stacking rule, the counters and every copy read one number. Effects the canon deliberately leaves
    /// out (states of an ability rather than balanced figures) have no ceiling and keep what they were given.
    /// </summary>
    [TestClass]
    public class EffectStackCeilingTests
    {
        /// <summary>The canonical ceilings the walks below lean on, written out so the claim does not
        /// quietly become "whatever the canon says today".</summary>
        private const int ClumsinessCeiling = 5;
        private const int WitheringCeiling = 3;

        /// <summary>The ice block's own stacks key, and what the shipped record adds to it — the record is
        /// built from its DECLARED figure, which is the worst rung of its rarity band.</summary>
        private const int BlockStacks = 3;
        private const string StackRecord = "Augment_Additional_Stacks";

        /// <summary>
        /// The ceiling is pulled from the composition at the moment an effect is built, so a walk about it
        /// has to BE a composition — the same road the game travels, where the registry is a service and
        /// nobody hands the effect a delegate. Composition is idempotent by design, so every case here
        /// shares the one container; the assertion guards the premise rather than assuming it, because a
        /// composition put up by somebody else would answer for a canon this walk never read.
        /// </summary>
        [TestInitialize]
        public void ComposeTheCanon()
        {
            var canon = new EffectProvider();
            foreach (string file in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Effects), "*.json"))
                canon.Apply(DataCatalog.Effects, new GameDataFile(Path.GetFileName(file), File.ReadAllText(file)));

            GameServiceProvider.Initialize(services => services.AddSingleton<IEffectProvider>(canon));

            Assert.AreEqual(ClumsinessCeiling, GameServiceProvider.TryGet<IEffectProvider>()?.StackCeilingOf("Effect_Clumsiness"),
                "the composition does not answer for the shipped canon, so nothing below is measuring the ceiling");
        }

        [TestMethod]
        public void AnEffectTheCanonBalancesNeverClaimsMoreStacksThanTheCanonAllows()
        {
            var overreaching = new Clumsiness(duration: 3, maxStacks: ClumsinessCeiling + 2, value: 0.15f);

            Assert.AreEqual(ClumsinessCeiling, overreaching.MaxStacks,
                "an effect was built claiming more stacks than the canon balances it at");
        }

        [TestMethod]
        public void AnEffectAskingForFewerStacksThanTheCanonAllowsKeepsItsOwnNumber()
        {
            // The ceiling holds a number back and never raises one — the same reading the parameter
            // floors have. An effect laid at one stack stays at one.
            var modest = new Clumsiness(duration: 3, maxStacks: 1, value: 0.15f);

            Assert.AreEqual(1, modest.MaxStacks, "the ceiling raised a number the ability never asked for");
        }

        [TestMethod]
        public void AnEffectOutsideTheCanonKeepsWhateverItsAbilityAsked()
        {
            Assert.IsNull(GameServiceProvider.TryGet<IEffectProvider>()?.StackCeilingOf("Effect_Stack_Ceiling_Probe"),
                "the canon answers for an id it does not carry, so the claim below proves nothing");

            Assert.AreEqual(7, new Uncanonical(maxStacks: 7).MaxStacks,
                "an effect the canon leaves out was clamped anyway");
        }

        [TestMethod]
        public void TheStackRecordRaisesTheAbilitysKeyAndTheLaidCeilingStaysAtTheCanon()
        {
            // Both halves of the fix in one claim. The record does what it says on the ability — the key
            // climbs — and the payload it lays is still held to what the effect is balanced for. Before the
            // ceiling the withering came out at four stacks, one past its canonical three.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityAugmentData? record = catalog.Find(StackRecord);
            Assert.IsNotNull(record, $"the shipped data declares no '{StackRecord}'");
            IAbilityAugment? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{StackRecord}'");

            var block = (Ability)registry.CreateAbility("Ability_Ice_Block");
            int declared = (int)record.UpgradeProperties["amount"];
            upgrade.Apply(block);

            Assert.AreEqual(BlockStacks + declared, (int)block[AbilityParameter.Stacks],
                "the record no longer raises the ability's own stacks key, so the ceiling below is untested");
            Assert.AreEqual(WitheringCeiling,
                new WitheringCurseEffect(duration: 3, maxStacks: (int)block[AbilityParameter.Stacks], value: 0.15f).MaxStacks,
                "the raised key carried the laid curse past the ceiling the canon balances it at");
        }

        [TestMethod]
        public void TheChargeGoesOffWhenItsPileIsFullAndNotAtSomeThresholdOfItsOwn()
        {
            // The one effect whose ceiling is also a rule about when it fires. The ability names how many
            // stacks it wants and the canon holds that number down; if the threshold were a figure of its
            // own it could be raised past the ceiling, and the charge would then count towards a number the
            // pile can never reach and never detonate at all.
            (AbilityProvider registry, _) = ShippedAbilityData.Load();
            var armor = (Ability)registry.CreateAbility("Ability_Static_Armor");
            int? ceiling = GameServiceProvider.TryGet<IEffectProvider>()?.StackCeilingOf("Effect_Charge");

            Assert.IsNotNull(ceiling, "the canon carries no ceiling for the charge, so nothing below is measured");
            Assert.IsTrue((int)armor[StaticArmor.Parameters.RequiredStacks] <= ceiling,
                "the armour asks for more charge stacks than the canon lets one pile hold — the pile would never fill");
            Assert.AreEqual(ceiling, new ChargeEffect(duration: 3, maxStacks: ceiling.Value + 2).DetonationStacks,
                "a charge built past the ceiling kept a detonation threshold the ceiling will not let it reach");
        }

        /// <summary>An effect wearing an id the canon carries no row for.</summary>
        private sealed class Uncanonical(int maxStacks) : Effect("Effect_Stack_Ceiling_Probe", duration: 3, maxStacks)
        {
            public override IEffect Copy() => new Uncanonical(MaxStacks);
        }
    }
}
