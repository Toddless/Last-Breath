namespace Core.Interfaces.Battle
{
    using Core.Enums;

    public interface IOnGettingAttackEventArgs
    {
        ICharacter Character { get; }
        float Damage { get; }
        bool IsCrit { get; }
        AttackResults Result { get; }
    }
}
