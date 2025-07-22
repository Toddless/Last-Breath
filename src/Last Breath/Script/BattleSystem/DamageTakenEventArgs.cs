namespace Playground.Script.BattleSystem
{

    public class DamageTakenEventArgs(float damage, bool isCrit, ICharacter character)
    {
        public float Damage { get; } = damage;
        public bool IsCrit { get; } = isCrit;
        public ICharacter Character { get; } = character;
    }
}
