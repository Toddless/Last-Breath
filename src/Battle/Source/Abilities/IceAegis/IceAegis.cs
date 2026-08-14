namespace Battle.Source.Abilities.IceAegis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>Cast plan of the Ice Aegis: barrier numbers and the optional stage payloads.</summary>
    public class AegisPlan
    {
        public float BarrierBase { get; set; }
        public float PerIntelligenceScale { get; set; }
        public int Duration { get; set; }
        public float ReflectPercent { get; set; }
        public float HealPerTurnPercent { get; set; }
        public Func<IEffect>? AttackerEffectFactory { get; set; }
        public Action? OnBarrierBroken { get; set; }
    }

    /// <summary>
    /// Grants a barrier scaling with intelligence for a few turns; the remainder burns on expiry.
    /// Stage 2 raises the intelligence scale, stage 3 makes attackers catch Clumsiness while the
    /// aegis holds, stage 4 freezes every enemy when the barrier is shattered early — the break
    /// reaction lives inside the barrier EFFECT, the ability only wires it up.
    /// </summary>
    public class IceAegis(AbilityBaseData data) : MulticastAbility<AegisPlan>(data)
    {
        public float BarrierBase => this[Parameters.BarrierBase];
        public float PerIntelligenceScale => this[Parameters.PerIntelligenceScale];
        public int Duration => (int)this[AbilityParameter.Duration];

        public static class Parameters
        {
            public const string BarrierBase = nameof(BarrierBase);
            public const string PerIntelligenceScale = nameof(PerIntelligenceScale);
            public const string StageTwoScaleBonus = nameof(StageTwoScaleBonus);

            /// <summary>How long the Clumsiness the aegis puts on an attacker holds — laid on somebody
            /// else, so not the caster-side <see cref="AbilityParameter.Duration"/>.</summary>
            public const string ClumsinessDuration = nameof(ClumsinessDuration);

            public const string ClumsinessValue = nameof(ClumsinessValue);
            public const string FreezeDuration = nameof(FreezeDuration);
            public const string ReflectPercent = nameof(ReflectPercent);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(Parameters.BarrierBase, 500f);
            parameters.RegisterDefault(Parameters.PerIntelligenceScale, 35f);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterDefault(Parameters.StageTwoScaleBonus, 15f);
            parameters.RegisterDefault(Parameters.ClumsinessDuration, 3);
            parameters.RegisterDefault(AbilityParameter.Stacks, 5);
            parameters.RegisterDefault(Parameters.ClumsinessValue, 0.15f);
            parameters.RegisterDefault(Parameters.FreezeDuration, 1);
            // Zero by default; upgrades raise them with decorators — the ability knows nothing about the upgrades
            parameters.RegisterDefault(Parameters.ReflectPercent, 0f);
            parameters.RegisterDefault(AbilityParameter.HealthRegeneration, 0f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new IceAegis(Data));

        protected override AegisPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                BarrierBase = BarrierBase,
                PerIntelligenceScale = PerIntelligenceScale,
                Duration = Duration,
                ReflectPercent = this[Parameters.ReflectPercent],
                HealPerTurnPercent = this[AbilityParameter.HealthRegeneration]
            };

        protected override void ApplyStage(int stage, AegisPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.PerIntelligenceScale += this[Parameters.StageTwoScaleBonus];
                    break;
                case 3:
                    plan.AttackerEffectFactory = CreateAttackerEffect;
                    break;
                case 4:
                    plan.OnBarrierBroken = () => FreezeAllEnemies(owner, field);
                    break;
            }
        }

        protected override async Task ExecutePlan(AegisPlan plan, IFightable owner, IBattleField field)
        {
            float intelligence = owner.Parameters.GetValueForParameter(EntityParameter.Intelligence);
            float amount = plan.BarrierBase + (plan.PerIntelligenceScale * intelligence);
            await new IceAegisEffect(plan.Duration, amount, plan.AttackerEffectFactory, plan.OnBarrierBroken,
                    plan.ReflectPercent, plan.HealPerTurnPercent)
                .Apply(Laying(owner));
        }

        /// <summary>Stage-3 payload for attackers: Clumsiness.</summary>
        private IEffect CreateAttackerEffect() =>
            new Clumsiness(
                (int)this[Parameters.ClumsinessDuration], (int)this[AbilityParameter.Stacks], this[Parameters.ClumsinessValue]);

        /// <summary>Stage 4: the shattering of the barrier freezes the field. Nobody was aimed at and the
        /// freeze exists only because the aegis broke — splash. The reaction is the effect's, but the
        /// closure is the ability's own and holds it, so there is nothing here to wait for a later wave:
        /// a frozen enemy is a touched target and reports as one.</summary>
        private void FreezeAllEnemies(IFightable owner, IBattleField field)
        {
            foreach (IFightable enemy in field.GetEnemies(owner).Where(e => e.IsAlive))
            {
                _ = new FreezeEffect((int)this[Parameters.FreezeDuration])
                    .Apply(Laying(enemy));
                _ = ApplyImpactRiders(new AbilityImpact(owner, enemy, field, Succeeded: true, IsCritical: false, Damage: 0)
                {
                    Source = this,
                    Kind = ImpactKind.Splash
                });
            }
        }
    }
}
