namespace LastBreath.Script.Abilities.Interfaces
{
    using Core.Interfaces.Skills;
    using LastBreath.Script;

    public interface IOnGettingAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}
