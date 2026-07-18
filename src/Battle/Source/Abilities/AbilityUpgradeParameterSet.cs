namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Generic upgrade: additive bump of SEVERAL ability parameters at once
    /// (e.g. Ice Block T2: +150 damage, +15%/+45% scales — one upgrade, three decorators).</summary>
    public class AbilityUpgradeParameterSet(string id, string[] tags, int tier, IReadOnlyList<(string Parameter, float Amount)> bumps)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability)
        {
            foreach ((string parameter, float amount) in bumps)
                ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                    parameter, Priority.Weak, OperationType.Add, amount, DecoratorId(parameter), Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            foreach ((string parameter, _) in bumps)
                ability.RemoveParameterDecorator(DecoratorId(parameter), parameter);
        }

        public override IAbilityUpgrade Copy() => new AbilityUpgradeParameterSet(Id, Tags, Tier, bumps);

        private string DecoratorId(string parameter) => $"Ability_Parameter_Decorator_{Id}_{parameter}";
    }
}
