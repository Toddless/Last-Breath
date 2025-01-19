namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeDamage : Ability<AttackComponent>, ICanBuffAttack
    {
        private readonly float _criticalStrikeDamageBonus = 0.2f;

        public BuffCriticalStrikeDamage(AttackComponent component) : base(component)
        {
        }

        public override void AfterBuffEnds(AttackComponent? component) => component.BaseCriticalStrikeDamage -= _criticalStrikeDamageBonus;
        public override void ActivateAbility(AttackComponent? component) => component.BaseCriticalStrikeDamage += _criticalStrikeDamageBonus;
        public override void EffectAfterAttack(AttackComponent? component) => throw new NotImplementedException();
    }
}
