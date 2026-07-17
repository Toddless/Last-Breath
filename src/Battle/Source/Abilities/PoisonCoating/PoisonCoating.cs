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
    /// Self-cast buff. For <see cref="CoatingDuration"/> turns, each of the caster's attacks
    /// applies a poison stack to the target.
    /// </summary>
    public class PoisonCoating(AbilityBaseData data) : Ability(data)
    {
        public int CoatingDuration => (int)this[Parameters.CoatingDuration];
        public int PoisonDuration => (int)this[Parameters.PoisonDuration];
        public float PoisonDamagePercent => this[Parameters.PoisonMultiplier];

        public static class Parameters
        {
            public const string CoatingDuration = nameof(CoatingDuration);
            public const string PoisonDuration = nameof(PoisonDuration);
            public const string PoisonMultiplier = nameof(PoisonMultiplier);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.CoatingDuration, 3);
            parameters.RegisterDefault(Parameters.PoisonDuration, 5);
            parameters.RegisterDefault(Parameters.PoisonMultiplier, 0.45f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new PoisonCoating(Data));

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            // Apply the coating buff to the caster; PoisonCoatingEffect handles attack interception
            var coatingBuff = new PoisonCoatingEffect(
                duration: CoatingDuration,
                maxStacks: 1,
                poisonDuration: PoisonDuration,
                poisonDamagePercent: PoisonDamagePercent);

            coatingBuff.Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = InstanceId });

            return Task.CompletedTask;
        }
    }
}
