namespace Battle.Source.Abilities.Riders
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Enums;

    /// <summary>Impact rider: every SUCCESSFUL impact splashes a share of its damage as pure damage
    /// onto a random other living enemy.</summary>
    public class SplashRandomTargetRider(float splashDamagePercent) : IImpactRider
    {
        public string Id => "Rider_Splash_Random_Target";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public async Task Apply(AbilityImpact impact)
        {
            if (!impact.Succeeded) return;

            var enemies = impact.Field.GetEnemies(impact.Caster)
                .Where(e => e.IsAlive && !e.IsSame(impact.Target.InstanceId))
                .ToList();
            if (enemies.Count == 0) return;

            var splashTarget = enemies[CombatRandom.Rolls.RandIntRange(0, enemies.Count - 1)];
            var damageContext = new DamageContext { Source = impact.Caster, Cause = DamageCause.Ability };
            damageContext.Add(DamageType.Physical, impact.Damage * splashDamagePercent);
            await splashTarget.TakeDamage(damageContext);
        }
    }
}
