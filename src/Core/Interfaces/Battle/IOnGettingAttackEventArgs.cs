namespace Core.Interfaces.Battle
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public interface IOnGettingAttackEventArgs
    {
        IEntity Character { get; }
        float Damage { get; }
        bool IsCrit { get; }
        AttackResults Result { get; }
    }
}
