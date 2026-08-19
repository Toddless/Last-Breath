namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Entity;
    using Core.Events;

    /// <summary>
    /// Counts the caster's successful attacks and lays <paramref name="buff"/> from the Nth one onward.
    /// The tally runs across casts and is zeroed on <see cref="BattleEndEvent"/>, because ability
    /// instances outlive a battle and a count carried into the next one is not the count that was bought.
    /// </summary>
    public class BuffAfterAttacksImpactRider(string id, int attacksNeeded, IEffect buff) : IImpactRider
    {
        private IFightable? _counted;
        private int _succeeded;

        public string Id => id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (impact.Kind != ImpactKind.Attack || !impact.Succeeded) return;

            Follow(impact.Caster);
            if (++_succeeded < attacksNeeded) return;

            await buff
                .Copy()
                .Apply(new EffectApplyingContext
                {
                    Caster = impact.Caster,
                    Target = impact.Caster,
                    Source = InstanceId,
                    Effectiveness = impact.Source.Effectiveness,
                    Trace = impact.Source.Trace
                });
        }

        /// <summary>Gives the fighter back his bus. Without it the rider stays a listener on a caster it
        /// no longer rides for, and a rebuild of the binder — which builds a fresh rider every pass —
        /// leaves one more of them behind each time.</summary>
        public void Detach()
        {
            _counted?.CombatEvents.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            _counted = null;
            _succeeded = 0;
        }

        /// <summary>Listens for the end of the battle on whoever is being counted, and starts the count
        /// over when that turns out to be somebody else.</summary>
        private void Follow(IFightable caster)
        {
            if (ReferenceEquals(_counted, caster)) return;

            _counted?.CombatEvents.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            _counted = caster;
            _succeeded = 0;
            caster.CombatEvents.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        private void OnBattleEnd(BattleEndEvent evnt) => _succeeded = 0;
    }
}
