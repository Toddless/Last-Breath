namespace Core.Interfaces.Battle
{
    using Enums;
    using Entity;

    public interface IOnGettingAttackEventArgs
    {
        IEntity Character { get; }
        float Damage { get; }
        bool IsCrit { get; }
        AttackResults Result { get; }
    }
}
