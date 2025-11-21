namespace Battle.TestData.Abilities
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class Cost(Func<int> costModule, Func<int> cost) : IAbilityCost
    {
        public Costs Resource => (Costs)cost.Invoke();
        public int Value => costModule.Invoke();
    }
}
