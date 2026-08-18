namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// L3 upgrade strategy: each successful attack also deals a percentage of the damage
    /// to a random enemy on the battlefield.
    /// </summary>
    public class IpDamageRandomTargetStrategy(float splashDamagePercent) : IpDefaultExecutionStrategy
    {
        private IncreasingPressure? _ability;
        private IFightable? _owner;
        private IBattleField? _field;

        public override async Task Execute(IncreasingPressure ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            _ability = ability;
            _owner = owner;
            Subscribe(owner);
            _field = field;
            await base.Execute(ability, owner, targets, field);
            Unsubscribe(owner);
            _owner = null;
            _field = null;
            _ability = null;
        }

        private void Subscribe(IFightable owner) =>
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (_ability == null) return;
            if (_owner == null) return;
            if (_field == null) return;
            var context = obj.Context;
            if (context.Result != AttackResults.Succeed) return;

            float splashDamage = context.FinalDamage.Total * splashDamagePercent;

            // Get a random enemy from the owner's group; fall back to main target
            // TODO: Replace with full battlefield enemy list via IEntityGroup or IBattle
            var group = _owner.Group;
            if (group == null) return;

            var enemies = _field.GetEnemies(_owner).Where(e => e.IsAlive && e != context.Target).ToList();
            if (enemies.Count == 0) return;

            int idx = CombatRandom.Rolls.RandIntRange(0, enemies.Count - 1);
            var randomTarget = enemies[idx];

            var damageContext = new DamageContext { Source = _owner, Cause = DamageCause.Ability };
            damageContext.Add(DamageType.Sacred, splashDamage);
            _ = randomTarget.TakeDamage(damageContext);
            // A share of a landed attack, thrown at somebody the attack never aimed at: splash, and a
            // touched target owed an impact like every other. What is reported is what the context ended
            // up carrying, like every other delivery — the mitigated number, not the one asked for.
            // The handler is the event bus's and cannot wait, so the riders start the way the damage does.
            _ = _ability.ApplyImpactRiders(new AbilityImpact(_owner, randomTarget, _field, Succeeded: true, IsCritical: false, DamageSnapshot.From(damageContext))
            {
                Source = _ability,
                Kind = ImpactKind.Splash
            });
        }

        private void Unsubscribe(IFightable owner) =>
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
    }
}
