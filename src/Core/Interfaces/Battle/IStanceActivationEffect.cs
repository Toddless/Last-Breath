namespace Core.Interfaces.Battle
{
    using Core.Interfaces.Entity;

    public interface IStanceActivationEffect
    {
        void OnActivate(IEntity owner);
        void OnDeactivate(IEntity owner);
    }
}
