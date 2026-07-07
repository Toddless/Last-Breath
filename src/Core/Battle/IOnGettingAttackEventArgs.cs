namespace Core.Battle
{
    using Enums;
    using Entity;

    public interface IOnGettingAttackEventArgs
    {
        IFightable Character { get; }
        float Damage { get; }
        bool IsCrit { get; }
        AttackResults Result { get; }
    }
}
