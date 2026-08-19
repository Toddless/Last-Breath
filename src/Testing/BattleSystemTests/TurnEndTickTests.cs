namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>
    /// The bearer's end of turn, and the two things about it that used to be nobody's to rely on.
    ///
    /// <para><b>It can be waited for.</b> The tick was an <c>async void</c>: it handed control back at
    /// its first await, so everything the fighter does to close his turn — the recovery, the three
    /// announcements — ran BEFORE the damage-over-time damage landed, and no caller up to the turn loop
    /// could wait for it even in principle. A tick that kills would then kill after the turn had been
    /// declared over, past the gate the presentation waits on.</para>
    ///
    /// <para><b>It happens in one order.</b> Every effect counts itself down and banks what it means to
    /// tick; only then is the whole tick dealt, in one blow per status per caster. A poison on its last
    /// turn still poisons, because the tick is booked before the turn is spent.</para>
    /// </summary>
    [TestClass]
    public class TurnEndTickTests
    {
        private const float Blow = 100f;
        private const float Potency = 0.5f;

        [TestMethod]
        public async Task TheTickIsDealtBeforeTheEndOfTurnIsOver()
        {
            // What "awaitable" is worth. The caller holds the task; when it comes back the damage has
            // landed. Under async void the assertion below read the health as it was before the tick.
            var bearer = new Fighter();
            await Poison(bearer, turns: 3);
            float before = bearer.Damage;

            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(0f, before, "the poison ticked before the turn ended at all");
            Assert.AreEqual(Blow * Potency, bearer.Damage, 0.001f,
                "the end of turn came back with its damage still on the way");
        }

        [TestMethod]
        public async Task TheLastTurnOfAPoisonStillPoisons()
        {
            // The order, where it shows. The stack has one turn left: it books its tick and then spends
            // the turn. Counting down first would leave it expired with nothing banked, and the player
            // would be paying for a turn of poison he never gets.
            var bearer = new Fighter();
            IEffect stack = await Poison(bearer, turns: 1);

            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(Blow * Potency, bearer.Damage, 0.001f, "the poison spent its last turn without ticking on it");
            Assert.AreEqual(0, stack.Duration, "the stack did not spend the turn it ticked on");
        }

        [TestMethod]
        public async Task EveryTurnDealsExactlyOneTickAndNoMore()
        {
            // The tally is emptied by the tick that paid it out. A banked tick surviving its own turn
            // would compound: every turn would deal that turn's poison and every turn before it.
            var bearer = new Fighter();
            await Poison(bearer, turns: 3);

            await bearer.Object.Effects.TriggerTurnEnd();
            await bearer.Object.Effects.TriggerTurnEnd();
            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(Blow * Potency * 3, bearer.Damage, 0.001f,
                "three turns of one stack came to something other than three ticks");
        }

        /// <summary>One poison stack on the bearer, ticking a share of a blow.</summary>
        private static async Task<IEffect> Poison(Fighter bearer, int turns)
        {
            var stack = new DamageOverTurnEffect(turns, StatusEffects.Poison, percentFromDamage: Potency);
            await stack.Apply(new EffectApplyingContext
            {
                Caster = bearer.Object,
                Target = bearer.Object,
                Source = $"Test_{Guid.NewGuid()}",
                Damage = DamageSnapshot.Of(DamageType.Physical, Blow)
            });

            return stack;
        }

        /// <summary>A fightable with real effects that records what it was dealt: the tick's damage is
        /// the whole of what these walks measure, and mitigation is somebody else's subject.</summary>
        private sealed class Fighter
        {
            private readonly Mock<IFightable> _mock = new();

            public Fighter()
            {
                var effects = new EffectsComponent(_mock.Object);
                _mock.Setup(fighter => fighter.InstanceId).Returns(Guid.NewGuid().ToString());
                _mock.Setup(fighter => fighter.IsAlive).Returns(true);
                _mock.Setup(fighter => fighter.Effects).Returns(effects);
                _mock.Setup(fighter => fighter.ModifierHandler).Returns(new ModifierHandlerComponent());
                _mock.Setup(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                _mock.Setup(fighter => fighter.Parameters).Returns(Mock.Of<IEntityParametersComponent>());
                _mock.Setup(fighter => fighter.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
                _mock.Setup(fighter => fighter.TakeDamage(It.IsAny<IDamageContext>()))
                    .Returns<IDamageContext>(Record);
            }

            public IFightable Object => _mock.Object;

            /// <summary>Everything the bearer has been dealt since it was made.</summary>
            public float Damage { get; private set; }

            private Task Record(IDamageContext context)
            {
                Damage += context.TotalDamage;
                return Task.CompletedTask;
            }
        }
    }
}
