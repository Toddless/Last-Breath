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
    /// A decorator's id is the augment's id and the parameter it moves, which is what an upgrade takes
    /// off again: the pair names one move of one record, so taking this augment off an ability leaves
    /// everything else it wears exactly where it was. One augment never names the same parameter twice,
    /// so the pair is unique by construction. Whether the move actually works once it is seated is
    /// another question and is not settled by the id — two augments reaching for the same parameter the
    /// same way are one effect, and only the strongest of them is read (<see cref="AbilityEffectIdentity"/>).
    /// </summary>
    public class AbilityAugmentParameterSet(
        string id,
        string[] tags,
        int tier,
        IReadOnlyList<(string Parameter, OperationType Operation, float Amount)> moves)
        : AbilityAugment<Ability>(id, tags, tier)
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

        public override IAbilityAugment Copy() => new AbilityAugmentParameterSet(Id, Tags, Tier, moves);

        private string DecoratorId(string parameter) => $"Ability_Parameter_Decorator_{Id}_{parameter}";
    }
}
