namespace Core.Interfaces.Entity
{
    using System;
    using Core.Interfaces.Components;

    public interface IFightable
    {
        IHealthComponent Health {  get; }
        IDamageComponent Damage {  get; }
        IDefenceComponent Defence {  get; }

        bool IsFighting { get; set; }
        bool IsAlive {  get; set; }

        event Action? TurnStart, TurnEnd;
    }
}
