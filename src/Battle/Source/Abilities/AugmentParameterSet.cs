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
    /// everything else it wears exactly where it was. Uniqueness of the pair is not free any more — a
    /// record naming both the book's scale key and a private member of the same family would land twice on
    /// that member and only one of the two would ever come off — so it is asserted rather than assumed
    /// (<c>NoTableRecordNamesTwoMembersOfOneFamily</c>). Whether the move actually works once it is seated is
    /// another question and is not settled by the id — two augments reaching for the same parameter the
    /// same way are one effect, and only the strongest of them is read (<see cref="AbilityEffectIdentity"/>).
    ///
    /// A key names one number everywhere except the two damage scales, which name a family: the ability
    /// answers with every coefficient its delivery reads (<see cref="AbilityParameterSet.Family"/>) and
    /// the move is laid on each of them. Scale points are sold to the CAST, so a delivery of several
    /// figures gets them all rather than whichever figure the book's own key happens to carry.
    /// </summary>
    public class AugmentParameterSet(
        string id,
        string[] tags,
        int tier,
        IReadOnlyList<(string Parameter, OperationType Operation, float Amount)> moves)
        : Augment<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability)
        {
            foreach ((string move, OperationType operation, float amount) in moves)
                foreach (string parameter in ability.Family(move))
                    ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                        parameter, Priority.Weak, operation, amount, DecoratorId(parameter), Id, rank: Tier));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            foreach ((string move, _, _) in moves)
                foreach (string parameter in ability.Family(move))
                    ability.RemoveParameterDecorator(DecoratorId(parameter), parameter);
        }

        public override IAugment Copy() => new AugmentParameterSet(Id, Tags, Tier, moves);
    }
}
