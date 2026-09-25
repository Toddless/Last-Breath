namespace Battle.Source.Abilities.PoisonCoating
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>
    /// Self-cast buff. For <see cref="Duration"/> turns, each of the caster's attacks
    /// applies a poison stack to the target.
    /// </summary>
    public class PoisonCoating(AbilityBaseData data) : Ability(data)
    {
        /// <summary>How long the coating buff itself holds on the caster — the common buff duration.</summary>
        public int Duration => (int)this[AbilityParameter.Duration];

        public int PoisonDuration => (int)this[AbilityParameter.PoisonDuration];
        public float PoisonDamagePercent => this[Parameters.PoisonMultiplier];

        /// <summary>Set by the record that buys a stack for every enemy: the coating lays one per living
        /// enemy instead of one per blow. A switch rather than a number — the count belongs to the field
        /// at the moment of the blow, not to the cast.</summary>
        public bool StacksPerLivingEnemy { get; set; }

        public static class Parameters
        {
            public const string PoisonMultiplier = nameof(PoisonMultiplier);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            // TODO:
            // Вынести магические цифры в статичный класс с константами для дефолтных значений
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterAppliedDuration(AbilityParameter.PoisonDuration, 5);
            parameters.RegisterDefault(Parameters.PoisonMultiplier, 0.45f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new PoisonCoating(Data));

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            // Apply the coating buff to the caster; PoisonCoatingEffect handles attack interception
            var coatingBuff = new PoisonCoatingEffect(
                duration: Duration,
                maxStacks: 1,
                poisonDuration: PoisonDuration,
                poisonDamagePercent: PoisonDamagePercent,
                stacksPerLivingEnemyOn: StacksPerLivingEnemy ? field : null);

            return coatingBuff.Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId, Trace = Trace });
        }
    }
}
