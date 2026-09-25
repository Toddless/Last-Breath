namespace Core.Enums
{
    public enum OperationType : byte
    {
        Add,
        Subtract,
        Divide,
        Multiply,
        /// <summary>
        /// Replaces the accumulated value entirely, ignoring the base and the modifiers below in the chain.
        /// Combine with <see cref="Priority.Absolute"/> to guarantee it applies last ("parameter equals X");
        /// with a lower priority the modifiers above in the chain still apply on top of the overridden value.
        /// The only meaningful operation for categorical parameters stored as float (e.g. CostType).
        /// </summary>
        Override
    }
}
