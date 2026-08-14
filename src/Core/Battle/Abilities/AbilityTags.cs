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
        /// <summary>Delivery that sends counted projectiles, each of them an impact of its own.</summary>
        public const string Projectile = "projectile";
        public const string Cold = "cold";
        public const string Lightning = "lightning";
        public const string Fire = "fire";
        public const string Stun = "stun";
        public const string Freeze = "freeze";
        /// <summary>Critical chance and critical damage, of the cast itself or granted as a buff.</summary>
        public const string Critical = "critical";
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

        public const string Accuracy = "accuracy";
        public const string Armor = "armor";
        /// <summary>Burning damage over turns, as opposed to the fire damage of a cast itself.</summary>
        public const string Burn = "burn";
        /// <summary>Cast that climbs through stages, each one costing and hitting more than the last.</summary>
        public const string Charge = "charge";
        /// <summary>Takes the target's turn away (stun, freeze and the rest of the hard control).</summary>
        public const string Control = "control";
        public const string Cooldown = "cooldown";
        public const string Cost = "cost";
        /// <summary>Lasting mark on the target that works on what it may do rather than on its numbers.</summary>
        public const string Curse = "curse";
        public const string Damage = "damage";
        /// <summary>Negative effect the caster puts on the target.</summary>
        public const string Debuff = "debuff";
        /// <summary>Mitigation the owner gains from the cast itself, apart from armor and barriers.</summary>
        public const string Defence = "defence";
        public const string Duration = "duration";
        /// <summary>Works on the effects a cast leaves behind rather than on the cast.</summary>
        public const string Effect = "effect";
        /// <summary>The strength one stack of a stacking effect is worth.</summary>
        public const string Effectiveness = "effectiveness";
        /// <summary>Delivery the target may dodge — the attacks of a series and the strikes beside them.</summary>
        public const string Evadable = "evadable";
        /// <summary>Restores health to the owner as a cast of its own.</summary>
        public const string Heal = "heal";
        /// <summary>Direct touches of a delivery (hit sequences), as opposed to real attacks.</summary>
        public const string Hit = "hit";
        public const string Leech = "leech";
        /// <summary>Health and mana coming back over turns, as regeneration rather than as a cast.</summary>
        public const string Recovery = "recovery";
        /// <summary>Gives a resource back on impact — the counterpart of what the cast spent.</summary>
        public const string Restore = "restore";
        /// <summary>Weapon and spell damage coefficients a cast is measured with.</summary>
        public const string Scale = "scale";
        public const string Splash = "splash";
        /// <summary>Carries what sits on one target over to the others.</summary>
        public const string Spread = "spread";
        public const string Stacks = "stacks";
        /// <summary>Who the cast reaches: how many targets and how they are picked.</summary>
        public const string Target = "target";
        /// <summary>Damage returned by wearing armor rather than by the retaliation family.</summary>
        public const string Thorn = "thorn";

        /// <summary>Cast activated for its effect rather than its delivery (the owner's "Activation" axis).</summary>
        public const string Activation = "activation";
        /// <summary>Elemental damage of the cast itself (fire, cold, lightning as a family).</summary>
        public const string Elemental = "elemental";
        /// <summary>Weapon-borne physical damage, as opposed to the elemental family.</summary>
        public const string Physical = "physical";
        /// <summary>Pure damage — mitigated by nothing.</summary>
        public const string Pure = "pure";
        /// <summary>Consumes a resource or an effect as the price or fuel of the cast.</summary>
        public const string Consume = "consume";
        /// <summary>The berserker's Fury effect family.</summary>
        public const string Fury = "fury";
        /// <summary>Multicast activation levels of the intelligence stance.</summary>
        public const string Stage = "stage";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Attack, Spell, Series, Buff, Poison, Cold, Lightning, Fire, Stun, Freeze, Critical, Barrier,
            Mana, Health, Evasion, Execute, Retaliation, Empower, Shield, Summon, Npc, Projectile,
            Accuracy, Armor, Burn, Charge, Control, Cooldown, Cost, Curse, Damage, Debuff, Defence,
            Duration, Effect, Effectiveness, Evadable, Heal, Hit, Leech, Recovery, Restore, Scale,
            Splash, Spread, Stacks, Target, Thorn,
            Activation, Elemental, Physical, Pure, Consume, Fury, Stage,
        };

        /// <summary>The tag-compatibility rule of the augment system: one shared tag is enough.</summary>
        public static bool SharesAny(IEnumerable<string> left, IEnumerable<string> right) =>
            left.Intersect(right, StringComparer.OrdinalIgnoreCase).Any();
    }
}
