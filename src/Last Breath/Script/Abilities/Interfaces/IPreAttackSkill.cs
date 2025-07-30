namespace Playground.Script.Abilities.Interfaces
{
    using Playground.Script.BattleSystem;

    public interface IPreAttackSkill : ISkill
    {
        void Activate(AttackContext context);
    }
}
