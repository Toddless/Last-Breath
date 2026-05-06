namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Effects;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Events.GameEvents;

    public class SoAsPoisonOnAttackExecutionStrategy(int duration, int stacks) : SoAsDefaultExecutionStrategy
    {
        private SeriesOfAttacks? _ability;
        private IEntity? _owner;

        public override async Task Execute(SeriesOfAttacks ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            Subscribe(ability, owner);
            await base.Execute(ability, owner, targets, field);
            Unsubscribe(owner);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            if (_ability == null || _owner == null) return;
            var context = obj.Context;
            var poison = new DamageOverTurnEffect(duration, stacks, StatusEffects.Poison);
            var applyContext = new EffectApplyingContext
            {
                Caster = _owner,
                Target = context.Target,
                Source = _ability.InstanceId,
                Damage = context.FinalDamage,
                IsCritical = context.IsCritical
            };
            poison.Apply(applyContext);
        }

        private void Subscribe(SeriesOfAttacks ability, IEntity owner)
        {
            _ability = ability;
            _owner = owner;
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void Unsubscribe(IEntity owner)
        {
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            _ability = null;
            _owner = null;
        }
    }
}
