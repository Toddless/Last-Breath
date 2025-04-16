namespace Playground.Script.Abilities.Modifiers
{
    using Playground.Script.Enums;

    public interface IModifier
    {
        Parameter Parameter { get; }
        ModifierType Type { get; }
        /// <summary>
        /// Determines the order in which modifiers are applied. Lower number => higher priority. Use <see cref="ModifierPriorities"/> when creating a new modifier to set this property.
        /// </summary>
        int Priority { get; }
        float Value { get; }
        object Source {  get; }
        /// <summary>
        /// Call this method only for <see cref="ModifierType.Additive"/> and <see cref="ModifierType.Multiplicative"/>. For <see cref="ModifierType.MultiplicativeSum"/> just use Linq.Sum
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        float ModifyValue(float value);
    }
}
