namespace LastBreath.Script.Abilities.Interfaces
{
    using Core.Interfaces.Skills;
    using LastBreath.Script;

    public interface IOnAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}
