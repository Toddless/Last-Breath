namespace Battle.Source.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>Everything a Charge detonation needs, frozen at cast time from the ability's
    /// (post-upgrade) parameters.</summary>
    public record ChargeDetonation(
        string SourceAbilityId,
        float Damage,
        float WeaponScale,
        float SpellScale,
        int RequiredStacks,
        int ChargeDuration,
        EffectValue BarrierRestorePercent,
        float SplashPercent,
        bool ApplyOnHitTaken);

    /// <summary>
    /// "Статический доспех": while the buff lasts, every landed attack of the bearer puts a Charge
    /// stack on its target; at <see cref="ChargeDetonation.RequiredStacks"/> stacks the charge
    /// detonates with lightning damage. Stage 2 refunds barrier from detonation damage, stage 3
    /// splashes a share to a random other enemy, stage 4 also charges attackers who hit the bearer.
    /// </summary>
    public class StaticArmorEffect(int duration, ChargeDetonation settings, IBattleField field)
        : Effect(id: "Effect_Static_Armor", duration, maxStacks: 1)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (!IsApplied || Target == null) return;

            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
            if (settings.ApplyOnHitTaken)
                SubscribeUntilRemoved<DamageTakenEvent>(Target.CombatEvents, OnDamageTaken);
        }

        public override IEffect Copy() => new StaticArmorEffect(Duration, settings, field);

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (evt.Context.Result is not AttackResults.Succeed) return;
            ApplyCharge(evt.Context.Target);
        }

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (Target == null) return;
            var context = evt.Context;
            if (context.Cause is not (DamageCause.Attack or DamageCause.Ability)) return;
            var attacker = context.Source;
            if (attacker.IsSame(Target.InstanceId) || !attacker.IsAlive) return;

            ApplyCharge(attacker);
        }

        private void ApplyCharge(IFightable victim)
        {
            if (Target == null || !victim.IsAlive) return;
            _ = new ChargeEffect(settings.ChargeDuration, settings.RequiredStacks, Detonate)
                .Apply(new EffectApplyingContext { Caster = Target, Target = victim, Source = InstanceId, Trace = Trace });
        }

        private void Detonate(IFightable victim)
        {
            var owner = Target;
            if (owner == null || !victim.IsAlive) return;

            float damage = settings.Damage
                           + owner.Parameters.PhysicalDamage * settings.WeaponScale
                           + owner.Parameters.SpellDamage * settings.SpellScale;

            float dealt = DealDetonationDamage(owner, victim, damage);
            // Through the one point every laid number goes through: this effect carries the
            // effectiveness of the cast that raised the armour, so the refund is scaled with it.
            float share = Effective(settings.BarrierRestorePercent);
            if (share > 0) owner.CurrentBarrier += dealt * share;

            if (settings.SplashPercent <= 0) return;
            IFightable? splashTarget = RandomEnemy(owner, except: victim);
            if (splashTarget != null) DealDetonationDamage(owner, splashTarget, damage * settings.SplashPercent);
        }

        /// <summary>One detonation hit; returns the post-mitigation damage.</summary>
        private float DealDetonationDamage(IFightable owner, IFightable victim, float damage)
        {
            var context = new DamageContext
            {
                Source = owner, Cause = DamageCause.Ability, SourceAbilityId = settings.SourceAbilityId
            };
            context.Add(DamageType.Lightning, damage);
            _ = victim.TakeDamage(context);

            return context.TotalDamage;
        }

        private IFightable? RandomEnemy(IFightable owner, IFightable except)
        {
            var enemies = field.GetEnemies(owner).Where(enemy => enemy.IsAlive && !enemy.IsSame(except.InstanceId)).ToList();
            return enemies.Count == 0 ? null : enemies[CombatRandom.Rolls.RandIntRange(0, enemies.Count - 1)];
        }
    }
}
