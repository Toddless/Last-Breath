namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;

    public class SeriesOfAttacks(AbilityBaseData data) : DamagingAbility(data)
    {
        public int MinAttacks => (int)this[Parameters.MinAttacks];
        public int MaxAttacks => (int)this[Parameters.MaxAttacks];
        public float DamageMultiplier => this[AbilityParameter.DamageMultiplier];
        public ISoAExecutionStrategy ExecutionStrategy { get; set; } = new SoAsDefaultExecutionStrategy();
        public AttackModifierPipeline AttackModifiers { get; } = new();

        /// <summary>
        /// The series is a RANGE and not a count, which is why the ability does not register
        /// <see cref="AbilityParameter.Attacks"/>: a floor and a ceiling are two decisions, and one
        /// number offering "+1 attack" cannot say which of them it is buying. An augment on the common
        /// count is inert here on purpose — the two ends are moved by records that name them.
        /// </summary>
        public static class Parameters
        {
            public const string MinAttacks = nameof(MinAttacks);
            public const string MaxAttacks = nameof(MaxAttacks);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.DamageMultiplier, 1.3f);
            parameters.RegisterDefault(Parameters.MinAttacks, 2);
            parameters.RegisterDefault(Parameters.MaxAttacks, 5);
        }

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        public override IAbility Copy() => CopyUpgradesTo(new SeriesOfAttacks(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await ExecutionStrategy.Execute(this, owner, targets, field);
    }
}
