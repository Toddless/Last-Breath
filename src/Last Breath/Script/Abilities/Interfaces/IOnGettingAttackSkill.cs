namespace Playground.Script.Abilities.Interfaces
{
    public interface IOnGettingAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}
