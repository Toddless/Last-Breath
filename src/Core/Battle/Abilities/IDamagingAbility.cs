namespace Core.Battle.Abilities
{
    public interface IDamagingAbility : IAbility
    {
        float Damage { get; }
        float WeaponDamageScale { get; }
        float SpellDamageScale { get; }
        bool IsEvadable { get; set; }
    }
}
