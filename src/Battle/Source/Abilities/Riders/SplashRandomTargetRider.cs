namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Enums;

    /// <summary>Impact rider: every SUCCESSFUL impact splashes a share of its damage onto a random other
    /// living enemy. The share is read off the ability the impact came out of — decorated and at the
    /// moment of the touch, so a record raising <see cref="AbilityParameter.SplashShare"/> is felt.</summary>
    public class SplashRandomTargetRider : IImpactRider
    {
        public string Id => "Rider_Splash_Random_Target";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return;

            float share = impact.Source.ValueOr(AbilityParameter.SplashShare, 0f);
            if (share <= 0) return;

            var enemies = impact.Field.GetEnemies(impact.Caster)
                .Where(e => e.IsAlive && !e.IsSame(impact.Target.InstanceId))
                .ToList();
            if (enemies.Count == 0) return;

            var splashTarget = enemies[CombatRandom.Rolls.RandIntRange(0, enemies.Count - 1)];
            var damageContext = new DamageContext { Source = impact.Caster, Cause = DamageCause.Ability };
            damageContext.Add(DamageType.Physical, impact.Damage.Total * share);
            await splashTarget.TakeDamage(damageContext);
        }
    }
}
