namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;

    public class IpExtendPoisonDuration(int duration) : IpDefaultExecutionStrategy
    {
        public override async Task Execute(IncreasingPressure ability, IFightable owner, List<IFightable> targets, IBattleField field)
        {
            Subscribe(owner);
            await base.Execute(ability, owner, targets, field);
            Unsubscribe(owner);
        }

        private void Subscribe(IFightable owner) => owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);

        private void OnAfterAttack(AfterAttackEvent obj)
        {
            var context = obj.Context;
            if (context.Result != AttackResults.Succeed) return;
            foreach (IEffect effect in context.Target.Effects.GetBy(x => x.Status == StatusEffects.Poison))
                effect.Duration += duration;
        }

        private void Unsubscribe(IFightable owner) => owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
    }
}
