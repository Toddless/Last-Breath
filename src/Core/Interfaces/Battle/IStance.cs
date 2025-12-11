namespace Core.Interfaces.Battle
{
    using Enums;
    using System;
    using Entity;
    using Components;

    public interface IStance
    {
        int CurrentLevel { get; }
        Stance StanceType { get; }

        void OnActivate();
        void OnDeactivate();
    }
}
