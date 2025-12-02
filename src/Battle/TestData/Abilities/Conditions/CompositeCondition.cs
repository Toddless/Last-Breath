namespace Battle.TestData.Abilities.Conditions
{
    using System.Linq;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public class CompositeCondition(string id, ConditionOperator oper, List<ICondition> conditions) : ICondition
    {
        private List<ICondition> Conditions => conditions;
        private ConditionOperator Operator => oper;

        public string Id => id;

        public bool IsMet(EffectApplyingContext context) =>
            Operator == ConditionOperator.And
                ? Conditions.All(c => c.IsMet(context))
                : Conditions.Any(c => c.IsMet(context));
    }
}
