namespace Core.Enums
{
    using System;

    [Flags]
    public enum StatusEffects
    {
        None = 0,
        Stun = 1 << 0,
        Paralysis = 1 << 1,
        Freeze = 1 << 2,
        Blind = 1 << 3,
        Bleed = 1 << 4,
        Poison = 1 << 5,
        Burning = 1 << 6,
        Heal = 1 << 7,
        Regeneration = 1 << 8,
        Rust = 1 << 9
    }
}
