namespace Playground.Script.Enums
{
    public enum ModifierType
    {
        // order is matter!!!
        /// <summary>
        /// Raw value
        /// </summary>
        Additive = 0,
        /// <summary>
        /// Value should be 0.1, 0.3 etc
        /// </summary>
        MultiplicativeSum,
        /// <summary>
        /// Value should be 1.3, 1.1 etc.
        /// </summary>
        Multiplicative
    }
}
