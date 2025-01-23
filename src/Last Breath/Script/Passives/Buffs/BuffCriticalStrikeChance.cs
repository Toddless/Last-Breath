namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class BuffCriticalStrikeChance : Ability<AttackComponent, BaseEnemy>, ICanBuffAttack
    {
        private readonly float _criticalStrikeChanceBonus = 0.1f;


        public override void ActivateAbility(AttackComponent? component) => component.BaseCriticalStrikeChance += _criticalStrikeChanceBonus;
        public override void SetTargetCharacter(BaseEnemy? target)
        {
            TargetCharacter = target;
            TargetComponent = target.AttackComponent;
        }
    }
}
