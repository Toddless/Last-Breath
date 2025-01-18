namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeChance : Ability, ICanBuffAttack
    {
        private readonly float _criticalStrikeChanceBonus = 0.1f;

        public override Type TargetTypeComponent => typeof(AttackComponent);

        public override void AfterBuffEnds(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            attack.BaseCriticalStrikeChance -= _criticalStrikeChanceBonus;
        }

        public override void ActivateAbility(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            attack.BaseCriticalStrikeChance += _criticalStrikeChanceBonus;
        }

        public void Sas(IGameComponent? component)
        {
            if (component == null)
            {
                return;
            }
            if (component is not AttackComponent attack)
            {
                return;
            }
            attack.BaseAdditionalAttackChance += _criticalStrikeChanceBonus;
        }

        public override void EffectAfterAttack(IGameComponent? component) => throw new System.NotImplementedException();
    }
}
