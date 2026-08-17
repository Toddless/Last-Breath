namespace Battle.Source.Abilities.Porcupine
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Effects;

    /// <summary>
    /// Self-buff: for a few turns every enemy hit is answered with pure damage — a share of the taken
    /// damage plus a share of the bearer's armor. The activatable sibling of PorcupinePassiveSkill.
    /// </summary>
    public class Porcupine(AbilityBaseData data) : Ability(data)
    {
        public int Duration => (int)this[AbilityParameter.Duration];
        public float DamageReturn => this[Parameters.DamageReturn];
        public float ArmorReturn => this[Parameters.ArmorReturn];
        public float HealOnHit => this[Parameters.HealOnHit];
        public float CooldownReductionChance => this[AbilityParameter.CooldownReductionChance];

        public static class Parameters
        {
            public const string DamageReturn = nameof(DamageReturn);
            public const string ArmorReturn = nameof(ArmorReturn);
            public const string HealOnHit = nameof(HealOnHit);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterDefault(AbilityParameter.Duration, 3);
            parameters.RegisterDefault(Parameters.DamageReturn, 0.25f);
            parameters.RegisterDefault(Parameters.ArmorReturn, 0.15f);
            parameters.RegisterDefault(Parameters.HealOnHit, 0f);
            parameters.RegisterDefault(AbilityParameter.CooldownReductionChance, 0f);
        }

        public override IAbility Copy() => CopyUpgradesTo(new Porcupine(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
            await new PorcupineBuffEffect(this, Duration, DamageReturn, ArmorReturn, HealOnHit, CooldownReductionChance)
                .Apply(Laying(owner));
    }
}
