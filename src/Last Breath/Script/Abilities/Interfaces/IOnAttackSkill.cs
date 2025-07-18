namespace Playground.Script.Abilities.Interfaces
{
    public interface IOnAttackSkill : ISkill
    {
        void Activate(ICharacter target);
    }
}
