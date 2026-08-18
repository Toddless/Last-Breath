namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// "Everything this cast leaves on somebody lasts longer." The one record in the book that moves a
    /// FAMILY of keys instead of a named one: it asks the ability which of its numbers are durations of
    /// what it applies and decorates every one of them.
    ///
    /// The family is the ability's own word (<see cref="AbilityParameterSet.RegisterAppliedDuration"/>),
    /// and it has to be: a poison's turns, a stun's turns and the turns a debuff sits on the target are
    /// spelled differently by every ability that owns one, while the length of the buff a caster keeps on
    /// ITSELF is a different concept living on a shared key of its own. A registry naming the members
    /// centrally would have to know each ability's private keys — the very coupling shared keys exist to
    /// end — and would silently miss the next ability to lay something.
    ///
    /// An ability that puts nothing on anybody has an empty family, so the record fits it by tag and
    /// moves nothing. That is the ordinary silence of the catalog and is written down in the ledger.
    /// </summary>
    public class AbilityAugmentAppliedDuration(string id, string[] tags, int tier, float turns)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability)
        {
            foreach (string parameter in ability.AppliedDurations)
                ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                    parameter, Priority.Weak, OperationType.Add, turns, DecoratorId(parameter), Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            foreach (string parameter in ability.AppliedDurations)
                ability.RemoveParameterDecorator(DecoratorId(parameter), parameter);
        }

        public override IAbilityAugment Copy() => new AbilityAugmentAppliedDuration(Id, Tags, Tier, turns);

        private string DecoratorId(string parameter) => $"Ability_Parameter_Decorator_{Id}_{parameter}";
    }
}
