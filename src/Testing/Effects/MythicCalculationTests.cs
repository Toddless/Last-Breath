namespace LastBreathTest.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Abilities.Riders;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Events;
    using Moq;

    /// <summary>
    /// The first mythic record of the catalog, measured where its whole promise lives: while the buff
    /// stands, every critical hit its bearer scores gives ONE more turn to EVERY buff he carries. The
    /// Crit Calculation buff already did that for itself alone; this generalises it to the standing
    /// arrangement, which is what makes a mythic worth being one.
    ///
    /// Three things have to be true together, and the middle one is what the design rests on: the crit
    /// extends, the loop STOPS, and nothing extends when the buff is not there. A crit build lands
    /// dozens of criticals in a fight, so an extension without a ceiling is not a strong augment but a
    /// buff that never ends — and the ceiling is not this effect's own invention. It is the extension
    /// budget every extender in the game is charged to, carried per instance, which is why the walk
    /// reads the budget off the effect rather than naming a number.
    /// </summary>
    [TestClass]
    public class MythicCalculationTests
    {
        private const int BuffTurns = 3;

        [TestMethod]
        public async Task ACritExtendsEveryBuffTheBearerCarriesByOneAndLeavesHisDebuffsAlone()
        {
            // What the design line says, taken literally on a bearer wearing more than one thing. The
            // debuff is beside them on purpose: "each of your buffs" is a promise about the arrangement,
            // and an extension that walked the whole list would be lengthening what is hurting him.
            var bearer = new ConditionOwner();
            await Standing(bearer);

            var buff = new AccuracyBuff(BuffTurns, maxStacks: 1, value: 0.15f);
            var debuff = new Clumsiness(BuffTurns, maxStacks: 1, value: 0.15f);
            await Lay(buff, bearer);
            await Lay(debuff, bearer);

            Crit(bearer);

            Assert.AreEqual(BuffTurns + 1, buff.Duration, "a crit under the calculation left the bearer's buff where it was");
            Assert.AreEqual(BuffTurns, debuff.Duration, "a crit under the calculation lengthened a debuff the bearer is suffering");
        }

        [TestMethod]
        public async Task AnAttackThatDidNotCritExtendsNothing()
        {
            // The other half of the trigger. The effect listens to every attack the bearer resolves, so
            // reading the flag is the whole of what makes this a CRIT augment rather than a hit one.
            var bearer = new ConditionOwner();
            await Standing(bearer);
            var buff = new AccuracyBuff(BuffTurns, maxStacks: 1, value: 0.15f);
            await Lay(buff, bearer);

            bearer.CombatEvents.Publish(new AfterAttackEvent(Mock.Of<IAttackContext>()));

            Assert.AreEqual(BuffTurns, buff.Duration, "an ordinary attack extended the bearer's buff");
        }

        [TestMethod]
        public async Task TheCritLoopRunsIntoTheExtensionBudgetAndStopsThere()
        {
            // The ceiling, measured from both sides of the last crit that still counts. A crit build
            // lands far more criticals than the budget allows, and the extension every one of them asks
            // for is charged to the same instance — so the buff ends up at its budget and stays there
            // however long the fight goes on.
            var bearer = new ConditionOwner();
            await Standing(bearer);
            var buff = new AccuracyBuff(BuffTurns, maxStacks: 1, value: 0.15f);
            await Lay(buff, bearer);
            int budget = buff.ExtensionBudget;
            Assert.IsTrue(budget > 1, "the rules leave no room to extend, so the loop below proves nothing");

            for (int crit = 0; crit < budget - 1; crit++) Crit(bearer);
            Assert.AreEqual(BuffTurns + budget - 1, buff.Duration, "the crits before the ceiling did not each add a turn");

            Crit(bearer);
            Assert.AreEqual(BuffTurns + budget, buff.Duration, "the crit that spends the last of the budget was refused early");

            for (int crit = 0; crit < 10; crit++) Crit(bearer);
            Assert.AreEqual(BuffTurns + budget, buff.Duration, "the crit loop went on extending past the budget");
        }

        [TestMethod]
        public async Task WithoutTheCalculationACritExtendsNothingAtAll()
        {
            // The claim the other three rest on: the extension belongs to this effect and to nothing
            // else in the pipeline. Same bearer, same buff, same crit — and the calculation not laid.
            var bearer = new ConditionOwner();
            var buff = new AccuracyBuff(BuffTurns, maxStacks: 1, value: 0.15f);
            await Lay(buff, bearer);

            Crit(bearer);

            Assert.AreEqual(BuffTurns, buff.Duration, "a crit lengthened a buff with no calculation standing");
        }

        [TestMethod]
        public async Task TheCalculationExtendsItselfAlongWithTheRest()
        {
            // It is a buff of the bearer's like any other, so it is in the arrangement it lengthens —
            // the same reading its predecessor had. Worth pinning because the alternative (skipping
            // itself) is one plausible line away and would quietly halve the mythic's lifetime.
            var bearer = new ConditionOwner();
            MythicCalculationEffect calculation = await Standing(bearer);

            Crit(bearer);

            Assert.AreEqual(BuffTurns + 1, calculation.Duration, "the calculation did not extend itself");
        }

        [TestMethod]
        public async Task TheCalculationLeavesWithTheHostBuffWhenItRunsOut()
        {
            // The augment is a reading OF the Crit Calculation, so it cannot stand where the buff it
            // reads does not. Pinned on the host running out of turns rather than being taken off,
            // because expiry is the road nobody calls: the turn end removes the buff itself.
            var bearer = new ConditionOwner();
            MythicCalculationEffect calculation = await Standing(bearer);
            var host = new CritCalculationBuff(duration: 1, maxStacks: 1, value: 0.15f);
            await Lay(host, bearer);

            await bearer.Effects.TriggerTurnEnd();
            await bearer.Effects.TriggerTurnEnd();

            Assert.IsFalse(bearer.Effects.Effects.Contains(host), "the host buff outlived its own duration, so nothing below is measured");
            Assert.IsTrue(calculation.Duration > 0, "the calculation ran out on its own, so its leaving proves nothing about the host");
            Assert.IsFalse(bearer.Effects.Effects.Contains(calculation), "the host buff ran out and the mythic reading of it stayed standing");
        }

        [TestMethod]
        public async Task TheCalculationLeavesWithTheHostBuffWhenItIsDispelled()
        {
            // The enemy's counter, and the reason the tie is worth having: the mythic is beyond a weak
            // dispel itself (see EffectPowerTests), so taking the host is how it is answered at all.
            var bearer = new ConditionOwner();
            MythicCalculationEffect calculation = await Standing(bearer);
            await Lay(new CritCalculationBuff(duration: 3, maxStacks: 1, value: 0.15f), bearer);

            bearer.Effects.Dispel(EffectPower.Weak, DispelScope.Target);

            Assert.IsFalse(bearer.Effects.Effects.Contains(calculation), "the host buff was dispelled and the mythic reading of it stayed standing");
        }

        [TestMethod]
        public async Task OneStackOfTheHostGoingOutIsNotTheHostLeaving()
        {
            // The count is the whole of it: the Crit Calculation is laid in stacks, and a mythic that
            // read the first removal as "the buff is gone" would end on the bearer's strongest turn.
            var bearer = new ConditionOwner();
            MythicCalculationEffect calculation = await Standing(bearer);
            var first = new CritCalculationBuff(duration: 3, maxStacks: 2, value: 0.15f);
            var second = new CritCalculationBuff(duration: 3, maxStacks: 2, value: 0.15f);
            await Lay(first, bearer);
            await Lay(second, bearer);
            Assert.AreEqual(2, bearer.Effects.Effects.OfType<CritCalculationBuff>().Count(), "the host did not stack, so the walk proves nothing");

            first.Remove();

            Assert.IsTrue(bearer.Effects.Effects.Contains(calculation), "one stack of the host went out and took the mythic with it");

            second.Remove();

            Assert.IsFalse(bearer.Effects.Effects.Contains(calculation), "the last stack of the host went out and the mythic stayed");
        }

        /// <summary>The calculation laid on the bearer the way a cast lays it — through the activation
        /// rider, so what the walks read is an effect that travelled the road the augment installs.</summary>
        private static async Task<MythicCalculationEffect> Standing(ConditionOwner bearer)
        {
            var calculation = new MythicCalculationEffect(BuffTurns, maxStacks: 1);
            await new AbilityBuffActivationRider(calculation).Apply(Activation(bearer));

            MythicCalculationEffect? laid = bearer.Effects.Effects.OfType<MythicCalculationEffect>().FirstOrDefault();
            Assert.IsNotNull(laid, "the rider laid no calculation at all, so the crits prove nothing");
            Assert.AreEqual(BuffTurns, laid.Duration, "the calculation landed on something other than its own duration");
            return laid;
        }

        /// <summary>One attack of the bearer's that critted, as the pipeline reports it.</summary>
        private static void Crit(ConditionOwner bearer) =>
            bearer.CombatEvents.Publish(new AfterAttackEvent(Mock.Of<IAttackContext>(context => context.IsCritical)));

        private static async Task Lay(IEffect effect, ConditionOwner bearer) =>
            await effect.Apply(new EffectApplyingContext
            {
                Caster = bearer,
                Target = bearer,
                Source = "Test_Mythic_Calculation",
                Damage = default
            });

        /// <summary>A cast of the bearer's, with nothing in it the rider does not read.</summary>
        private static IAbilityActivationContext Activation(ConditionOwner bearer)
        {
            var ability = new Mock<IAbility>();
            ability.Setup(cast => cast.Effectiveness).Returns(1f);

            var context = new Mock<IAbilityActivationContext>();
            context.Setup(cast => cast.Caster).Returns(bearer);
            context.Setup(cast => cast.Ability).Returns(ability.Object);
            return context.Object;
        }
    }
}
