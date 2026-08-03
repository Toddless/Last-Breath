namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Moves an ability parameter by a share of itself, for the augments that state what they change
    /// as a fraction instead of a number of their own — taken off the parameter with
    /// <see cref="OperationType.Subtract"/>, put on top of it with <see cref="OperationType.Add"/>.
    /// It sits at <see cref="Priority.Base"/> — the head of the parameter's chain — so the number the
    /// share is measured against is the ability's own base value and nothing else. Everything any
    /// other augment does to the parameter is applied on top of the result, which is what makes the
    /// change the same whatever order the player picked their augments in and however many times this
    /// one is taken off and put back on. Two shares on one parameter would read one another instead of
    /// the base, so the records carrying them are written at one tier and an ability wears one augment
    /// per tier.
    ///
    /// What the share comes to is rounded to a whole unit, half away from zero, and never down to
    /// nothing while there is anything to measure: costs are paid in whole points and cooldowns counted
    /// in whole turns, and an augment that rounds down to zero is chosen, worn, paid for and changes
    /// nothing — the silent refusal a share is worthless without. A base of nothing has nothing to give
    /// and is left alone, so a free and instant cast is neither handed a negative price nor charged for
    /// being free.
    /// </summary>
    public class AbilityParameterShare(string parameter, OperationType operation, float share, string id, string source)
        : AbilityParameterDecorator(parameter, Priority.Base, id, source)
    {
        public override float Decorate(float baseValue) => operation switch
        {
            OperationType.Add => baseValue + Share(baseValue),
            OperationType.Subtract => baseValue - Share(baseValue),
            _ => baseValue
        };

        private float Share(float baseValue) =>
            baseValue <= 0f
                ? 0f
                : Math.Max(1f, MathF.Round(baseValue * share, MidpointRounding.AwayFromZero));
    }
}
