namespace Playground.Script.BattleSystem
{
    using Playground.Script;

    public interface IStanceActivationEffect
    {
        void OnActivate(ICharacter owner);
        void OnDeactivate(ICharacter owner);
    }
}
