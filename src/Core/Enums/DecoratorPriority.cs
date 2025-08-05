namespace Core.Enums
{
    public enum DecoratorPriority
    {
        /// <summary>
        /// Decorators cannot be the base. The base is only for the main module with the base logic.
        /// </summary>
        Base = 0,
        /// <summary>
        /// Weak decorators are deep within the chain, so they have less impact than strong ones.
        /// </summary>
        Weak,
        /// <summary>
        /// The most powerful decorators. They should be outside the chain.
        /// </summary>
        Strong
    }
}
