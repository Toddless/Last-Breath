namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    /// <summary>
    /// Impact rider: when an impacted target later dies, its remaining poison stacks jump to a random
    /// living enemy. Waits on each touched target once, and keeps every watch it set so that leaving the
    /// ability calls them all off — a target who simply never dies would otherwise hold the closure for
    /// the rest of the battle, one more with every impact.
    /// </summary>
    public class TransferPoisonOnDeathRider : IImpactRider
    {
        private readonly Dictionary<IFightable, Action<EntityDiedEvent>> _watching = [];

        public string Id => "Ability_Transfer_Poison_On_Death_Rider";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(AbilityImpact impact)
        {
            // The trace is captured HERE and not read off the ability later: the death this waits for
            // may be turns away, by which time the ability is on another cast with another id.
            SubscribeTransfer(impact.Target, impact.Caster, impact.Field, impact.Source.Trace);
            return Task.CompletedTask;
        }

        public void Detach()
        {
            foreach ((IFightable target, Action<EntityDiedEvent> watch) in _watching)
                target.CombatEvents.Unsubscribe(watch);
            _watching.Clear();
        }

        private void SubscribeTransfer(IFightable target, IFightable owner, IBattleField field, AbilityTrace trace)
        {
            if (_watching.ContainsKey(target)) return;

            _watching[target] = OnDead;
            target.CombatEvents.Subscribe<EntityDiedEvent>(OnDead);
            return;

            void OnDead(EntityDiedEvent dead)
            {
                if (dead.Entity.InstanceId != target.InstanceId) return;
                target.CombatEvents.Unsubscribe<EntityDiedEvent>(OnDead);
                _watching.Remove(target);
                TransferPoison(dead.Entity, owner, field, trace);
            }
        }

        /// <summary>Domain mechanic: what jumps is ALL the poison on the corpse, whoever laid it. What
        /// lands on the new target is this cast's, so the ability that moved it may go on prolonging it.</summary>
        private static void TransferPoison(IFightable dead, IFightable owner, IBattleField field, AbilityTrace trace)
        {
            var poisonStacks = dead.Effects.GetBy(e => e.Status == StatusEffects.Poison).OfType<DamageOverTurnEffect>().ToList();
            if (poisonStacks.Count == 0) return;

            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0) return;

            var newTarget = enemies[CombatRandom.Rolls.RandIntRange(0, enemies.Count - 1)];

            foreach (var stack in poisonStacks)
            {
                var clone = stack.Copy();
                clone.Apply(new EffectApplyingContext
                {
                    Caster = owner,
                    Target = newTarget,
                    Source = dead.InstanceId,
                    Trace = trace
                });
            }
        }
    }
}
