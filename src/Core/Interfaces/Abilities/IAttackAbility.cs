namespace Core.Interfaces.Abilities
{
    public interface IAttackAbility : IAbility
    {
        float Damage { get; }
        float WeaponDamageScale { get; }
        float SpellDamageScale { get; }
        bool IsEvadable { get; set; }
    }
}
