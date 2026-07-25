namespace Core.Items
{
    using Enums;

    public interface IWeaponItem : IEquipItem
    {
        WeaponType WeaponType { get; }
        Handedness Handedness { get; }

        /// <summary>Effective weapon damage: base scaled by upgrade level and the weapon's local damage modifiers.
        /// Replaces the owner's base Damage parameter while equipped.</summary>
        float Damage { get; }

        /// <summary>Replaces the owner's base critical chance while equipped. Not scaled by upgrades.</summary>
        float CriticalChance { get; }

        /// <summary>Replaces the owner's base critical damage while equipped. Not scaled by upgrades.</summary>
        float CriticalDamage { get; }

        /// <summary>The stat-block view of one base stat: the bare base (sharpening-scaled where it
        /// applies) and the LOCAL lines' contribution, folded exactly as the domain applies them —
        /// effective = Base + LocalBonus. LocalBonus 0 = the stat is untouched by the item's lines.</summary>
        (float Base, float LocalBonus) GetStatBreakdown(EntityParameter parameter);
    }
}
