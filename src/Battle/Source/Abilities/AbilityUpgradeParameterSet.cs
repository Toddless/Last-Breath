namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// The one upgrade behind every augment that does nothing but move numbers: it lays a decorator on
    /// each parameter it is handed and takes the same ones off again. What separates such augments from
    /// one another is the key they stand on, what they do to the value there and by how much — three
    /// things that are data, so they arrive as data and no augment of this kind needs a class of its own.
    ///
    /// A decorator's id is the augment's id and the parameter it moves, which is the pair that has to be
    /// unique: two augments worn on the same ability and reaching for the same parameter must leave two
    /// decorators, because a shared id makes the second one silently lose to the first (see
    /// <see cref="AbilityParameterSet.AddDecorator"/>) and the player pays for an augment that does
    /// nothing. One augment never names the same parameter twice, so the pair is unique by construction.
    /// </summary>
    public class AbilityUpgradeParameterSet(
        string id,
        string[] tags,
        int tier,
        IReadOnlyList<(string Parameter, OperationType Operation, float Amount)> moves)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability)
        {
            foreach ((string parameter, OperationType operation, float amount) in moves)
                ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                    parameter, Priority.Weak, operation, amount, DecoratorId(parameter), Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            foreach ((string parameter, _, _) in moves)
                ability.RemoveParameterDecorator(DecoratorId(parameter), parameter);
        }

        public override IAbilityUpgrade Copy() => new AbilityUpgradeParameterSet(Id, Tags, Tier, moves);

        private string DecoratorId(string parameter) => $"Ability_Parameter_Decorator_{Id}_{parameter}";
    }
}
