namespace Core.Interfaces
{
    public interface IModifierInstance : IModifier, IWeighable
    {
        /// <summary>
        /// Determines the order in which modifiers are applied. Lower number => higher priority. Use <see cref="ModifierPriorities"/> when creating a new modifier to set this property.
        /// </summary>
        int Priority { get; }

        string InstanceId { get; }

        /// <summary>
        /// Value
        /// </summary>
        object Source { get; }

        IModifierInstance Copy();
    }
}
