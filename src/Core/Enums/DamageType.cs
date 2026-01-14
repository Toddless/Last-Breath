namespace Core.Enums
{
    using System;

    [Flags]
    public enum DamageType
    {
        Normal = 0,
        Pure = 1 << 0,
        Burning = 1 << 1,
        Poison = 1 << 2,
        Bleed = 1 << 3,
    }
}
