namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Activation;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// Base of the Intelligence stance abilities: every cast rolls an activation stage.
    /// Stages are cumulative — a stage-4 roll applies the mutations of stages 2 and 3 to the cast plan.
    /// <typeparamref name="TPlan"/> is the per-cast state shape: a damage volley, a shield, a debuff set —
    /// the base only guarantees the roll, the cumulative stage loop and that a plan lives exactly one cast.
    /// Carries NO damage parameters — damaging descendants register those keys themselves.
    /// </summary>
    public abstract class MulticastAbility<TPlan>(AbilityBaseData data)
        : Ability(data)
        where TPlan : class
    {
        private const int BaseStage = 1;

        /// <summary>The stance activation roll; the knobs live inside (upgrades/boss phases tune it).</summary>
        public MulticastActivation Activation { get; } = new();

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            int stage = Activation.Roll(owner);
            owner.CombatEvents.Publish(new AbilityStageActivatedEvent(this, owner, stage));

            var plan = CreateBasePlan(targets, owner, field);
            for (int current = BaseStage + 1; current <= stage; current++)
                ApplyStage(current, plan, owner, field);

            await ExecutePlan(plan, owner, field);
        }

        /// <summary>The stage-1 cast built from the ability's current (post-upgrade) parameters.</summary>
        protected abstract TPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field);

        /// <summary>Mutation of a single stage; called for every stage from 2 up to the rolled one.</summary>
        protected abstract void ApplyStage(int stage, TPlan plan, IFightable owner, IBattleField field);

        /// <summary>Executes the fully mutated plan.</summary>
        protected abstract Task ExecutePlan(TPlan plan, IFightable owner, IBattleField field);

        /// <summary>
        /// One damaging hit of a plan: ability-boosted crit roll, the cast's CastId stamped for chord
        /// grouping. Every plan-based delivery deals its damage through this — the single seam.
        /// </summary>
        protected async Task<ProjectileHit> DealPlanDamage(DamagingCastPlan plan, IFightable owner, IFightable target)
        {
            float damage = CalculateHitDamage(plan, owner);
            bool isCritical = RollCritical(owner);
            if (isCritical) damage *= this.CriticalMultiplierOf(owner);

            var context = new DamageContext
            {
                Source = owner,
                Cause = DamageCause.Ability,
                IsCrit = isCritical,
                CastId = CastId,
                IgnoreResistances = plan.IgnoreResistances || (isCritical && plan.CritIgnoresResistances)
            };
            context.Add(plan.DamageType, damage);
            await target.TakeDamage(context);
            return new ProjectileHit(target, isCritical, context.TotalDamage);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterCriticalParameters(parameters);
        }

        protected bool RollCritical(IFightable owner) => this.RollsCritical(owner);

        protected float CalculateHitDamage(DamagingCastPlan plan, IFightable owner) =>
            plan.Damage
            + owner.Parameters.Damage * plan.WeaponDamageScale
            + owner.Parameters.SpellDamage * plan.SpellDamageScale;

    }
}
