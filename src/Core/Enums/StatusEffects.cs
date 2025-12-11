namespace Core.Enums
{
    using System;

    [Flags]
    public enum StatusEffects
    {
        None = 0,
        /// <summary>
        /// Skip turn
        /// </summary>
        Stun = 1 << 0,
        /// <summary>
        /// Can´t use any spells
        /// </summary>
        Paralysis = 1 << 1,
        /// <summary>
        /// Skip turn and take more damage
        /// </summary>
        Freeze = 1 << 2,
        /// <summary>
        /// Lower chance for additional attack
        /// </summary>
        Blind = 1 << 3,
        /// <summary>
        /// Damage overturn
        /// </summary>
        Bleed = 1 << 4,
        /// <summary>
        /// Damage overturn
        /// </summary>
        Poison = 1 << 5,     // all of this have different base damage, duration and amount of stacks
        /// <summary>
        /// Damage overturn
        /// </summary>
        Burning = 1 << 6,
        /// <summary>
        /// Health recovery overturn
        /// </summary>
        Regeneration = 1 << 7,
        /// <summary>
        /// Reduced armor
        /// </summary>
        Rust = 1 << 8,
        /// <summary>
        /// Having one or more curses
        /// </summary>
        Cursed = 1 << 9,
        /// <summary>
        /// Losing hp with each attack
        /// </summary>
        Fury = 1 << 10,
        /// <summary>
        /// Random target choosing
        /// </summary>
        Confused = 1 << 11,
        Charmed = 1 << 12,
    }
}
