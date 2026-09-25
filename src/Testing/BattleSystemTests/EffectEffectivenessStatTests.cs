namespace LastBreathTest.BattleSystemTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers;
    using Moq;

    /// <summary>
    /// Effectiveness as a stat of the ENTITY, beside the one the cast owns. A cast says how hard what it
    /// lays lands; the owner's knobs say how hard everything he applies lands, and the two meet on the
    /// instance — the cast stamps its figure, the caster-side application pipeline multiplies it, and every
    /// number of the effect is read through the product.
    ///
    /// What separates the entity stat from the cast's own: it belongs to whoever APPLIES the effect rather
    /// than to a record, so it also reaches what no cast laid at all — a passive, an item grant, a boss
    /// stage — where an augment has nothing to hold on to.
    /// </summary>
    [TestClass]
    public class EffectEffectivenessStatTests
    {
        /// <summary>The figure the effects below are authored with, a cast that lands a third harder, and
        /// the shares the owner's knobs are written at.</summary>
        private const float Authored = 0.15f;
        private const float Strong = 1.35f;
        private const float Fifth = 0.2f;
        private const float Half = 0.5f;
        private const float Tolerance = 0.0001f;

        /// <summary>One blow the damaging stacks below feed on.</summary>
        private const float Blow = 100f;

        [TestMethod]
        public async Task TheOwnersKnobMultipliesWhatTheCastStamped()
        {
            // The two axes compose rather than replace one another: an augmented cast at 1.35 on an owner
            // carrying +20% lands at 1.62. A stat that OVERWROTE the cast's figure would quietly delete
            // every augment bought on the effectiveness axis.
            var buff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);
            ConditionOwner bearer = Bearer((ContextParameter.EffectEffectivenessScale, Fifth));

            await buff.Apply(Laying(bearer, Strong));

            Assert.AreEqual(Strong * (1f + Fifth), buff.Effectiveness, Tolerance,
                "the owner's knob replaced the cast's effectiveness instead of multiplying it");
            Assert.AreEqual(Authored * Strong * (1f + Fifth), buff.Value, Tolerance,
                "the product of the two axes never reached the figure the buff carries");
        }

        [TestMethod]
        public async Task AnEffectNoCastLaidIsRaisedByTheOwnersKnobAllTheSame()
        {
            // The difference from the augment axis, and the reason the stat lives on the entity: a passive,
            // an item grant and a boss stage hand over no effectiveness at all, and an augment can reach
            // none of them. The owner's knob is asked at the moment of application and reaches all three.
            var buff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);
            ConditionOwner bearer = Bearer((ContextParameter.EffectEffectivenessScale, Fifth));

            await buff.Apply(Laying(bearer));

            Assert.AreEqual(1f + Fifth, buff.Effectiveness, Tolerance,
                "an effect laid by nothing in particular stayed at one, so the stat is unreachable without a cast");
            Assert.AreEqual(Authored * (1f + Fifth), buff.Value, Tolerance,
                "the stat never reached the figure of an effect no cast laid");
        }

        [TestMethod]
        public async Task AKindKnobReachesOnlyItsOwnKind()
        {
            // A buff knob raising the debuffs its owner puts on somebody else is the whole point of having
            // kinds at all. Both directions are asked, because a classification read backwards is green in
            // one of them.
            ConditionOwner buffs = Bearer((ContextParameter.BuffEffectivenessScale, Fifth));
            var raisedBuff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);
            var untouchedDebuff = new Clumsiness(duration: 3, maxStacks: 5, value: Authored);

            await raisedBuff.Apply(Laying(buffs));
            await untouchedDebuff.Apply(Laying(buffs));

            Assert.AreEqual(1f + Fifth, raisedBuff.Effectiveness, Tolerance, "the buff knob never reached a buff");
            Assert.AreEqual(1f, untouchedDebuff.Effectiveness, Tolerance, "the buff knob reached a debuff");

            ConditionOwner debuffs = Bearer((ContextParameter.DebuffEffectivenessScale, Fifth));
            var raisedDebuff = new Clumsiness(duration: 3, maxStacks: 5, value: Authored);
            var untouchedBuff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);

            await raisedDebuff.Apply(Laying(debuffs));
            await untouchedBuff.Apply(Laying(debuffs));

            Assert.AreEqual(1f + Fifth, raisedDebuff.Effectiveness, Tolerance, "the debuff knob never reached a debuff");
            Assert.AreEqual(1f, untouchedBuff.Effectiveness, Tolerance, "the debuff knob reached a buff");
        }

        [TestMethod]
        public async Task ADamagingEffectIsItsOwnKindAndNoneOfTheOtherTwo()
        {
            // A stack that ticks damage is classified by what it DOES and not by the harmful flag: it
            // belongs to the damaging kind, and neither the buff knob nor the debuff knob may claim it.
            // The tick is derived before the pipeline ever runs, so the axis has to reach it there or the
            // knob a damage build buys moves nothing at all.
            float plain = await TickOf(Bearer());

            Assert.AreEqual(plain * (1f + Fifth),
                await TickOf(Bearer((ContextParameter.DamagingEffectEffectivenessScale, Fifth))), Tolerance,
                "the damaging knob never reached the tick");
            Assert.AreEqual(plain, await TickOf(Bearer((ContextParameter.DebuffEffectivenessScale, Fifth))), Tolerance,
                "the debuff knob claimed a damaging stack");
            Assert.AreEqual(plain, await TickOf(Bearer((ContextParameter.BuffEffectivenessScale, Fifth))), Tolerance,
                "the buff knob claimed a damaging stack");
        }

        [TestMethod]
        public async Task TheKnobOverEverythingAndTheKindKnobBothRun()
        {
            // Two lines, two mutators, one after the other over the same instance — the way any two context
            // knobs of an owner compose. A rule letting the kind knob shadow the general one (or the other
            // way round) would make half of a build's allocation silently free.
            var buff = new CritMitigationEffect(duration: 3, maxStacks: 3, value: Authored);
            ConditionOwner bearer = Bearer(
                (ContextParameter.EffectEffectivenessScale, Fifth),
                (ContextParameter.BuffEffectivenessScale, Half));

            await buff.Apply(Laying(bearer));

            Assert.AreEqual((1f + Fifth) * (1f + Half), buff.Effectiveness, Tolerance,
                "one of the two knobs standing on the same effect did nothing");
        }

        [TestMethod]
        public async Task NeitherDurationNorStacksIsMovedByTheAxis()
        {
            // The boundary of the concept, and the same one the cast's own effectiveness is drawn at. An
            // axis that also stretched durations would be a second duration knob nobody authored — and one
            // no extension budget is charged for.
            const int duration = 3;
            const int maxStacks = 5;
            var debuff = new Clumsiness(duration, maxStacks, value: Authored);
            ConditionOwner bearer = Bearer(
                (ContextParameter.EffectEffectivenessScale, Half),
                (ContextParameter.DebuffEffectivenessScale, Half));

            await debuff.Apply(Laying(bearer, Strong));

            Assert.AreEqual(duration, debuff.Duration, "the effectiveness axis stretched the duration");
            Assert.AreEqual(maxStacks, debuff.MaxStacks, "the effectiveness axis moved the stack count");
        }

        [TestMethod]
        public async Task TheGainedAndLostSharesAreScaledInTheirOwnFormsAndTheLossStopsAtNothing()
        {
            // The three shapes a figure is read in have to survive the second axis exactly as they survive
            // the first: a share GAINED comes out above one, a share LOST below it, and a loss the knobs
            // have driven past the whole of the parameter takes all of it rather than turning the
            // multiplier negative.
            var gained = new ArmorBuffEffect(duration: 3, maxStacks: 3, value: Authored);
            await gained.Apply(Laying(Bearer((ContextParameter.EffectEffectivenessScale, Fifth))));
            Assert.AreEqual(1f + (Authored * (1f + Fifth)), gained.Value, Tolerance,
                "a gained share was reshaped on its way through the owner's knob");

            var lost = new Clumsiness(duration: 3, maxStacks: 5, value: Authored);
            await lost.Apply(Laying(Bearer((ContextParameter.EffectEffectivenessScale, Fifth))));
            Assert.AreEqual(1f - (Authored * (1f + Fifth)), lost.Value, Tolerance,
                "a lost share was inverted before it was scaled, so the debuff moved the wrong way");

            var whole = new WitheringCurseEffect(duration: 3, maxStacks: 1, value: 0.8f);
            await whole.Apply(Laying(Bearer((ContextParameter.EffectEffectivenessScale, 0.3f))));
            Assert.AreEqual(0f, whole.Value, Tolerance, "the loss went past everything there was to lose");
        }

        [TestMethod]
        public async Task AChainOfEffectsAnswersEachKnobOnceAndByTheKindOfEachLink()
        {
            // A coating is a BUFF whose content is the poison it lays, and the poison is a damaging effect
            // applied by the same owner — so it runs his knobs itself. What the coating hands on is the
            // CAST's claim and not what the coating grew to under those knobs: hand over the grown figure
            // and the chain pays the general knob twice (1.2 becomes 1.44), and pays it by the PARENT's
            // kind — a knob bought for one's own buffs would be raising the enemy's poison.
            float plain = await CoatedPoisonTick();

            Assert.AreEqual(plain * (1f + Fifth),
                await CoatedPoisonTick((ContextParameter.EffectEffectivenessScale, Fifth)), Tolerance,
                "the knob over everything was paid more than once along the chain");
            Assert.AreEqual(plain,
                await CoatedPoisonTick((ContextParameter.BuffEffectivenessScale, Fifth)), Tolerance,
                "the buff knob reached the poison a buff laid — the child took its parent's kind");
            Assert.AreEqual(plain * (1f + Fifth),
                await CoatedPoisonTick((ContextParameter.DamagingEffectEffectivenessScale, Fifth)), Tolerance,
                "the damaging knob moved the laid poison by something other than once");
        }

        /// <summary>What the poison of a coating ticks with: the coating is laid on the bearer by a cast,
        /// the bearer lands one blow, and the stack the coating put on his victim is read.</summary>
        private static async Task<float> CoatedPoisonTick(params (ContextParameter Knob, float Share)[] knobs)
        {
            ConditionOwner bearer = Bearer(knobs);
            var victim = new ConditionOwner();
            var coating = new PoisonCoatingEffect(duration: 3, maxStacks: 1, poisonDuration: 3, poisonDamagePercent: Authored);

            await coating.Apply(Laying(bearer, Strong));
            LandBlowOn(bearer, victim);

            IEffect? poison = victim.Effects.GetBy(effect => effect is IDamageOverTurnEffect).FirstOrDefault();
            Assert.IsNotNull(poison, "the coating laid no poison at all, so the walk measures nothing");

            return ((IDamageOverTurnEffect)poison).DamagePerTick;
        }

        /// <summary>One landed blow as the fighters announce it — the coating listens on the attacker's
        /// own bus and everything it lays hangs off what that blow dealt.</summary>
        private static void LandBlowOn(ConditionOwner attacker, ConditionOwner victim)
        {
            var landed = new Mock<IAttackContext>();
            landed.Setup(context => context.Result).Returns(AttackResults.Succeed);
            landed.Setup(context => context.Target).Returns(victim);
            landed.Setup(context => context.FinalDamage).Returns(DamageSnapshot.Of(DamageType.Physical, Blow));

            attacker.CombatEvents.Publish(new AfterAttackEvent(landed.Object));
        }

        /// <summary>What one stack of poison ticks with when the bearer's knobs have had their say.</summary>
        private static async Task<float> TickOf(ConditionOwner bearer)
        {
            var poison = new DamageOverTurnEffect(
                duration: 3, StatusEffects.Poison, DamageOverTurnEffect.NoCeilingOfItsOwn, percentFromDamage: Authored);

            await poison.Apply(Laying(bearer, Strong, DamageSnapshot.Of(DamageType.Physical, Blow)));

            return poison.DamagePerTick;
        }

        /// <summary>A fighter wearing the context lines named, each written as a share the way a tree node
        /// or an item line writes one.</summary>
        private static ConditionOwner Bearer(params (ContextParameter Knob, float Share)[] knobs)
        {
            var bearer = new ConditionOwner();
            foreach ((ContextParameter knob, float share) in knobs)
                new ContextModifierEntry(knob, ModifierValueType.Increase, share).Attach(bearer);

            return bearer;
        }

        /// <summary>An application on a fighter, with the effectiveness of the cast that lays it — one when
        /// nothing laid it but the bearer himself.</summary>
        private static EffectApplyingContext Laying(ConditionOwner bearer, float effectiveness = 1f, DamageSnapshot blow = default) =>
            new() { Caster = bearer, Target = bearer, Source = "test", Effectiveness = effectiveness, Damage = blow };
    }
}
