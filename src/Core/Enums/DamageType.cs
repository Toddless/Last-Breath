namespace Core.Enums
{
    using System;

    [Flags]
    public enum DamageType
    {
        Sacred = 1 << 0,
        Blight = 1 << 1,
        Burning = 1 << 2,
        Poison = 1 << 3,
        Bleed = 1 << 4,
        Physical = 1 << 5,
        Fire = 1 << 6,
        Cold = 1 << 7,
        Lightning = 1 << 8
    }
}
