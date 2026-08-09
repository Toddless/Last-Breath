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
    /// </summary>
    public class PoisonOnHitRider : IImpactRider
    {
        /// <summary>Keys the rider reads off the ability it is riding on.</summary>
        public static class Parameters
        {
            /// <summary>Turns a stack laid by this rider lasts.</summary>
            public const string PoisonDuration = nameof(PoisonDuration);

            /// <summary>Share of the impact's damage one tick of the stack carries.</summary>
            public const string PoisonPotency = nameof(PoisonPotency);
        }

        public string Id => "Rider_Poison_On_Hit";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return;

            var poison = new DamageOverTurnEffect(
                (int)impact.Source[Parameters.PoisonDuration],
                StatusEffects.Poison,
                percentFromDamage: impact.Source[Parameters.PoisonPotency]);

            await poison.Apply(new EffectApplyingContext
            {
                Caster = impact.Caster,
                Target = impact.Target,
                Source = InstanceId,
                Damage = impact.Damage,
                IsCritical = impact.IsCritical
            });
        }
    }
}
