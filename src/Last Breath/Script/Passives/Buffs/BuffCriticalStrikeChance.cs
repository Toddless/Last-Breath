namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeChance : Ability<AttackComponent>, ICanBuffAttack
    {
        private readonly float _criticalStrikeChanceBonus = 0.1f;

        public BuffCriticalStrikeChance(AttackComponent component) : base(component)
        {
        }

        public override void AfterBuffEnds(AttackComponent? component) => component.BaseCriticalStrikeChance -= _criticalStrikeChanceBonus;
        public override void ActivateAbility(AttackComponent? component) => component.BaseCriticalStrikeChance += _criticalStrikeChanceBonus;
        public override void EffectAfterAttack(AttackComponent? component) => throw new NotImplementedException();
    }
}
