namespace LastBreath.Script.Abilities.Interfaces
{
    using LastBreath.Script;

    public interface IOnGettingAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}
