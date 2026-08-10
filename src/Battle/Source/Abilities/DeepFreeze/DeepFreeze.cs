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
        /// <summary>L2 upgrade point: chance to also freeze one random other enemy.</summary>
        public float SpreadFreezeChance { get; set; }

        /// <summary>L2 upgrade point: every effect already on the target lasts 1 more turn.</summary>
        public bool ExtendTargetEffects { get; set; }

        /// <summary>Four durations of effects laid on the TARGET — none of them the caster-side
        /// <see cref="AbilityParameter.Duration"/>, and no single one of them "the" applied duration.</summary>
        public static class Parameters
        {
            public const string FreezeDuration = nameof(FreezeDuration);
            public const string FrostbiteDuration = nameof(FrostbiteDuration);
            public const string FrostbiteColdAmp = nameof(FrostbiteColdAmp);
            public const string ColdResistanceShred = nameof(ColdResistanceShred);
            public const string ShredDuration = nameof(ShredDuration);
            public const string HealReductionValue = nameof(HealReductionValue);
            public const string HealReductionDuration = nameof(HealReductionDuration);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(Parameters.FreezeDuration, 1);
            parameters.RegisterDefault(Parameters.FrostbiteDuration, 3);
            parameters.RegisterDefault(AbilityParameter.Stacks, 8);
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
                        effect.Extend(1);

                await ApplyPayload(plan, owner, target);
                // A landing that carries no damage is still a landing: the road is the plan's own target
                // list, the same one every other direct delivery walks.
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, IsCritical: false, Damage: 0)
                {
                    Source = this,
                    Kind = ImpactKind.Hit
                });
            }

            await TrySpreadFreeze(plan, owner, field);
        }

        private async Task ApplyPayload(DeepFreezePlan plan, IFightable owner, IFightable target)
        {
            await new FreezeEffect(plan.FreezeDuration)
                .Apply(Laying(target));
            await new FrostbiteEffect(plan.FrostbiteDuration, (int)this[AbilityParameter.Stacks], this[Parameters.FrostbiteColdAmp])
                .Apply(Laying(target));

            if (plan.ColdResistanceShred > 0)
                await new ColdResistanceShredEffect((int)this[Parameters.ShredDuration], maxStacks: 1, plan.ColdResistanceShred)
                    .Apply(Laying(target));

            if (plan.HealReduction > 0)
                await new HealReductionEffect((int)this[Parameters.HealReductionDuration], maxStacks: 1, plan.HealReduction)
                    .Apply(Laying(target));
        }

        /// <summary>L2 upgrade: a coin flip freezes one random enemy the cast did not touch. He is a
        /// touched target all the same — a landing without damage is still a landing — but a SPLASH one:
        /// the plan never aimed at him (he is picked from the enemies it left out), and he is only frozen
        /// because the cast landed on somebody else.</summary>
        private async Task TrySpreadFreeze(DeepFreezePlan plan, IFightable owner, IBattleField field)
        {
            if (SpreadFreezeChance <= 0 || CombatRandom.Rolls.RandFloat() > SpreadFreezeChance) return;

            var untouched = field.GetEnemies(owner)
                .Where(enemy => enemy.IsAlive && plan.Targets.All(t => !t.IsSame(enemy.InstanceId)))
                .ToList();
            if (untouched.Count == 0) return;

            IFightable lucky = untouched[CombatRandom.Rolls.RandIntRange(0, untouched.Count - 1)];
            await new FreezeEffect(plan.FreezeDuration)
                .Apply(Laying(lucky));
            await ApplyImpactRiders(new AbilityImpact(owner, lucky, field, Succeeded: true, IsCritical: false, Damage: 0)
            {
                Source = this,
                Kind = ImpactKind.Splash
            });
        }
    }
}
