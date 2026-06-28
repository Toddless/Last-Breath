namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;

    public class SoAsApplyBuffStrategy(int attacksNeedForBuff, IEffect toApply) : SoAsDefaultExecutionStrategy
    {
        private SeriesOfAttacks? _ability;
        private IEntity? _owner;
        private int _successfulAttacks;

        public override async Task Execute(SeriesOfAttacks ability, IEntity owner, List<IEntity> targets, IBattleField field)
        {
            _successfulAttacks = 0;
            Subscribe(owner);
            await base.Execute(ability, owner, targets, field);
            Unsubscribe();
        }

        private void Subscribe(IEntity owner)
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
            var copy = toApply.Copy();
            var applyContext = new EffectApplyingContext { Caster = _owner, Target = _owner, Source = _ability.InstanceId };
            copy.Apply(applyContext);
        }

        private void Unsubscribe()
        {
            if (_owner == null || _ability == null) return;
            _owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            _owner = null;
            _ability = null;
        }
    }
}
