namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;

    public class SeriesOfAttacks(AbilityBaseData data) : DamagingAbility(data), IAttackModifierHost
    {
        public int MinAttacks => (int)this[Parameters.MinAttacks];
        public int MaxAttacks => (int)this[Parameters.MaxAttacks];
        public float DamageMultiplier => this[AbilityParameter.DamageMultiplier];
        public ISoAExecutionStrategy ExecutionStrategy { get; set; } = new SoAsDefaultExecutionStrategy();
        public AttackModifierPipeline AttackModifiers { get; } = new();

        /// <summary>The series is a RANGE, so the ability owns no <see cref="AbilityParameter.Attacks"/>:
        /// one number cannot say which end a record is buying.</summary>
        public static class Parameters
        {
            public const string MinAttacks = nameof(MinAttacks);
            public const string MaxAttacks = nameof(MaxAttacks);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterCriticalParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.AccuracyBonus, 0f);
            parameters.RegisterDefault(AbilityParameter.DamageMultiplier, 1f);
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
