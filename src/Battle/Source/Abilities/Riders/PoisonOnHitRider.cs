namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;
    using Effects;

    /// <summary>
    /// Impact rider: every SUCCESSFUL impact puts a poison stack on its target, scaled by the impact
    /// damage.
    ///
    /// How long a stack lasts and how much of the blow one of its ticks carries are NOT the rider's:
    /// they are parameters of the ability the impact came out of, read through <c>impact.Source</c> at
    /// the moment the stack is laid. That is the whole of the transitivity — an augment that lengthens
    /// or strengthens the poison of a cast decorates those keys, and every stack this rider lays comes
    /// out decorated, with neither augment written for the other and neither knowing the other is
    /// there. A number taken at construction would be the number forever, and an amplifier seated
    /// beside the applier would move a value nothing reads.
    ///
    /// The keys are put on the ability by whoever installs the rider (<see cref="AugmentPoisonOnHit"/>).
    /// An ability that already names a poison duration of its own keeps its own base value, and the
    /// stacks are laid at what THAT cast is worth.
    ///
    /// An installer that lends no potency at all is not an error and not a reason to invent one: the
    /// stack is then laid the way the canon balances poison, through the registry. Only a potency the
    /// ability actually carries goes the other road, because only that one is a number augments can
    /// reach and decorate.
    /// </summary>
    public class PoisonOnHitRider : IImpactRider
    {
        /// <summary>Keys the rider reads off the ability. Stack duration is the book's shared
        /// <see cref="AbilityParameter.PoisonDuration"/>; the tick share belongs to this rider.</summary>
        public static class Parameters
        {
            /// <summary>Share of the impact's damage one tick of the stack carries.</summary>
            public const string PoisonPotency = nameof(PoisonPotency);
        }

        public string Id => "Rider_Poison_On_Hit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return;

            IEffect? poison = PoisonFor((int)impact.Source[AbilityParameter.PoisonDuration], impact.Source);
            if (poison == null) return;

            await poison.Apply(new EffectApplyingContext
            {
                Caster = impact.Caster,
                Target = impact.Target,
                Source = InstanceId,
                Damage = impact.Damage,
                IsCritical = impact.IsCritical,
                Effectiveness = impact.Source.Effectiveness,
                Trace = impact.Source.Trace
            });
        }

        /// <summary>The stack to lay, for as long as the ability says. A potency the ability carries is
        /// used as it stands — decorators and all — and only its absence hands the question to the canon;
        /// a share of nothing is an absence too, since a stack ticking for nothing is never laid anyway.</summary>
        private static IEffect? PoisonFor(int duration, IAbility source)
        {
            float potency = source.ValueOr(Parameters.PoisonPotency, 0f);
            return potency > 0f
                ? new DamageOverTurnEffect(duration, StatusEffects.Poison, DamageOverTurnEffect.NoCeilingOfItsOwn, potency)
                : DamageOverTurnEffect.FromCanon(duration, StatusEffects.Poison);
        }
    }
}
