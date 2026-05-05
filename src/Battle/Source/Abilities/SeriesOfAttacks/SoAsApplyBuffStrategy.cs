namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public class SoAsApplyBuffStrategy(int attacksNeedForBuff, IEffect toApply) : SoAsDefaultExecutionStrategy
    {
        private SeriesOfAttacks? _ability;
        private IEntity? _owner;
        private int _successfulAttacks;

        public override async Task Execute(SeriesOfAttacks ability, IEntity owner, List<IEntity> targets)
        {
            _successfulAttacks = 0;
            Subscribe(ability, owner);
            await base.Execute(ability, owner, targets);
            Unsubscribe(owner);
        }

        private void Subscribe(SeriesOfAttacks ability, IEntity owner)
        {
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            var context = obj.Context;
            if (context.Result is not AttackResults.Succeed) return;
            _successfulAttacks++;
            if (_successfulAttacks >= attacksNeedForBuff) ApplyBuff();
        }

        private void ApplyBuff()
        {
            if (_owner == null || _ability == null) return;
            var copy = toApply.Clone();
            var applyContext = new EffectApplyingContext { Caster = _owner, Target = _owner, Source = _ability.InstanceId };
            copy.Apply(applyContext);
        }

        private void Unsubscribe(IEntity owner)
        {
            if (_owner == null || _ability == null) return;
            _owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            _owner = null;
            _ability = null;
        }
    }
}
