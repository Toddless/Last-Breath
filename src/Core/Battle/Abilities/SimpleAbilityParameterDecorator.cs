namespace Core.Battle.Abilities
{
    using Enums;

    public class SimpleAbilityParameterDecorator(string parameter, Priority priority, OperationType type, float value, string id, string source)
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
            OperationType.Subtract => baseValue - value,
            OperationType.Divide => baseValue / value,
            OperationType.Multiply => baseValue * value,
            OperationType.Override => value,
            _ => baseValue
        };

        private static AbilityEffectDirection Moved(bool raises) =>
            raises ? AbilityEffectDirection.Raise : AbilityEffectDirection.Lower;
    }
}
