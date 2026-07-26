namespace Core.Enums
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Membership map for aggregate ("all X") parameters. An aggregate is a bucket-only
    /// <see cref="EntityParameter"/> that stands for a whole family of concrete parameters ("all resistances").
    /// It is never read as a value: <c>ParameterModifiersComponent</c> folds an aggregate's modifiers into each
    /// family member at resolution and fans change events out to the members. Kept in code (structural, tied to
    /// the enum), not data.</summary>
    public static class AggregateParameters
    {
        private static readonly IReadOnlyDictionary<EntityParameter, EntityParameter[]> s_families =
            new Dictionary<EntityParameter, EntityParameter[]>
            {
                [EntityParameter.AllResistance] = [EntityParameter.FireResistance, EntityParameter.ColdResistance, EntityParameter.LightningResistance],
                [EntityParameter.AllAttribute] = [EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Intelligence],
                [EntityParameter.AllDefence] = [EntityParameter.Evade, EntityParameter.Armor],
                [EntityParameter.AllResistancePenetration] = [EntityParameter.FireResistancePenetration, EntityParameter.ColdResistancePenetration, EntityParameter.LightningResistancePenetration],
            };

        // Reverse index (concrete member -> aggregates that include it), built once for the resolution-time fold.
        private static readonly IReadOnlyDictionary<EntityParameter, EntityParameter[]> s_membership =
            s_families
                .SelectMany(family => family.Value.Select(member => (member, aggregate: family.Key)))
                .GroupBy(pair => pair.member)
                .ToDictionary(group => group.Key, group => group.Select(pair => pair.aggregate).ToArray());

        public static bool IsAggregate(EntityParameter parameter) => s_families.ContainsKey(parameter);

        /// <summary>Concrete members of an aggregate; empty for a non-aggregate parameter.</summary>
        public static IReadOnlyList<EntityParameter> Members(EntityParameter aggregate) =>
            s_families.TryGetValue(aggregate, out var members) ? members : [];

        /// <summary>Aggregates whose family contains this concrete parameter; empty if none.</summary>
        public static IReadOnlyList<EntityParameter> Aggregates(EntityParameter member) =>
            s_membership.TryGetValue(member, out var aggregates) ? aggregates : [];
    }
}
