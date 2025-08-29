namespace LastBreath.Script.Abilities.Interfaces
{
    using Core.Interfaces.Skills;
    using LastBreath.Script.BattleSystem;

    public interface IPreAttackSkill : ISkill
    {
        void Activate(AttackContext context);
    }
}
