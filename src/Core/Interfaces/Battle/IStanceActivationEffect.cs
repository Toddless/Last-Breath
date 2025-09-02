namespace Core.Interfaces.Battle
{
    public interface IStanceActivationEffect
    {
        void OnActivate(ICharacter owner);
        void OnDeactivate(ICharacter owner);
    }
}
