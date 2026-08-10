namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;

    public class IncreasingPressure(AbilityBaseData data) : DamagingAbility(data)
    {
        public AttackModifierPipeline AttackModifiers { get; } = new();
        public float Attacks => this[AbilityParameter.Attacks];
        public float AttackDamageMultiplier => this[Parameters.AttackDamageStepMultiplier];
        public IIpExecutionStrategy ExecutionStrategy = new IpDefaultExecutionStrategy();

        /// <summary>The ability's bonus damage added on top of the owner's basic attack: flat + weapon- and spell-scaled.</summary>
        public float BonusDamage(IFightable owner) =>
            Damage + owner.Parameters.Damage * WeaponDamageScale + owner.Parameters.SpellDamage * SpellDamageScale;

        /// <summary>Full damage of a single hit at the given escalation multiplier — the owner's basic weapon attack plus
        /// <see cref="BonusDamage"/>. Single source of truth for every execution strategy.</summary>
        public float PerHitDamage(IFightable owner, float increase) =>
            (owner.Parameters.Damage + BonusDamage(owner)) * increase;

        public static class Parameters
        {
            /// <summary>How much each landed attack empowers the NEXT one — compounding down a series,
            /// not the cast-wide <see cref="AbilityParameter.DamageMultiplier"/>.</summary>
            public const string AttackDamageStepMultiplier = nameof(AttackDamageStepMultiplier);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Attacks, 5);
            parameters.RegisterDefault(Parameters.AttackDamageStepMultiplier, 0.15f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new IncreasingPressure(Data));

        public void AddAttackModifier(IAttackModifier modifier) => AttackModifiers.Add(modifier);
        public void RemoveAttackModifier(string id) => AttackModifiers.Remove(id);

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await ExecutionStrategy.Execute(this, owner, targets, field);
    }
}
