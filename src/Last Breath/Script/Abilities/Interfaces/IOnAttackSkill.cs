namespace LastBreath.Script.Abilities.Interfaces
{
    using LastBreath.Script;

    public interface IOnAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}
