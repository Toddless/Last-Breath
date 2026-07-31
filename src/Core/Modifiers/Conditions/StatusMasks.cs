namespace Core.Modifiers.Conditions
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Enums;

    /// <summary>
    /// Named status groups the design talks in — "under any control", "burning, bleeding or poisoned",
    /// "clean". Data spells a group by its alias instead of retyping the members, so a status joining a
    /// group is one edit here and not a sweep through every node and item.
    /// </summary>
    public static class StatusMasks
    {
        /// <summary>Statuses that take the fighter's turn or spells away.</summary>
        public const StatusEffects Control = StatusEffects.Stun | StatusEffects.Paralysis | StatusEffects.Freeze;

        /// <summary>Statuses that tick damage between turns.</summary>
        public const StatusEffects DamageOverTime = StatusEffects.Poison | StatusEffects.Bleed | StatusEffects.Burning;

        /// <summary>Every status the fight resolves against its bearer: the turn or the spells taken away
        /// (<see cref="Control"/>) and health burned between turns (<see cref="DamageOverTime"/>); the
        /// inverted form reads as "clean" of those. Membership is what the status does to whoever carries
        /// it, not who reads its flag — Poison and Bleed are members whose whole rule lives on the effect
        /// instance and whose flag nobody reads. The remaining members of <see cref="StatusEffects"/> stay
        /// out because they either work for their bearer or do nothing on their own: whatever the fight
        /// applies through them belongs to the carrying effect, and a line over those counts the carriers
        /// (<see cref="EffectScope.Debuff"/>).</summary>
        public const StatusEffects Debuff = Control | DamageOverTime;

        private static readonly Dictionary<string, StatusEffects> s_aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Control)] = Control,
            [nameof(DamageOverTime)] = DamageOverTime,
            [nameof(Debuff)] = Debuff,
        };

        /// <summary>Resolves one data name: an alias first, then a plain status. An unknown name throws
        /// like any other strict enum parse, so a typo is reported and never a silent empty mask.</summary>
        public static StatusEffects Resolve(string name) =>
            s_aliases.TryGetValue(name, out var mask) ? mask : EnumParser.ParseEnum<StatusEffects>(name);
    }
}
