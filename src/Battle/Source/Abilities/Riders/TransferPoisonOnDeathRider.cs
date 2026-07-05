namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Effects;

    /// <summary>
    /// Impact rider: when an impacted target later dies, its remaining poison stacks jump to a random
    /// living enemy. Subscribes once per target per cast.
    /// </summary>
    public class TransferPoisonOnDeathRider : IImpactRider
    {
        public string Id => "Ability_Transfer_Poison_On_Death_Rider";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public Task Apply(AbilityImpact impact)
        {
            SubscribeTransfer(impact.Target, impact.Caster, impact.Field);
            return Task.CompletedTask;
        }

        private static void SubscribeTransfer(IFightable target, IFightable owner, IBattleField field)
        {
            target.CombatEvents.Subscribe<EntityDiedEvent>(OnDead);
            return;

            void OnDead(EntityDiedEvent dead)
            {
                if (dead.Entity.InstanceId != target.InstanceId) return;
                target.CombatEvents.Unsubscribe<EntityDiedEvent>(OnDead);
                TransferPoison(dead.Entity, owner, field);
            }
        }

        private static void TransferPoison(IFightable dead, IFightable owner, IBattleField field)
        {
            var poisonStacks = dead.Effects.GetBy(e => e.Status == StatusEffects.Poison).OfType<DamageOverTurnEffect>().ToList();
            if (poisonStacks.Count == 0) return;

            var enemies = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0) return;

            var rnd = new Godot.RandomNumberGenerator();
            rnd.Randomize();
            var newTarget = enemies[rnd.RandiRange(0, enemies.Count - 1)];

            foreach (var stack in poisonStacks)
            {
                var clone = stack.Copy();
                clone.Apply(new EffectApplyingContext
                {
                    Caster = owner,
                    Target = newTarget,
                    Source = dead.InstanceId,
                    Damage = stack.DamagePerTick
                });
            }
        }
    }
}
