namespace Battle.Source.Abilities.IceBlock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>Cast plan of the Ice Block: the volley fields plus the stage-mutable stun length.</summary>
    public class IceBlockPlan : DamagingCastPlan
    {
        public int StunDuration { get; set; }
        public List<Action<TargetHit>> OnHitRiders { get; } = [];
    }

    /// <summary>
    /// Drops a huge ice block on the target: one heavy cold hit that stuns. Stage 2 extends the stun,
    /// stage 3 adds a Withering Curse stack, stage 4 drops three extra blocks at half damage.
    /// </summary>
    public class IceBlocks(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int stunDuration,
        int witheringDuration,
        int witheringMaxStacks,
        float witheringValue,
        int extraBlocks,
        float extraBlockDamagePercent,
        Costs costType = Costs.Mana)
        : MulticastAbility<IceBlockPlan>(id: "Ability_Ice_Block", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        // No upgrades touch these, so plain properties are enough for live description values (no ModuleManager).
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values["StunDuration"] = stunDuration;
                values["ExtraBlocks"] = extraBlocks;
                values["ExtraBlockDamagePercent"] = extraBlockDamagePercent;
                return values;
            }
        }

        public override IAbility Copy()
        {
            var copy = new IceBlocks(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,
                stunDuration, witheringDuration, witheringMaxStacks, witheringValue, extraBlocks, extraBlockDamagePercent, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override Task ExecutePlan(IceBlockPlan plan, IFightable owner)
        {
            return Task.CompletedTask;
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
                StunDuration = stunDuration
            };
            // The rider closes over the plan: stage 2 mutations to StunDuration are picked up automatically.
            plan.OnHitRiders.Add(hit => _ = new StunEffect(plan.StunDuration)
                .Apply(new EffectApplyingContext { Caster = owner, Target = hit.Target, Source = InstanceId }));
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
                    plan.OnHitRiders.Add(hit => _ = new WitheringCurseEffect(witheringDuration, witheringMaxStacks, witheringValue)
                        .Apply(new EffectApplyingContext { Caster = owner, Target = hit.Target, Source = InstanceId }));
                    break;
                case 4:
                    plan.OnHitRiders.Add(hit => DropExtraBlocks(plan, owner, hit.Target));
                    break;
            }
        }



        /// <summary>Stage 4: three more blocks crash down, each at a share of the main block's damage.</summary>
        private void DropExtraBlocks(IceBlockPlan plan, IFightable owner, IFightable target)
        {
            float blockDamage = CalculateHitDamage(plan, owner) * extraBlockDamagePercent;
            for (int i = 0; i < extraBlocks; i++)
            {
                if (!target.IsAlive) return;
                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
                context.Add(DamageType.Cold, blockDamage);
                _ = target.TakeDamage(context);
            }
        }
    }
}
