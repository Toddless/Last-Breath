namespace Core.Data
{
    /// <summary>
    /// What KIND of touch an <see cref="AbilityImpact"/> is — the road the delivery travelled to reach
    /// the target, not the payload it carried. A rider (and, later, a data-declared behaviour) filters
    /// on it: the design vocabulary tells "attacks" from "hits", and a chain jump is neither of them.
    /// The kind must be honest at the point the impact is built: naming a jump an attack would put every
    /// attack-only rider on a road it was never bought for, and no filter downstream could tell.
    /// </summary>
    public enum ImpactKind
    {
        /// <summary>One resolved attack of the attack pipeline (the weapon swing that can be dodged,
        /// blocked or crit and is announced as an attack). The only road that carries the "attacks"
        /// tag of the design list; every attack-series ability delivers through it.</summary>
        Attack,

        /// <summary>A direct hit the ability deals itself, outside the attack pipeline: the cast reaches
        /// a target of its own hit sequence or plan and lands on it. Damage is not what makes it a hit —
        /// a landing that only applies effects travelled the same road. A BOUNCE of an
        /// <c>IHitSequenceStrategy</c> is a hit too (the poison jar's landings are hits on purpose):
        /// <see cref="ChainJump"/> is only for a delivery whose plan owns a jump count and a damage
        /// falloff, because that is what makes a jump a different event and not merely a later target.</summary>
        Hit,

        /// <summary>One projectile of a volley landing on one target. Countable on purpose: an augment
        /// that adds projectiles must multiply per-impact riders by arithmetic alone, so N projectiles
        /// over M targets are N×M impacts and not one.</summary>
        Projectile,

        /// <summary>A jump of a chain — every strike after the initial one, on the target the chain hopped
        /// to. Kept apart from <see cref="Hit"/> because the chain's own numbers (falloff, jump count)
        /// belong to the jump and an "on hit" reading of a jump would be a reading of a different event.</summary>
        ChainJump,

        /// <summary>Secondary damage spilled by another impact onto a bystander: a shard bursting into
        /// shrapnel over the field, overkill leaping to a neighbour, a splash share of a landed blow.
        /// It is nobody's primary delivery — it exists only because another impact already happened —
        /// so it must not be counted as one of them, and it never spawns further impacts.</summary>
        Splash
    }
}
