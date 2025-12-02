namespace Core.Enums
{
    using System;

    [Flags]
    public enum StatusEffects
    {
        None = 0,
        Stun = 1 << 0, // can do nothing
        Paralysis = 1 << 1, // can´t cast spells
        Freeze = 1 << 2, // can do nothing && get increase damage
        Blind = 1 << 3, // can't see
        Bleed = 1 << 4, // damage overturn
        Poison = 1 << 5, // damage overturn         // all of this have different base damage, duration and amount of stacks
        Burning = 1 << 6, // damage overturn
        Regeneration = 1 << 7, // heal overturn
        Rust = 1 << 8, // lower armor
        Cursed = 1 << 9, // has one or more curses
        Fury = 1 << 10, // on each attack lose hp
    }
}
