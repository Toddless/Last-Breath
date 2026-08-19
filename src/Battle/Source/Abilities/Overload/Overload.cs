namespace Battle.Source.Abilities.Overload
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>Cast plan of the Overload: the mana-to-multiplier knobs.</summary>
    public class OverloadPlan
    {
        public float ManaBurnPercent { get; set; }
        public float DamagePerStep { get; set; }
        public float ManaPerStep { get; set; }
        public float ManaRestorePercent { get; set; }
        public bool IsFinalStage { get; set; }
    }

    /// <summary>
    /// Self-buff: absorbs a share of the caster's CURRENT mana; the next activated ability deals
    /// <c>DamagePerStep</c> more damage for every <c>ManaPerStep</c> mana absorbed.
    /// Stage 2 absorbs more, stage 3 counts every single point of mana as a step,
    /// stage 4 refills the caster's mana entirely after the cast.
    /// </summary>
    public class Overload(AbilityBaseData data) : MulticastAbility<OverloadPlan>(data)
    {
        public float ManaBurnPercent => this[Parameters.ManaBurnPercent];

        /// <summary>Augment point: rolling the final stage resets this ability's cooldown.</summary>
        public bool ResetCooldownOnFinalStage { get; set; }

        public static class Parameters
        {
            public const string ManaBurnPercent = nameof(ManaBurnPercent);

            /// <summary>Damage multiplier granted per step of absorbed mana to the NEXT ability — a rate,
            /// and this cast deals no damage of its own.</summary>
            public const string DamagePerStep = nameof(DamagePerStep);

            public const string ManaPerStep = nameof(ManaPerStep);
            public const string StageTwoAbsorbBonus = nameof(StageTwoAbsorbBonus);
            public const string StageThreeManaPerStep = nameof(StageThreeManaPerStep);
            public const string StageFourManaRestore = nameof(StageFourManaRestore);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            // The ability wears the 'buff' tag; without the key the effectiveness records that fit it
            // were bought and moved nothing.
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(AbilityParameter.ConsumeEffectiveness, 1f);
            parameters.RegisterDefault(Parameters.ManaBurnPercent, 0.25f);
            parameters.RegisterDefault(Parameters.DamagePerStep, 0.02f);
            parameters.RegisterDefault(Parameters.ManaPerStep, 3f);
            parameters.RegisterDefault(Parameters.StageTwoAbsorbBonus, 0.05f);
            parameters.RegisterDefault(Parameters.StageThreeManaPerStep, 1f);
            parameters.RegisterDefault(Parameters.StageFourManaRestore, 1f);
        }

        public override IAbility Copy() =>
            CopyUpgradesTo(new Overload(Data) { ResetCooldownOnFinalStage = ResetCooldownOnFinalStage });

        protected override OverloadPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                ManaBurnPercent = ManaBurnPercent,
                DamagePerStep = this[Parameters.DamagePerStep],
                ManaPerStep = this[Parameters.ManaPerStep]
            };

        protected override void ApplyStage(int stage, OverloadPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.ManaBurnPercent += this[Parameters.StageTwoAbsorbBonus];
                    break;
                case 3:
                    plan.ManaPerStep = this[Parameters.StageThreeManaPerStep];
                    break;
                case 4:
                    plan.ManaRestorePercent = this[Parameters.StageFourManaRestore];
                    plan.IsFinalStage = true;
                    break;
            }
        }

        protected override async Task ExecutePlan(OverloadPlan plan, IFightable owner, IBattleField field)
        {
            float absorbed = owner.CurrentMana * plan.ManaBurnPercent;
            owner.CurrentMana -= absorbed;

            // The absorbed mana's WORTH goes through the consumption multiplier, not how much is absorbed.
            float multiplier = plan.DamagePerStep * (absorbed / plan.ManaPerStep)
                               * this[AbilityParameter.ConsumeEffectiveness];
            if (multiplier > 0)
                await new OverloadChargeEffect(Id, multiplier).Apply(Laying(owner));

            if (plan.ManaRestorePercent > 0)
                owner.RestoreMana(new ManaRecoveryContext(owner, owner) { Amount = owner.Parameters.MaxMana * plan.ManaRestorePercent });

            if (ResetCooldownOnFinalStage && plan.IsFinalStage) CooldownLeft = 0;
        }
    }
}
