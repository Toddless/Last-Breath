namespace Core.Enums
{
    public enum Priority : byte
    {
        /// <summary>
        /// First one in the chain.
        /// </summary>
        Base = 0,
        /// <summary>
        /// Deeper in the chain, so they have less impact than strong ones.
        /// </summary>
        Weak,
        /// <summary>
        /// Middle int the chain, they have more impact than weak or base one, but lower than absolute.
        /// </summary>
        Strong,
        /// <summary>
        /// Most impactful, only one of these can be within the chain.
        /// </summary>
        Absolute
    }
}
