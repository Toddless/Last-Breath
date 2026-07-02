namespace Core.Enums
{
    using System;

    [Flags]
    public enum DamageType
    {
        Pure = 0,
        Burning = 1 << 0,
        Poison = 1 << 1,
        Bleed = 1 << 2,
        Physical = 1 << 3,
        Fire = 1 << 4,
        Cold = 1 << 5,
        Lightning = 1 << 6
    }
}
