namespace Core.Interfaces.Battle
{
    using Entity;
    using Enums;

    public interface IOnGettingAttackEventArgs
    {
        IFightable Character { get; }
        float Damage { get; }
        bool IsCrit { get; }
        AttackResults Result { get; }
    }
}
