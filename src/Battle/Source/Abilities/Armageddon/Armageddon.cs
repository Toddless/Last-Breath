namespace Battle.Source.Abilities.Armageddon
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;
    using HitDelivery;

    /// <summary>
    /// Charged ability: holding the button picks one of three stages, each consuming a share of max
    /// health and dealing its own damage; stage 3 additionally stuns. Direct hits (no attack pipeline).
    /// Stage-1 numbers live in the base damage modules so upgrades can override them; if health runs
    /// short at cast time the stage downgrades to the highest affordable one.
    /// </summary>
    public class Armageddon(AbilityBaseData data) : DamagingAbility(data), IChargedAbility
    {
        /// <summary>"+1 damage per every 5 missing health" — the missing-health step of the L3 upgrade.</summary>
        private const float MissingHpStep = 5f;

        /// <summary>Charge bookkeeping; lazy — the affordability gate needs the owner.</summary>
        private Activation.ChargedActivation Charge =>
            field ??= new Activation.ChargedActivation(maxStage: 3, stage => Owner != null && HpCost(stage) < Owner.CurrentHealth);

        public int StunDuration => (int)this[Parameters.StunDuration];
        public float HpCostMultiplier => this[Parameters.HpCostMultiplier];
        public float MissingHpRate => this[Parameters.MissingHpRate];

        public IHitSequenceStrategy HitSequence { get; set; } = new SelectedTargetsHits();

        /// <summary>L2 upgrade point: extra effect stage 3 puts on every hit target (e.g. burning stacks).</summary>
        public Func<IEffect>? Stage3EffectFactory { get; set; }
        public int Stage3EffectStacks { get; set; } = 1;

        public int MaxStage => Charge.MaxStage;

        public int PendingStage
        {
            get => Charge.PendingStage;
            set => Charge.PendingStage = value;
        }

        /// <summary>The charge is a commitment: once selection begins there is no backing out.</summary>
        public bool IsCancellable => false;

        public int MaxAffordableStage => Charge.MaxAffordableStage;

        public static class Parameters
        {
            public const string StunDuration = nameof(StunDuration);
            public const string HpCostMultiplier = nameof(HpCostMultiplier);
            public const string MissingHpRate = nameof(MissingHpRate);
            public const string SecondDamage = nameof(SecondDamage);
            public const string SecondWeaponScale = nameof(SecondWeaponScale);
            public const string SecondSpellScale = nameof(SecondSpellScale);
            public const string ThirdDamage = nameof(ThirdDamage);
            public const string ThirdWeaponScale = nameof(ThirdWeaponScale);
            public const string ThirdSpellScale = nameof(ThirdSpellScale);
            public const string Stage1HpCost = nameof(Stage1HpCost);
            public const string Stage2HpCost = nameof(Stage2HpCost);
            public const string Stage3HpCost = nameof(Stage3HpCost);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.StunDuration, 2);
            parameters.RegisterDefault(Parameters.HpCostMultiplier, 1f);
            parameters.RegisterDefault(Parameters.MissingHpRate, 0f);
            parameters.RegisterDefault(Parameters.SecondDamage, 600f);
            parameters.RegisterDefault(Parameters.SecondWeaponScale, 1.2f);
            parameters.RegisterDefault(Parameters.SecondSpellScale, 1.2f);
            parameters.RegisterDefault(Parameters.ThirdDamage, 1000f);
            parameters.RegisterDefault(Parameters.ThirdWeaponScale, 1.8f);
            parameters.RegisterDefault(Parameters.ThirdSpellScale, 1.8f);
            parameters.RegisterDefault(Parameters.Stage1HpCost, 0.05f);
            parameters.RegisterDefault(Parameters.Stage2HpCost, 0.15f);
            parameters.RegisterDefault(Parameters.Stage3HpCost, 0.30f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new Armageddon(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            // "При отсутствии необходимого уровня здоровья активируется последняя достигнутая стадия."
            int stage = Charge.ConsumeStage();
            owner.ConsumeResource(Costs.Health, HpCost(stage));

            float abilityDamage = StageDamage(stage, owner);
            foreach (IFightable target in HitSequence.GetHitSequence(owner, targets, field))
            {
                if (!target.IsAlive) continue;
                float total = abilityDamage;
                if (MissingHpRate > 0)
                    total += (target.Parameters.MaxHealth - target.CurrentHealth) / MissingHpStep * MissingHpRate;

                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
                context.Add(DamageType.Physical, total);
                await target.TakeDamage(context);

                if (stage >= MaxStage) await ApplyStageThreeEffects(owner, target, context.TotalDamage);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, IsCritical: false, context.TotalDamage));
            }
        }

        private async Task ApplyStageThreeEffects(IFightable owner, IFightable target, float damageDealt)
        {
            await new StunEffect(StunDuration).Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });
            if (Stage3EffectFactory == null) return;
            await Stage3EffectFactory().ApplyStacks(
                new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId, Damage = damageDealt },
                Stage3EffectStacks);
        }

        private float HpCost(int stage) => (Owner?.Parameters.MaxHealth ?? 0) * StageHpPercent(stage) * HpCostMultiplier;

        private float StageHpPercent(int stage) => stage switch
        {
            1 => this[Parameters.Stage1HpCost],
            2 => this[Parameters.Stage2HpCost],
            _ => this[Parameters.Stage3HpCost]
        };

        private float StageDamage(int stage, IFightable owner) => stage switch
        {
            1 => Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale),
            2 => this[Parameters.SecondDamage] + (owner.Parameters.Damage * this[Parameters.SecondWeaponScale]) + (owner.Parameters.SpellDamage * this[Parameters.SecondSpellScale]),
            _ => this[Parameters.ThirdDamage] + (owner.Parameters.Damage * this[Parameters.ThirdWeaponScale]) + (owner.Parameters.SpellDamage * this[Parameters.ThirdSpellScale])
        };
    }
}
