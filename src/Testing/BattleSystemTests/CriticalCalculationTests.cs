namespace LastBreathTest.BattleSystemTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Services;
    using Moq;
    using CriticalCalculationCast = Battle.Source.Abilities.CriticalCalculation.CriticalCalculation;

    /// <summary>
    /// Every figure of the Critical Calculation, asked where it comes from. The chance one stack is worth
    /// and the turns a critical hit gives back used to be written into the code — the chance as a constant
    /// of the ability, the turn as a literal inside the buff — while the record carried a number for the
    /// second that nothing read. Both are the record's now, so what the walks below measure is not the
    /// figure alone but the ROAD it travels: the record steers the cast, and a decorator seated on either
    /// key moves what the cast lays, which is the whole point of a number being a parameter at all.
    ///
    /// The stack ceiling is the third half of it. The buff had no canonical row, so it was held to
    /// whatever the ability asked for and to nothing else; the row it has now is read at construction out
    /// of the composition, like every other effect's.
    /// </summary>
    [TestClass]
    public class CriticalCalculationTests
    {
        private const string AbilityId = "Ability_Critical_Calculation";
        private const string BuffId = "Effect_Crit_Calculation_Buff";

        /// <summary>The record that adds stacks to what an ability lays, and the one thing on the shipped
        /// board that can drive this cast past its own three.</summary>
        private const string StackRecord = "Augment_Additional_Stacks";

        /// <summary>What the shipped record names, transcribed rather than read back out of the file the
        /// walks are about: the chance one stack is worth, how many stacks and turns a cast lays, and the
        /// turns one critical hit gives back.</summary>
        private const float ShippedChance = 0.15f;
        private const int ShippedStacks = 3;
        private const int ShippedTurns = 3;
        private const int ShippedTurnsPerCrit = 1;

        /// <summary>The canonical ceiling of the buff, written out so the claim does not quietly become
        /// "whatever the canon says today". Five is the ability's three plus the two the stacks record
        /// can add — see <see cref="TheCeilingLeavesTheStacksRecordEverythingItCanAdd"/>.</summary>
        private const int CanonCeiling = 5;

        /// <summary>The move a decorator makes on a key, big enough to tell from any figure of the record.</summary>
        private const float ChanceProbe = 0.4f;

        /// <summary>
        /// The ceiling and the strength are pulled from the composition at the moment an effect is built,
        /// so a walk about either has to BE a composition — the same road the game travels. Composition is
        /// idempotent by design, so the assertion guards the premise rather than assuming it.
        /// </summary>
        [TestInitialize]
        public void ComposeTheCanon()
        {
            EffectProviders.ComposeShipped();

            Assert.AreEqual(CanonCeiling, GameServiceProvider.TryGet<IEffectProvider>()?.StackCeilingOf(BuffId),
                "the composition does not answer for the shipped canon, so nothing below is measuring the ceiling");
        }

        [TestMethod]
        public async Task TheBuffCarriesTheCriticalChanceTheRecordNames()
        {
            // The figure itself, at the far end of the road: the record, the parameter set, the factory,
            // the copy every stack is made of. A constant in the ability read the same and answered to
            // nothing — no record could move it and no augment could ever reach it.
            var bearer = new ConditionOwner();
            CritCalculationBuff buff = await LaidBy(Shipped(), bearer);

            Assert.AreEqual(1f + ShippedChance, buff.Value, 0.0001f,
                "the buff no longer carries the critical chance the record names");
            Assert.AreEqual(ShippedStacks, bearer.Effects.Effects.OfType<CritCalculationBuff>().Count(),
                "the cast laid a different number of stacks than the record names");
            Assert.AreEqual(ShippedTurns, buff.Duration, "the buff was laid for other turns than the record names");
        }

        [TestMethod]
        public async Task ADecoratorOnTheChanceKeyMovesWhatTheBuffCarries()
        {
            // What the key being a PARAMETER buys, and the half a constant could never have: the cast
            // reads the decorated value at the moment it lays the buff, so whatever is seated on the key
            // by then is already in the number the bearer gets.
            Ability ability = Shipped();
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                CriticalCalculationCast.Parameters.CriticalChance, Priority.Weak, OperationType.Add, ChanceProbe,
                "Ability_Parameter_Decorator_Test_Crit_Chance", "Test"));

            CritCalculationBuff buff = await LaidBy(ability, new ConditionOwner());

            Assert.AreEqual(1f + ShippedChance + ChanceProbe, buff.Value, 0.0001f,
                "a decorator seated on the chance key never reached the buff the cast lays");
        }

        [TestMethod]
        public async Task ACriticalHitGivesTheBuffTheTurnsTheRecordNames()
        {
            var bearer = new ConditionOwner();
            CritCalculationBuff buff = await LaidBy(Shipped(), bearer);
            Assert.IsTrue(buff.ExtensionBudget >= ShippedTurnsPerCrit,
                "the rules leave no room to extend, so the crit below proves nothing");

            Crit(bearer);

            Assert.AreEqual(ShippedTurns + ShippedTurnsPerCrit, buff.Duration,
                "a critical hit gave the buff other turns than the record names");
        }

        [TestMethod]
        public async Task ADecoratorOnTheExtensionKeyMovesWhatACriticalHitGivesBack()
        {
            // The other key of the pair, and the reason the record's figure had to be read at all: the
            // turn used to be a literal inside the effect, so "a critical hit extends the buff" was a
            // sentence no record could ever change.
            const int Extra = 1;
            Ability ability = Shipped();
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                CriticalCalculationCast.Parameters.AdditionalDurationAmount, Priority.Weak, OperationType.Add, Extra,
                "Ability_Parameter_Decorator_Test_Extension", "Test"));

            var bearer = new ConditionOwner();
            CritCalculationBuff buff = await LaidBy(ability, bearer);
            Assert.IsTrue(buff.ExtensionBudget >= ShippedTurnsPerCrit + Extra,
                "the rules leave no room for the longer extension, so the crit below proves nothing");

            Crit(bearer);

            Assert.AreEqual(ShippedTurns + ShippedTurnsPerCrit + Extra, buff.Duration,
                "a decorator seated on the extension key never reached the buff's own lengthening");
        }

        [TestMethod]
        public void TheBuffNeverClaimsMoreStacksThanTheCanonBalances()
        {
            // The buff joined the canon at А-1b. Before it had a row, an instance kept whatever number it
            // was handed, whoever handed it over and for whatever reason.
            var overreaching = new CritCalculationBuff(ShippedTurns, CanonCeiling + 2, ShippedChance);

            Assert.AreEqual(CanonCeiling, overreaching.MaxStacks,
                "the buff was built claiming more stacks than the canon balances it at");
        }

        [TestMethod]
        public void TheChanceAndTheTurnsAreOneNumberOnBothRoadsThatLayTheBuff()
        {
            // The figures live in two files: the ability's record, which the CAST reads, and the canonical
            // row, which the REGISTRY reads (a grant, an item, anything laying the buff without a cast
            // behind it). Nothing holds the two against each other — the guard over records restating the
            // canon walks AUGMENT records — so raising the chance in one file would leave the other road
            // laying the old number in silence. The ceiling is the one figure allowed to differ, and it
            // has a pin of its own; these two are not.
            IEffect? fromCanon = Canon().CreateEffect(BuffId, RecordProperties.Empty);
            Assert.IsNotNull(fromCanon, "the canon alone builds no buff at all, so nothing below is measured");

            Assert.AreEqual(1f + ShippedChance, ((CritCalculationBuff)fromCanon).Value, 0.0001f,
                "the chance the registry lays and the chance the cast lays came apart");
            Assert.AreEqual(ShippedTurns, fromCanon.Duration,
                "the turns the registry lays and the turns the cast lays came apart");
        }

        [TestMethod]
        public async Task TheCastLaysEveryStackTheBestStacksRecordBuys()
        {
            // The ceiling measured where it is actually paid for: a minted copy at the top of its band,
            // seated, cast. The arithmetic walk below says the numbers add up to five; this one says five
            // stacks reach the bearer — which is the claim the row's number was chosen for, and the one a
            // ceiling written at the ability's own three would have quietly broken.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityAugmentData? record = catalog.Find(StackRecord);
            Assert.IsNotNull(record, $"the shipped data declares no '{StackRecord}'");

            // The BEST rung, minted: the record built from itself carries its worst one (+1), which would
            // lay four stacks and prove nothing about the ceiling.
            AugmentInstance copy = new AugmentMinter(catalog, new DefaultRandomNumberGenerator(seed: 1))
                .Mint(record, Rarity.Legendary);
            IAugment? upgrade = registry.CreateUpgrade(copy);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for a copy of '{StackRecord}'");

            var ability = (Ability)registry.CreateAbility(AbilityId);
            upgrade.Apply(ability);
            Assert.AreEqual(CanonCeiling, (int)ability[AbilityParameter.Stacks],
                "the seated copy no longer asks for the stacks the ceiling was written around");

            var bearer = new ConditionOwner();
            await LaidBy(ability, bearer);

            Assert.AreEqual(CanonCeiling, bearer.Effects.Effects.OfType<CritCalculationBuff>().Count(),
                "the canonical ceiling held back stacks a shipped build had bought");
        }

        [TestMethod]
        public void TheCeilingLeavesTheStacksRecordEverythingItCanAdd()
        {
            // WHY the row says five. The ceiling was written to change nothing a shipped build already
            // had: the cast lays three and the one record that raises the count is worth two at its best
            // rarity, so the ceiling has to sit at their sum. Anything lower is a nerf of that build and
            // anything higher is room nothing can reach — either way the number here has to be looked at.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            AbilityAugmentData? record = catalog.Find(StackRecord);
            Assert.IsNotNull(record, $"the shipped data declares no '{StackRecord}'");

            var ability = (Ability)registry.CreateAbility(AbilityId);
            (float _, float atBest) = record.Ends("amount");

            Assert.AreEqual(CanonCeiling, (int)ability[AbilityParameter.Stacks] + (int)atBest,
                "the ceiling and what a shipped build can ask for came apart — the canonical row has to be looked at");
        }

        /// <summary>The shipped canonical numbers, read through the registry that loads them.</summary>
        private static EffectProvider Canon() => EffectProviders.FromShippedData();

        /// <summary>The shipped ability as the game builds it, off the real files and the real loader.</summary>
        private static Ability Shipped()
        {
            (AbilityProvider registry, _) = ShippedAbilityData.Load();
            return (Ability)registry.CreateAbility(AbilityId);
        }

        /// <summary>One cast of the ability on its owner, and the buff it left there.</summary>
        private static async Task<CritCalculationBuff> LaidBy(Ability ability, ConditionOwner bearer)
        {
            ability.SetOwner(bearer);
            await ability.Execute([bearer], Mock.Of<IBattleField>());

            CritCalculationBuff? buff = bearer.Effects.Effects.OfType<CritCalculationBuff>().FirstOrDefault();
            Assert.IsNotNull(buff, "the cast laid no crit calculation buff at all, so nothing below is measured");
            return buff;
        }

        /// <summary>One attack of the bearer's that critted, as the pipeline reports it.</summary>
        private static void Crit(ConditionOwner bearer) =>
            bearer.CombatEvents.Publish(new AfterAttackEvent(Mock.Of<IAttackContext>(context => context.IsCritical)));
    }
}
