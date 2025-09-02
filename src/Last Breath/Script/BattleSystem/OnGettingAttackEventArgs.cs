namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public class OnGettingAttackEventArgs(ICharacter character, AttackResults result, float damage = default, bool isCrit = false) : IOnGettingAttackEventArgs
    {
        public float Damage { get; } = damage;
        public bool IsCrit { get; } = isCrit;
        public ICharacter Character { get; } = character;
        public AttackResults Result { get; } = result;
    }
}
