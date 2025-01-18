namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeDamage : Ability, ICanBuffAttack
    {
        private readonly float _criticalStrikeDamageBonus = 0.2f;

        public override Type TargetTypeComponent => typeof(AttackComponent);

        public override void AfterBuffEnds(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            attack.BaseCriticalStrikeDamage -= _criticalStrikeDamageBonus;
        }

        public override void ActivateAbility(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            attack.BaseCriticalStrikeDamage += _criticalStrikeDamageBonus;
        }
        public override void EffectAfterAttack(IGameComponent? component) => throw new System.NotImplementedException();
    }
}
