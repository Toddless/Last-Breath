namespace Core.Interfaces.Battle
{
    using Enums;

    public interface IStance
    {
        int CurrentLevel { get; }
        Stance StanceType { get; }

        void OnActivate();
        void OnDeactivate();
    }
}
