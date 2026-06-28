namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Godot;

    /// <summary>
    /// L3 upgrade strategy: each successful attack also deals a percentage of the damage
    /// to a random enemy on the battlefield.
    /// </summary>
    public class IpDamageRandomTargetStrategy(float splashDamagePercent) : IpDefaultExecutionStrategy
    {
        private IEntity? _owner;
        private readonly RandomNumberGenerator _splashRnd = new();
        private IBattleField? _field;

        public override async Task Execute(IncreasingPressure ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            _owner = owner;
            _splashRnd.Randomize();
            Subscribe(owner);
            _field = field;
            await base.Execute(ability, owner, targets, field);
            Unsubscribe(owner);
            _owner = null;
        }

        private void Subscribe(IEntity owner) =>
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (_owner == null) return;
            if (_field == null) return;
            var context = obj.Context;
            if (context.Result != AttackResults.Succeed) return;

            float splashDamage = context.FinalDamage * splashDamagePercent;

            // Get a random enemy from the owner's group; fall back to main target
            // TODO: Replace with full battlefield enemy list via IEntityGroup or IBattle
            var group = _owner.Group;
            if (group == null) return;

            var enemies = _field.GetEnemies(_owner).Where(e => e.IsAlive && e != context.Target).ToList();
            if (enemies.Count == 0) return;

            int idx = _splashRnd.RandiRange(0, enemies.Count - 1);
            var randomTarget = enemies[idx];

            randomTarget.TakeDamage(new DamageContext { Source = _owner, Damage = splashDamage, Cause = DamageCause.Ability, Type = DamageType.Normal });
        }

        private void Unsubscribe(IEntity owner) =>
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
    }
}
