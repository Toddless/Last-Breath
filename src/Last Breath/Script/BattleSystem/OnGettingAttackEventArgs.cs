namespace LastBreath.Script.BattleSystem
{
    using Core.Enums;
    using LastBreath.Script;

    public class OnGettingAttackEventArgs(ICharacter character, AttackResults result, float damage = default, bool isCrit = false)
    {
        public float Damage { get; } = damage;
        public bool IsCrit { get; } = isCrit;
        public ICharacter Character { get; } = character;
        public AttackResults Result { get; } = result;
    }
}
