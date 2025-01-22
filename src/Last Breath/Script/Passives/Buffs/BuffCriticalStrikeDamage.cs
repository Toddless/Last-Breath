namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeDamage : Ability<AttackComponent, BaseEnemy>, ICanBuffAttack
    {
        private readonly float _criticalStrikeDamageBonus = 0.2f;


        public override void ActivateAbility(AttackComponent? component) => component.BaseCriticalStrikeDamage += _criticalStrikeDamageBonus;
        public override void SetTargetCharacter(BaseEnemy? target) => throw new NotImplementedException();
    }
}
