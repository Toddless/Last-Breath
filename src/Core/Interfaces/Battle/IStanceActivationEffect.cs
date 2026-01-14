namespace Core.Interfaces.Battle
{
    using Entity;

    public interface IStanceActivationEffect
    {
        void OnActivate(IEntity owner);
        void OnDeactivate(IEntity owner);
    }
}
