namespace Battle
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class OnGettingAttackEventArgs(IEntity character, AttackResults result, float damage = default, bool isCrit = false) : IOnGettingAttackEventArgs
    {
        public float Damage { get; } = damage;
        public bool IsCrit { get; } = isCrit;
        public IEntity Character { get; } = character;
        public AttackResults Result { get; } = result;
    }
}
