namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Takes a share off one of the ability's numbers — for records whose design line states what they
    /// change as a fraction rather than as a figure of their own. The share is measured against the
    /// ability's own base (<see cref="AbilityParameterShare"/>), so the record is worth the same
    /// wherever it is seated and whatever else is worn beside it, which is the whole reason a share
    /// cannot live in the plain parameter table.
    /// <para>The cooldown has a class of its own because it also carries a floor; anything else that
    /// only needs the share belongs here rather than in a class per record.</para>
    /// </summary>
    public class AbilityAugmentReduceParameter(string id, string[] tags, int tier, string parameter, float share)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_{Id}";

        public override void ApplyUpgrade(Ability ability) =>
            ability.AddParameterDecorator(new AbilityParameterShare(parameter, OperationType.Subtract, share, DecoratorId, Id));

        public override void RemoveUpgrade(Ability ability) => ability.RemoveParameterDecorator(DecoratorId, parameter);

        public override IAbilityAugment Copy() => new AbilityAugmentReduceParameter(Id, Tags, Tier, parameter, share);
    }
}
