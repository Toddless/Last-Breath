namespace Battle.Source.Abilities.IceBlock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>Cast plan of the Ice Block: the volley fields plus the stage-mutable stun length.</summary>
    public class IceBlockPlan : DamagingCastPlan
    {
        public int StunDuration { get; set; }
        public List<Action<ProjectileHit>> OnHitRiders { get; } = [];
    }

    /// <summary>
    /// Drops a huge ice block on the target: one heavy cold hit that stuns. Stage 2 extends the stun,
    /// stage 3 adds a Withering Curse stack, stage 4 drops three extra blocks at half damage.
    /// </summary>
    public class IceBlocks(AbilityBaseData data) : MulticastAbility<IceBlockPlan>(data)
    {
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];
        public int StunDuration => (int)this[AbilityParameter.StunDuration];
        public int ExtraBlocks => (int)this[Parameters.ExtraBlocks];

        /// <summary>Chance the cast clears its own cooldown when it is done.</summary>
        public float CooldownResetChance => this[AbilityParameter.CooldownResetChance];

        /// <summary>L3 upgrade point: the stage-4 extra blocks crash on random enemies instead of the target.</summary>
        public bool ExtraBlocksHitRandomTargets { get; set; }

        /// <summary>L3 upgrade point: an existing stun is consumed from the target and the block hits twice as hard.</summary>
        public bool ConsumeStunForDoubleDamage { get; set; }

        public static class Parameters
        {
            public const string WitheringDuration = nameof(WitheringDuration);
            public const string WitheringValue = nameof(WitheringValue);
            public const string ExtraBlocks = nameof(ExtraBlocks);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            RegisterDamageParameters(parameters);
            parameters.RegisterAppliedDuration(AbilityParameter.StunDuration, 1);
            parameters.RegisterAppliedDuration(Parameters.WitheringDuration, 3);
            parameters.RegisterDefault(AbilityParameter.Stacks, 3);
            parameters.RegisterDefault(Parameters.WitheringValue, 0.15f);
            parameters.RegisterDefault(Parameters.ExtraBlocks, 3);
            parameters.RegisterDefault(AbilityParameter.StageFourDamage, 0.5f);
            parameters.RegisterDefault(AbilityParameter.CooldownResetChance, 0f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new IceBlocks(Data)
        {
            ExtraBlocksHitRandomTargets = ExtraBlocksHitRandomTargets,
            ConsumeStunForDoubleDamage = ConsumeStunForDoubleDamage
        });

        /// <summary>One heavy block per target: the hit stuns (base plan rider), stage riders and the
        /// ability's impact riders fire per crushed target.</summary>
        protected override async Task ExecutePlan(IceBlockPlan plan, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                float multiplier = TryConsumeStun(target) ? 2f : 1f;
                var hit = await DealBlockDamage(plan, owner, target, multiplier);
                foreach (var rider in plan.OnHitRiders)
                    rider(hit);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, hit.IsCritical, hit.Damage)
                {
                    Source = this,
                    Kind = ImpactKind.Hit
                });
            }

            if (CooldownResetChance > 0 && CombatRandom.Rolls.RandFloat() <= CooldownResetChance) CooldownLeft = 0;
        }

        protected override IceBlockPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var plan = new IceBlockPlan
            {
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                DamageType = DamageType.Cold,
                Targets = targets,
                StunDuration = StunDuration
            };
            // The rider closes over the plan: stage 2 mutations to StunDuration are picked up automatically.
            plan.OnHitRiders.Add(hit => _ = new StunEffect(plan.StunDuration)
                .Apply(Laying(hit.Target)));
            return plan;
        }

        protected override void ApplyStage(int stage, IceBlockPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.StunDuration += 1;
                    break;
                case 3:
                    plan.OnHitRiders.Add(hit => _ = new WitheringCurseEffect(
                            (int)this[Parameters.WitheringDuration], (int)this[AbilityParameter.Stacks], this[Parameters.WitheringValue])
                        .Apply(Laying(hit.Target)));
                    break;
                case 4:
                    plan.OnHitRiders.Add(hit => _ = DropExtraBlocks(plan, owner, field, hit.Target));
                    break;
            }
        }

        /// <summary>The upgrade eats the target's stun instead of stacking on it — the block converts
        /// the lost control into double damage.</summary>
        private bool TryConsumeStun(IFightable target)
        {
            if (!ConsumeStunForDoubleDamage) return false;
            var stuns = target.Effects.GetBy(effect => effect.IsSame("Effect_Stun")).ToList();
            if (stuns.Count == 0) return false;

            foreach (IEffect stun in stuns) stun.Remove();
            return true;
        }

        /// <summary>The whole hit (flat + scales) is doubled, so the multiplier scales the plan for
        /// one strike and restores it — the plan lives for the rest of the cast.</summary>
        private async Task<ProjectileHit> DealBlockDamage(IceBlockPlan plan, IFightable owner, IFightable target, float multiplier)
        {
            if (multiplier == 1f) return await DealPlanDamage(plan, owner, target);

            (float damage, float weapon, float spell) = (plan.Damage, plan.WeaponDamageScale, plan.SpellDamageScale);
            plan.Damage *= multiplier;
            plan.WeaponDamageScale *= multiplier;
            plan.SpellDamageScale *= multiplier;
            var hit = await DealPlanDamage(plan, owner, target);
            (plan.Damage, plan.WeaponDamageScale, plan.SpellDamageScale) = (damage, weapon, spell);
            return hit;
        }

        /// <summary>Stage 4: three more blocks crash down, each at a share of the main block's damage.
        /// With the L3 upgrade every extra block picks its own random enemy.
        /// A hit and not a splash, on countability: the ability owns the number of them
        /// (<see cref="ExtraBlocks"/>, a parameter an augment can raise), each carries its own share of
        /// the damage and, upgraded, picks its own victim — while a splash is by definition the touch
        /// nobody counts, spilled by an impact rather than aimed by the cast.</summary>
        private async Task DropExtraBlocks(IceBlockPlan plan, IFightable owner, IBattleField field, IFightable target)
        {
            float blockDamage = CalculateHitDamage(plan, owner) * this[AbilityParameter.StageFourDamage];
            for (int i = 0; i < ExtraBlocks; i++)
            {
                IFightable? victim = ExtraBlocksHitRandomTargets ? RandomEnemy(owner, field) : target;
                if (victim is not { IsAlive: true }) return;
                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
                context.Add(DamageType.Cold, blockDamage);
                await victim.TakeDamage(context);
                await ApplyImpactRiders(new AbilityImpact(owner, victim, field, Succeeded: true, IsCritical: false, context.TotalDamage)
                {
                    Source = this,
                    Kind = ImpactKind.Hit
                });
            }
        }

        private IFightable? RandomEnemy(IFightable owner, IBattleField field)
        {
            var enemies = field.GetEnemies(owner).Where(enemy => enemy.IsAlive).ToList();
            return enemies.Count == 0 ? null : enemies[CombatRandom.Rolls.RandIntRange(0, enemies.Count - 1)];
        }
    }
}
