namespace LastBreath.Script.Abilities.Interfaces
{
    using LastBreath.Script.BattleSystem;

    public interface IPreAttackSkill : ISkill
    {
        void Activate(AttackContext context);
    }
}
