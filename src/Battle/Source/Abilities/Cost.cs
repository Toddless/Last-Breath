namespace Battle.Source.Abilities
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>
    ///
    /// </summary>
    /// <param name="costValue">Actual value</param>
    /// <param name="resourceType">Define type of cost based on <see cref="Costs"/></param>
    public class Cost(Func<float> costValue, Func<int> resourceType) : IAbilityCost
    {
        public Costs Type => (Costs)resourceType.Invoke();
        public float Value => costValue.Invoke();
    }
}
