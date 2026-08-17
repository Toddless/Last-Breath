namespace Core.Battle.Abilities
{
    using Enums;

    /// <param name="floor">How low a REDUCTION by this decorator may take the value. Read exactly as the
    /// floor of <see cref="AbilityParameterShare"/>: it only ever holds a cut back, never above the base
    /// itself, so it cannot be the thing that raises a number. Nought is the decorator as it always was.
    /// A general record cutting whole turns off a wait needs it — a cooldown driven below zero leaves
    /// <c>CooldownLeft</c> negative, which never counts back down to nought and never lets the ability be
    /// cast again.</param>
    public class SimpleAbilityParameterDecorator(
        string parameter, Priority priority, OperationType type, float value, string id, string source, float floor = 0f)
        : AbilityParameterDecorator(parameter, priority, id, source)
    {
        /// <summary>Read off the operation together with the amount it carries and not off the operation
        /// alone, so a move written with a negative number is the move it actually makes rather than the
        /// one its operation is named after.</summary>
        public override AbilityEffectDirection Direction => type switch
        {
            OperationType.Add => Moved(raises: value >= 0f),
            OperationType.Subtract => Moved(raises: value < 0f),
            OperationType.Multiply => Moved(raises: value >= 1f),
            OperationType.Divide => Moved(raises: value < 1f),
            _ => AbilityEffectDirection.Replace
        };

        public override float Decorate(float baseValue) => type switch
        {
            OperationType.Add => baseValue + value,
            OperationType.Subtract => Floored(baseValue - value, baseValue),
            OperationType.Divide => baseValue / value,
            OperationType.Multiply => baseValue * value,
            OperationType.Override => value,
            _ => baseValue
        };

        /// <summary>The reduced value, held back by the floor only when one was asked for — a decorator
        /// written without it keeps the reach it always had, negatives included.</summary>
        private float Floored(float reduced, float baseValue) =>
            floor <= 0f ? reduced : System.MathF.Max(reduced, System.MathF.Min(baseValue, floor));

        private static AbilityEffectDirection Moved(bool raises) =>
            raises ? AbilityEffectDirection.Raise : AbilityEffectDirection.Lower;
    }
}
