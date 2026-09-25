namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;

    public class VolleyCastPlan : DamagingCastPlan
    {
        public int ProjectilesCount { get; set; }
        /// <summary>Executed for every landed projectile (stack appliers, leeches, crit-riders).</summary>
        public List<Action<ProjectileHit>> OnHitRiders { get; } = [];
    }
}
