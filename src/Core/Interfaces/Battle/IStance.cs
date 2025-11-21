namespace Core.Interfaces.Battle
{
    using Enums;
    using System;
    using Entity;
    using Components;

    public interface IStance
    {
        int CurrentLevel { get; }
        IResource Resource { get; }
        Stance StanceType { get; }

        event Action<float>? CurrentResourceChanges, MaximumResourceChanges;

        void OnAttack(IEntity target);
        void OnReceiveAttack(IAttackContext context);
        void OnActivate();
        void OnDeactivate();
    }
}
