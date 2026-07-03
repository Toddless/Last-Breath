namespace Battle.Source.Abilities
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Entity;

    /// <summary>
    /// Damage-dealing multicast family: the plan is a volley of direct (non-attack) hits.
    /// Owns the volley loop, hit damage math and crit application; concrete abilities
    /// only shape the plan (stage mutations, targets, on-hit riders).
    /// </summary>
    public abstract class MulticastVolleyAbility(
        string id,
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        Costs costType = Costs.Mana)
        : MulticastAbility<CastPlan>(id, tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        /// <summary>Every projectile hits every plan target as direct (non-attack) damage.</summary>
        protected override async Task ExecutePlan(CastPlan plan, IFightable owner)
        {
            for (int projectile = 0; projectile < plan.ProjectilesCount; projectile++)
            {
                foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
                {
                    var hit = await DealProjectileDamage(plan, owner, target);
                    foreach (var rider in plan.OnHitRiders)
                        rider(hit);
                }
            }
        }

        protected float CalculateProjectileDamage(CastPlan plan, IFightable owner) =>
            plan.Damage
            + owner.Parameters.Damage * plan.WeaponDamageScale
            + owner.Parameters.SpellDamage * plan.SpellDamageScale;

        protected async Task<ProjectileHit> DealProjectileDamage(CastPlan plan, IFightable owner, IFightable target)
        {
            float damage = CalculateProjectileDamage(plan, owner);
            bool isCritical = RollCritical(owner);
            // Crit damage is a pure additive multiplier by design: bonuses only ever add to it
            if (isCritical) damage *= owner.Parameters.CriticalDamage + CriticalDamageBonus;

            var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, IsCrit = isCritical };
            context.Add(plan.DamageType, damage);
            await target.TakeDamage(context);

            return new ProjectileHit(target, isCritical, context.TotalDamage);
        }
    }
}
