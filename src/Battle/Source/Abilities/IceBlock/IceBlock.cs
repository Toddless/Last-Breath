namespace Battle.Source.Abilities.IceBlock
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>Cast plan of the Ice Block: the volley fields plus the stage-mutable stun length.</summary>
    public class IceBlockPlan : CastPlan
    {
        public int StunDuration { get; set; }
    }

    /// <summary>
    /// Drops a huge ice block on the target: one heavy cold hit that stuns. Stage 2 extends the stun,
    /// stage 3 adds a Withering Curse stack, stage 4 drops three extra blocks at half damage.
    /// </summary>
    public class IceBlock(
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
        : MulticastVolleyAbility(id: "Ability_Ice_Block", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        public override IAbility Copy()
        {
            var copy = new IceBlock(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,
                stunDuration, witheringDuration, witheringMaxStacks, witheringValue, extraBlocks, extraBlockDamagePercent, CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override CastPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            var plan = new IceBlockPlan
            {
                ProjectilesCount = 1,
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

        protected override void ApplyStage(int stage, CastPlan plan, IFightable owner, IBattleField field)
        {
            if (plan is not IceBlockPlan block) return;
            switch (stage)
            {
                case 2:
                    block.StunDuration += 1;
                    break;
                case 3:
                    block.OnHitRiders.Add(hit => _ = new WitheringCurseEffect(witheringDuration, witheringMaxStacks, witheringValue)
                        .Apply(new EffectApplyingContext { Caster = owner, Target = hit.Target, Source = InstanceId }));
                    break;
                case 4:
                    block.OnHitRiders.Add(hit => DropExtraBlocks(block, owner, hit.Target));
                    break;
            }
        }

        /// <summary>Stage 4: three more blocks crash down, each at a share of the main block's damage.</summary>
        private void DropExtraBlocks(IceBlockPlan plan, IFightable owner, IFightable target)
        {
            float blockDamage = CalculateProjectileDamage(plan, owner) * extraBlockDamagePercent;
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
