namespace Core.Enums
{
    using System;

    [Flags]
    public enum Effects
    {
        Stun = 1,
        Paralysis = 2,
        Freeze = 4,
        Blind = 8,
        Bleed = 16,
        Poison = 32,
        Burning = 64,
        Heal = 128,
        Regeneration = 256,
        Rust = 512,
    }
}
