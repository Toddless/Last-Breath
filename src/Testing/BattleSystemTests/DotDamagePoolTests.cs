namespace LastBreathTest.BattleSystemTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    /// <summary>
    /// What a damage-over-time tick is taken from. A blow is not one number — it is a split by type, and
    /// each damaging status feeds on a different part of it: poison on the whole of it, burning on its
    /// fire, bleeding on its physical. On top of the share the effect is authored with stands the
    /// caster's own stat for that kind, and both are read ONCE, when the stack is laid: what the pool
    /// was worth at that moment is what every tick of that stack carries.
    /// </summary>
    [TestClass]
    public class DotDamagePoolTests
    {
        private const int Turns = 3;

        /// <summary>Half the pool per tick — a share small enough that a doubled pool cannot be mistaken
        /// for a doubled share.</summary>
        private const float Share = 0.5f;

        private const float Physical = 60f;
        private const float Fire = 40f;

        [TestMethod]
        public async Task PoisonFeedsOnEveryComponentOfTheBlow()
        {
            var caster = new ConditionOwner();
            var poison = new DamageOverTurnEffect(Turns, StatusEffects.Poison, percentFromDamage: Share);

            await poison.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));

            Assert.AreEqual((Physical + Fire) * Share, poison.DamagePerTick, 0.0001f,
                "poison is the whole blow — a component of it was left out of the pool");
        }

        [TestMethod]
        public async Task BurningFeedsOnTheFireOfTheBlowAlone()
        {
            var caster = new ConditionOwner();
            var burning = new DamageOverTurnEffect(Turns, StatusEffects.Burning, percentFromDamage: Share);

            await burning.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));

            Assert.AreEqual(Fire * Share, burning.DamagePerTick, 0.0001f,
                "the burn took the physical part of the blow with it");
        }

        [TestMethod]
        public async Task BleedingFeedsOnThePhysicalOfTheBlowAlone()
        {
            var caster = new ConditionOwner();
            var bleed = new DamageOverTurnEffect(Turns, StatusEffects.Bleed, percentFromDamage: Share);

            await bleed.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));

            Assert.AreEqual(Physical * Share, bleed.DamagePerTick, 0.0001f,
                "the bleed drank the elemental part of the blow");
        }

        [TestMethod]
        public async Task ATypelessDotStillFeedsOnTheWholeBlow()
        {
            // Nothing in the game lays one today, but the class takes StatusEffects.None and the
            // fallback has to be the reading that loses nothing.
            var caster = new ConditionOwner();
            var dot = new DamageOverTurnEffect(Turns, percentFromDamage: Share);

            await dot.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));

            Assert.AreEqual((Physical + Fire) * Share, dot.DamagePerTick, 0.0001f, "a typeless tick lost part of the blow");
        }

        [TestMethod]
        public async Task ABurnWithNothingToFeedOnIsNotLaidAtAll()
        {
            // A stack that would tick for nothing is refused outright. It is not a cosmetic question:
            // an empty stack holds a place under the ceiling, and the ceiling evicts the OLDEST stack of
            // the kind — so a fireless blow could put out a burn somebody else's fire was still doing.
            var caster = new ConditionOwner();
            var victim = new ConditionOwner();
            var burning = new DamageOverTurnEffect(Turns, StatusEffects.Burning, percentFromDamage: Share);

            await burning.Apply(Blow(caster, victim, (DamageType.Physical, Physical)));

            Assert.AreEqual(0f, burning.DamagePerTick, 0.0001f, "a fireless blow fed a burn");
            Assert.AreEqual(0, victim.Effects.GetBy(effect => effect.Status == StatusEffects.Burning).Count(),
                "a stack with nothing to tick with was laid all the same");
        }

        [TestMethod]
        public async Task ALivingStackIsNotEvictedByOneWithNothingToTickWith()
        {
            // The consequence the refusal exists for, walked on the road it actually happens on: a
            // MULTI-stack effect at its ceiling evicts the oldest stack unconditionally — no comparison
            // of strength stands between them — so a fireless blow could put out a burn that was working.
            var caster = new ConditionOwner();
            var victim = new ConditionOwner();
            var eldest = new DamageOverTurnEffect(Turns, StatusEffects.Burning, maxStacks: 2, percentFromDamage: Share);
            var younger = new DamageOverTurnEffect(Turns, StatusEffects.Burning, maxStacks: 2, percentFromDamage: Share);
            var starved = new DamageOverTurnEffect(Turns, StatusEffects.Burning, maxStacks: 2, percentFromDamage: Share);

            // The road is chosen by the ceiling: at one stack the eviction never happens and a strength
            // comparison guards the standing burn instead, which is not what is asked here.
            Assert.AreEqual(2, starved.MaxStacks, "the canon caps this burn lower than the walk needs");

            await eldest.Apply(Blow(caster, victim, (DamageType.Fire, Fire)));
            await younger.Apply(Blow(caster, victim, (DamageType.Fire, Fire)));
            await starved.Apply(Blow(caster, victim, (DamageType.Physical, Physical)));

            List<IEffect> burns = [.. victim.Effects.GetBy(effect => effect.Status == StatusEffects.Burning)];
            Assert.AreEqual(2, burns.Count, "the ceiling holds two burns and the victim carries a different number");
            Assert.IsTrue(burns.Contains(eldest), "an empty stack evicted the oldest living burn");
            Assert.IsTrue(burns.Contains(younger), "the younger living burn is gone");
        }

        [TestMethod]
        public async Task ASourceThatSpeaksForTheWholeHitPoolsAllOfIt()
        {
            // The lever a data record pulls (poolFromWholeHit): the burn is a share of ANY blow, so the
            // component of its own kind is not what it asks for.
            var caster = new ConditionOwner();
            var victim = new ConditionOwner();
            var burning = new DamageOverTurnEffect(Turns, StatusEffects.Burning, percentFromDamage: Share);

            await burning.Apply(Blow(caster, victim, (DamageType.Physical, Physical)) with { PoolFromWholeHit = true });

            Assert.AreEqual(Physical * Share, burning.DamagePerTick, 0.0001f,
                "the whole-hit lever was ignored and the burn went looking for fire");
        }

        [TestMethod]
        public async Task TheCastersOwnMultiplierRaisesThePoolOfItsKind()
        {
            var caster = new ConditionOwner();
            caster.SetMaximum(EntityParameter.BurningDamageMultiplier, 0.5f);
            var burning = new DamageOverTurnEffect(Turns, StatusEffects.Burning, percentFromDamage: Share);

            await burning.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));

            Assert.AreEqual(Fire * 1.5f * Share, burning.DamagePerTick, 0.0001f,
                "the caster's burning multiplier never reached the pool");
        }

        [TestMethod]
        public async Task TheCastersPoisonMultiplierRaisesTheWholeBlowItPoolsFrom()
        {
            var caster = new ConditionOwner();
            caster.SetMaximum(EntityParameter.PoisonDamageMultiplier, 0.5f);
            var poison = new DamageOverTurnEffect(Turns, StatusEffects.Poison, percentFromDamage: Share);

            await poison.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));

            Assert.AreEqual((Physical + Fire) * 1.5f * Share, poison.DamagePerTick, 0.0001f,
                "the caster's poison multiplier never reached the pool");
        }

        [TestMethod]
        public async Task AMultiplierOfOneKindLeavesTheOthersAlone()
        {
            var caster = new ConditionOwner();
            caster.SetMaximum(EntityParameter.PoisonDamageMultiplier, 1f);
            caster.SetMaximum(EntityParameter.BleedDamageMultiplier, 1f);
            var burning = new DamageOverTurnEffect(Turns, StatusEffects.Burning, percentFromDamage: Share);

            await burning.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));

            Assert.AreEqual(Fire * Share, burning.DamagePerTick, 0.0001f,
                "a poison or bleed stat moved a burn — the kinds are not told apart");
        }

        [TestMethod]
        public async Task TheStackKeepsTheCasterItWasLaidBy()
        {
            // The snapshot: the pool and the stat behind it are frozen at application, so a caster who
            // grows stronger afterwards moves his NEXT stack and not the one already standing.
            var caster = new ConditionOwner();
            var poison = new DamageOverTurnEffect(Turns, StatusEffects.Poison, percentFromDamage: Share);

            await poison.Apply(Blow(caster, new ConditionOwner(), (DamageType.Physical, Physical), (DamageType.Fire, Fire)));
            float laid = poison.DamagePerTick;
            caster.SetMaximum(EntityParameter.PoisonDamageMultiplier, 3f);

            Assert.AreEqual(laid, poison.DamagePerTick, 0.0001f, "a standing stack was recalculated behind its own back");
        }

        /// <summary>A landed blow of the given split, handed over the way a feeder hands it.</summary>
        private static EffectApplyingContext Blow(ConditionOwner caster, ConditionOwner victim, params (DamageType Type, float Amount)[] split) =>
            new()
            {
                Caster = caster,
                Target = victim,
                Source = "test",
                Damage = DamageSnapshot.From(split.ToDictionary(part => part.Type, part => part.Amount))
            };
    }
}
