namespace Core.Interfaces
{
    using Core.Enums;

    public interface IModifier
    {
        /// <summary>
        /// Call this method only for <see cref="ModifierType.Flat"/> and <see cref="ModifierType.Multiplicative"/>. For <see cref="ModifierType.Increase"/> just use Linq.Sum
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ModifierType ModifierType { get; }
        Parameter Parameter { get; }
        float BaseValue { get; }
        float Value { get; set; }
    }
}
