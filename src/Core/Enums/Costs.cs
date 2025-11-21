namespace Core.Enums
{
    using System;

    [Flags]
    public enum Costs
    {
        Mana = 1 << 0,
        Health = 1 << 1,
        Barrier = 1 << 2,
    }
}
