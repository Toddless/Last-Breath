namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The combat-tag vocabulary. Tags are the compatibility axis of the augment system: an augment
    /// carrying a tag fits (a socket of its tier on) any ability sharing that tag. Data (`tags` in
    /// BaseAbilityData.json and in augment records) must use ONLY members of this list — the audit
    /// test holds the invariant, so a typo is a red test, not a silently incompatible augment.
    /// </summary>
    public static class AbilityTags
    {
        /// <summary>Weapon-driven direct hits and attack chains.</summary>
        public const string Attack = "attack";
        /// <summary>Spell-flavored casts (multicast family and other spell-scaling deliveries).</summary>
        public const string Spell = "spell";
        /// <summary>Multi-attack chains where per-attack riders and "+1 attack" effects make sense.</summary>
        public const string Series = "series";
        /// <summary>Positive effect the caster puts on itself (or an ally).</summary>
        public const string Buff = "buff";
        public const string Poison = "poison";
        public const string Cold = "cold";
        public const string Lightning = "lightning";
        public const string Fire = "fire";
        public const string Stun = "stun";
        public const string Freeze = "freeze";
        public const string Crit = "crit";
        public const string Barrier = "barrier";
        /// <summary>Mana-economy mechanics (burn, refund, cost manipulation).</summary>
        public const string Mana = "mana";
        /// <summary>Health as a resource: costs, sacrifices, health-driven scaling.</summary>
        public const string Health = "health";
        public const string Evasion = "evasion";
        public const string Execute = "execute";
        /// <summary>Returns damage to the attacker (Porcupine family).</summary>
        public const string Retaliation = "retaliation";
        /// <summary>Charges the NEXT activated ability (Sacrifice, Overload).</summary>
        public const string Empower = "empower";
        public const string Shield = "shield";
        public const string Summon = "summon";
        /// <summary>Authored NPC/boss kit — never reachable by the player.</summary>
        public const string Npc = "npc";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Attack, Spell, Series, Buff, Poison, Cold, Lightning, Fire, Stun, Freeze, Crit, Barrier,
            Mana, Health, Evasion, Execute, Retaliation, Empower, Shield, Summon, Npc,
        };

        /// <summary>The tag-compatibility rule of the augment system: one shared tag is enough.</summary>
        public static bool SharesAny(IEnumerable<string> left, IEnumerable<string> right) =>
            left.Intersect(right, StringComparer.OrdinalIgnoreCase).Any();
    }
}
