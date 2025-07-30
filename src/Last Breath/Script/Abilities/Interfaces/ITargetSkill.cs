namespace Playground.Script.Abilities.Interfaces
{
    public interface ITargetSkill : ISkill
    {
        void Activate(ICharacter target);
        void Deactivate(ICharacter target);
    }
}
