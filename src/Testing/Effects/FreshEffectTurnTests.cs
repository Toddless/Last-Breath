namespace LastBreathTest.Effects
{
    using System;
    using System.Threading.Tasks;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;

    /// <summary>
    /// An effect that lands on a fighter whose turn is ALREADY under way, put there by somebody else.
    ///
    /// <para>The end of that turn is not one of the turns the effect was bought for: the bearer was in
    /// the middle of it when the effect arrived, and it arrived by another hand. A reaction freezing
    /// the fighter who shattered a guard used to burn its whole duration on the tail of the turn he was
    /// still playing — the guard answered and cost him nothing at all.</para>
    ///
    /// <para>The frame around it matters as much as the rule. The bearer's OWN casts still spend the
    /// turn they were cast in, and an effect that lands on a fighter who is not playing spends his next
    /// end of turn as it always did; widening the rule to either of those would hand every effect back
    /// the extra turn the countdown was fixed to take away.</para>
    /// </summary>
    [TestClass]
    public class FreshEffectTurnTests
    {
        private const float Health = 1000f;
        private const float Blow = 100f;
        private const float Potency = 0.5f;
        private const int PinnedTurns = 3;

        [TestMethod]
        public async Task TheFreezeAReactionLaysInTheBreakersOwnTurnCostsHimTheTurnAfter()
        {
            // The case the rule was written for: the shattered Ice Aegis answers in the middle of the
            // breaker's turn. Spending the freeze on the tail of a turn he has already played means he
            // skips nothing whatsoever, and the stage-four payload of the ability does not exist.
            var aegisOwner = Fighter();
            var breaker = Fighter();

            breaker.Effects.TriggerTurnStart();
            await Lay(new FreezeEffect(1), aegisOwner, breaker);
            await breaker.Effects.TriggerTurnEnd();

            Assert.AreEqual(StatusEffects.Freeze, breaker.StatusEffects.GetSkipTurnCause(),
                "the freeze was spent by the turn the breaker was already playing");

            breaker.Effects.TriggerTurnStart();
            await breaker.Effects.TriggerTurnEnd();

            Assert.AreEqual(StatusEffects.None, breaker.StatusEffects.GetSkipTurnCause(), "the freeze cost the breaker a second turn");
            Assert.AreEqual(0, breaker.Effects.Effects.Count, "the freeze outstayed the one turn it was bought for");
        }

        [TestMethod]
        public async Task ADotLaidInsideTheBearersTurnFirstTicksOnHisNextOne()
        {
            // The whole end of turn is sat out, not merely the countdown: a poison a reaction puts on a
            // fighter mid-turn does not get to bite him before he has had a turn to bleed on.
            var caster = Fighter();
            var bearer = Fighter();
            bearer.TakesDamageForReal = true;

            bearer.Effects.TriggerTurnStart();
            await Lay(Poison(PinnedTurns), caster, bearer);
            await bearer.Effects.TriggerTurnEnd();

            Assert.AreEqual(Health, bearer.CurrentHealth, 0.001f, "the poison bit on the turn it landed in");

            bearer.Effects.TriggerTurnStart();
            await bearer.Effects.TriggerTurnEnd();

            Assert.IsTrue(bearer.CurrentHealth < Health, "the poison never bit at all");
        }

        [TestMethod]
        public async Task TheTurnSatOutIsOneTurnAndTheRestAreSpentAsUsual()
        {
            // The hold-back is a debt to ONE end of turn, not a shift of the whole duration: past the
            // turn it landed in the effect counts down like any other, and three turns is three turns.
            var caster = Fighter();
            var bearer = Fighter();

            bearer.Effects.TriggerTurnStart();
            IEffect debuff = await Lay(Buff(PinnedTurns), caster, bearer);
            await bearer.Effects.TriggerTurnEnd();

            Assert.AreEqual(PinnedTurns, debuff.Duration, "the effect spent the turn it landed inside");

            for (int turn = 0; turn < PinnedTurns; turn++)
            {
                bearer.Effects.TriggerTurnStart();
                await bearer.Effects.TriggerTurnEnd();
            }

            Assert.AreEqual(0, bearer.Effects.Effects.Count, "the hold-back moved every turn of the effect instead of the one it landed in");
        }

        [TestMethod]
        public async Task ASelfCastInsideTheOwnTurnStillSpendsThatTurn()
        {
            // The near half of the frame. A buff a fighter puts on himself is his own doing and the turn
            // he cast it in is his to spend — a one-turn self-buff is meant to be gone by the next turn,
            // and holding it back here would hand every blessing its extra turn straight back.
            var bearer = Fighter();

            bearer.Effects.TriggerTurnStart();
            await Lay(Buff(1), bearer, bearer);
            await bearer.Effects.TriggerTurnEnd();

            Assert.AreEqual(0, bearer.Effects.Effects.Count, "a self-cast buff stopped spending the turn it was cast in");
        }

        [TestMethod]
        public async Task WipingTheBearersEffectsMidTurnLeavesHisTurnUnderWay()
        {
            // A boss transformation clears everything the boss carried, and it happens on HIS turn.
            // The wipe is about effects, not about the turn: a reaction answering later in that same
            // turn is still answering inside it, and the freeze it lays is still owed the turn after.
            var aegisOwner = Fighter();
            var boss = Fighter();

            boss.Effects.TriggerTurnStart();
            boss.Effects.RemoveAllEffects();
            await Lay(new FreezeEffect(1), aegisOwner, boss);
            await boss.Effects.TriggerTurnEnd();

            Assert.AreEqual(StatusEffects.Freeze, boss.StatusEffects.GetSkipTurnCause(),
                "clearing the effects mid-turn closed the turn and the freeze burned on the tail of it");
        }

        [TestMethod]
        public async Task AnEffectLandingAfterTheBearerHasPlayedHisWholeTurnIsNotHeldBack()
        {
            // The far edge of the window. The bearer's turn is over — his end of turn closed it — so
            // an effect arriving afterwards is owed nothing and spends his next end of turn. A window
            // that never closes would hand a held-back turn to every effect in the fight.
            var caster = Fighter();
            var bearer = Fighter();

            bearer.Effects.TriggerTurnStart();
            await bearer.Effects.TriggerTurnEnd();

            await Lay(Buff(1), caster, bearer);
            bearer.Effects.TriggerTurnStart();
            await bearer.Effects.TriggerTurnEnd();

            Assert.AreEqual(0, bearer.Effects.Effects.Count, "an effect laid after the bearer's turn was over was held back anyway");
        }

        [TestMethod]
        public async Task ARefreshInsideTheBearersTurnDoesNotBuyTheStandingStackATurnOff()
        {
            // The rule is about effects that LAND, and a re-application at the ceiling lands nothing:
            // it moves a number on a stack that has been living on the bearer's clock since it got
            // there. Holding that stack back would sell a free turn for every repeated cast.
            var caster = Fighter();
            var bearer = Fighter();
            IEffect standing = await Lay(Buff(PinnedTurns), caster, bearer);

            bearer.Effects.TriggerTurnStart();
            await bearer.Effects.TriggerTurnEnd();
            Assert.AreEqual(PinnedTurns - 1, standing.Duration, "the stack did not spend a turn, so the refresh below proves nothing");

            bearer.Effects.TriggerTurnStart();
            await Lay(Buff(PinnedTurns), caster, bearer);
            Assert.AreEqual(PinnedTurns, standing.Duration, "the re-application did not refresh the standing stack");
            await bearer.Effects.TriggerTurnEnd();

            Assert.AreEqual(PinnedTurns - 1, standing.Duration, "the refreshed stack sat out the turn instead of spending it");
        }

        [TestMethod]
        public async Task AnEffectLaidWhileTheBearerIsNotPlayingSpendsHisNextEndOfTurnAsBefore()
        {
            // The far half of the frame: the ordinary cast on an enemy. The bearer is not inside a turn
            // when it lands, so nothing is owed to him and his next end of turn is the effect's first.
            var caster = Fighter();
            var bearer = Fighter();

            caster.Effects.TriggerTurnStart();
            await Lay(Buff(1), caster, bearer);
            await caster.Effects.TriggerTurnEnd();

            bearer.Effects.TriggerTurnStart();
            await bearer.Effects.TriggerTurnEnd();

            Assert.AreEqual(0, bearer.Effects.Effects.Count, "an ordinary debuff stopped spending the bearer's own turn");
        }

        /// <summary>A fighter with pools to lose: the walks measure a tick on his health, and a caster
        /// whose ticks are cancelled with him has to be alive to deal them.</summary>
        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Health);
            fighter.CurrentHealth = Health;
            return fighter;
        }

        /// <summary>An effect with nothing to it but a duration.</summary>
        private static IEffect Buff(int turns) => new CritCalculationBuff(turns, maxStacks: 1, value: 1.1f);

        private static IEffect Poison(int turns) =>
            new DamageOverTurnEffect(turns, StatusEffects.Poison, DamageOverTurnEffect.NoCeilingOfItsOwn, Potency);

        /// <summary>The one road every walk here lays an effect by: a cast of the caster's landing on
        /// the bearer, carrying a blow for whatever feeds on one.</summary>
        private static async Task<IEffect> Lay(IEffect effect, IFightable caster, IFightable bearer)
        {
            await effect.Apply(new EffectApplyingContext
            {
                Caster = caster,
                Target = bearer,
                Source = $"Test_{Guid.NewGuid()}",
                Damage = DamageSnapshot.Of(DamageType.Physical, Blow)
            });

            return effect;
        }
    }
}
