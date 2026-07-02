namespace Core.Interfaces.Battle
{
    using Entity;

    public interface IStanceActivationEffect
    {
        void OnActivate(IFightable owner);
        void OnDeactivate(IFightable owner);
    }
}
