namespace Battle.Source.Abilities.DeepFreeze
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;
    using Godot;

    /// <summary>Cast plan of the Deep Freeze: durations plus the optional stage payloads.</summary>
    public class DeepFreezePlan
    {
        public List<IFightable> Targets { get; set; } = [];
        public int FreezeDuration { get; set; }
        public int FrostbiteDuration { get; set; }
        public float ColdResistanceShred { get; set; }
        public float HealReduction { get; set; }
    }

    /// <summary>
    /// Freezes the target and applies a Frostbite stack. Stage 2 shreds the target's cold
    /// resistance, stage 3 cuts its healing, stage 4 freezes every enemy on the battlefield.
    /// No direct damage — the ability sets up cold follow-ups.
    /// </summary>
    public class DeepFreeze(AbilityBaseData data) : MulticastAbility<DeepFreezePlan>(data)
    {
        private readonly RandomNumberGenerator _rnd = new();

        /// <summary>L2 upgrade point: chance to also freeze one random other enemy.</summary>
        public float SpreadFreezeChance { get; set; }

        /// <summary>L2 upgrade point: every effect already on the target lasts 1 more turn.</summary>
        public bool ExtendTargetEffects { get; set; }

        public static class Parameters
        {
            public const string FreezeDuration = nameof(FreezeDuration);
            public const string FrostbiteDuration = nameof(FrostbiteDuration);
            public const string FrostbiteMaxStacks = nameof(FrostbiteMaxStacks);
            public const string FrostbiteColdAmp = nameof(FrostbiteColdAmp);
            public const string ColdResistanceShred = nameof(ColdResistanceShred);
            public const string ShredDuration = nameof(ShredDuration);
            public const string HealReductionValue = nameof(HealReductionValue);
            public const string HealReductionDuration = nameof(HealReductionDuration);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.FreezeDuration, 1);
            parameters.RegisterDefault(Parameters.FrostbiteDuration, 3);
            parameters.RegisterDefault(Parameters.FrostbiteMaxStacks, 8);
            parameters.RegisterDefault(Parameters.FrostbiteColdAmp, 0.15f);
            parameters.RegisterDefault(Parameters.ColdResistanceShred, 0.25f);
            parameters.RegisterDefault(Parameters.ShredDuration, 3);
            parameters.RegisterDefault(Parameters.HealReductionValue, 0.45f);
            parameters.RegisterDefault(Parameters.HealReductionDuration, 3);
        }

        public override IAbility Copy() => CopyUpgradesTo(new DeepFreeze(Data)
        {
            SpreadFreezeChance = SpreadFreezeChance,
            ExtendTargetEffects = ExtendTargetEffects
        });

        protected override DeepFreezePlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                Targets = targets,
                FreezeDuration = (int)this[Parameters.FreezeDuration],
                FrostbiteDuration = (int)this[Parameters.FrostbiteDuration]
            };

        protected override void ApplyStage(int stage, DeepFreezePlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.ColdResistanceShred = this[Parameters.ColdResistanceShred];
                    break;
                case 3:
                    plan.HealReduction = this[Parameters.HealReductionValue];
                    break;
                case 4:
                    plan.Targets = field.GetEnemies(owner).ToList();
                    break;
            }
        }

        protected override async Task ExecutePlan(DeepFreezePlan plan, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in plan.Targets.Where(t => t.IsAlive).ToList())
            {
                // The extension counts only effects present BEFORE this cast lands its own payload
                if (ExtendTargetEffects)
                    foreach (IEffect effect in target.Effects.GetBy(_ => true).ToList())
                        effect.Duration += 1;

                await ApplyPayload(plan, owner, target);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, IsCritical: false, Damage: 0));
            }

            TrySpreadFreeze(plan, owner, field);
        }

        private async Task ApplyPayload(DeepFreezePlan plan, IFightable owner, IFightable target)
        {
            await new FreezeEffect(plan.FreezeDuration)
                .Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });
            await new FrostbiteEffect(plan.FrostbiteDuration, (int)this[Parameters.FrostbiteMaxStacks], this[Parameters.FrostbiteColdAmp])
                .Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });

            if (plan.ColdResistanceShred > 0)
                await new ColdResistanceShredEffect((int)this[Parameters.ShredDuration], maxStacks: 1, plan.ColdResistanceShred)
                    .Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });

            if (plan.HealReduction > 0)
                await new HealReductionEffect((int)this[Parameters.HealReductionDuration], maxStacks: 1, plan.HealReduction)
                    .Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });
        }

        /// <summary>L2 upgrade: a coin flip freezes one random enemy the cast did not touch.</summary>
        private void TrySpreadFreeze(DeepFreezePlan plan, IFightable owner, IBattleField field)
        {
            if (SpreadFreezeChance <= 0 || _rnd.Randf() > SpreadFreezeChance) return;

            var untouched = field.GetEnemies(owner)
                .Where(enemy => enemy.IsAlive && plan.Targets.All(t => !t.IsSame(enemy.InstanceId)))
                .ToList();
            if (untouched.Count == 0) return;

            IFightable lucky = untouched[_rnd.RandiRange(0, untouched.Count - 1)];
            _ = new FreezeEffect(plan.FreezeDuration)
                .Apply(new EffectApplyingContext { Caster = owner, Target = lucky, Source = InstanceId });
        }
    }
}
