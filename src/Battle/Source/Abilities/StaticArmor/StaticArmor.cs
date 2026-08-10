namespace Battle.Source.Abilities.StaticArmor
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>Cast plan of the Static Armor: buff duration plus the detonation knobs.</summary>
    public class StaticArmorPlan
    {
        public int Duration { get; set; }
        public float DetonationDamage { get; set; }
        public float WeaponScale { get; set; }
        public float SpellScale { get; set; }
        public int RequiredStacks { get; set; }
        public int ChargeDuration { get; set; }
        public float BarrierRestorePercent { get; set; }
        public float SplashPercent { get; set; }
        public bool ApplyOnHitTaken { get; set; }
    }

    /// <summary>
    /// Self-buff: while it lasts, the caster's attacks put Charge stacks on their target; at the
    /// required count the charge detonates with lightning. Stage 2 refunds barrier from detonation
    /// damage, stage 3 splashes a share to another enemy, stage 4 charges attackers who hit the caster.
    /// </summary>
    public class StaticArmor(AbilityBaseData data) : MulticastAbility<StaticArmorPlan>(data)
    {
        /// <summary>L3 upgrade point: detonations ignore elemental resistances.</summary>
        public bool IgnoreResistances { get; set; }

        /// <summary>L3 upgrade point: detonation overkill jumps to a random other enemy.</summary>
        public bool OverkillToRandom { get; set; }

        public static class Parameters
        {
            public const string DetonationDamage = nameof(DetonationDamage);

            /// <summary>Scales of the DETONATION, which happens turns later off somebody else's attacks;
            /// the cast itself deals nothing.</summary>
            public const string DetonationWeaponScale = nameof(DetonationWeaponScale);

            /// <inheritdoc cref="DetonationWeaponScale"/>
            public const string DetonationSpellScale = nameof(DetonationSpellScale);

            /// <summary>Threshold of Charge stacks before detonation — smaller is better, so not
            /// <see cref="AbilityParameter.Stacks"/> in meaning or direction.</summary>
            public const string RequiredStacks = nameof(RequiredStacks);

            public const string ChargeDuration = nameof(ChargeDuration);
            public const string StageTwoBarrierRestore = nameof(StageTwoBarrierRestore);
            public const string StageThreeSplashDamage = nameof(StageThreeSplashDamage);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterDefault(Parameters.DetonationDamage, 250f);
            parameters.RegisterDefault(Parameters.DetonationWeaponScale, 0.85f);
            parameters.RegisterDefault(Parameters.DetonationSpellScale, 0.75f);
            parameters.RegisterDefault(Parameters.RequiredStacks, 3);
            parameters.RegisterDefault(Parameters.ChargeDuration, 3);
            parameters.RegisterDefault(Parameters.StageTwoBarrierRestore, 0.25f);
            parameters.RegisterDefault(Parameters.StageThreeSplashDamage, 0.5f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new StaticArmor(Data)
        {
            IgnoreResistances = IgnoreResistances,
            OverkillToRandom = OverkillToRandom
        });

        protected override StaticArmorPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                Duration = (int)this[AbilityParameter.Duration],
                DetonationDamage = this[Parameters.DetonationDamage],
                WeaponScale = this[Parameters.DetonationWeaponScale],
                SpellScale = this[Parameters.DetonationSpellScale],
                RequiredStacks = (int)this[Parameters.RequiredStacks],
                ChargeDuration = (int)this[Parameters.ChargeDuration]
            };

        protected override void ApplyStage(int stage, StaticArmorPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.BarrierRestorePercent = this[Parameters.StageTwoBarrierRestore];
                    break;
                case 3:
                    plan.SplashPercent = this[Parameters.StageThreeSplashDamage];
                    break;
                case 4:
                    plan.ApplyOnHitTaken = true;
                    break;
            }
        }

        protected override async Task ExecutePlan(StaticArmorPlan plan, IFightable owner, IBattleField field)
        {
            var settings = new ChargeDetonation(
                SourceAbilityId: Id,
                Damage: plan.DetonationDamage,
                WeaponScale: plan.WeaponScale,
                SpellScale: plan.SpellScale,
                RequiredStacks: plan.RequiredStacks,
                ChargeDuration: plan.ChargeDuration,
                BarrierRestorePercent: plan.BarrierRestorePercent,
                SplashPercent: plan.SplashPercent,
                ApplyOnHitTaken: plan.ApplyOnHitTaken,
                IgnoreResistances: IgnoreResistances,
                OverkillToRandom: OverkillToRandom);

            await new StaticArmorEffect(plan.Duration, settings, field)
                .Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });
        }
    }
}
