namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using Core.Enums;

    /// <summary>
    /// One number an augment moves, as the registry declares it: the parameter key it stands on, what
    /// it does to the value there, the property of the augment's own record that carries the amount,
    /// and the amount to use when the record carries no such property.
    ///
    /// The last of the four is the part that has to be written down. An augment whose record has lost
    /// its property does not fail to compile and does not fail to load — it reads a missing number as
    /// nothing at all and goes on being offered, chosen and worn while changing zero. Naming the
    /// fallback here keeps that number where the parameter and the operation already are, so the three
    /// things a numeric augment consists of are read in one line instead of three places.
    /// </summary>
    public readonly record struct AugmentParameterMove(
        string Parameter,
        OperationType Operation,
        string Property,
        float WithoutTheProperty)
    {
        /// <summary>What this move is worth on a given record.</summary>
        public float AmountIn(IReadOnlyDictionary<string, float> properties) =>
            properties.GetValueOrDefault(Property, WithoutTheProperty);
    }
}
