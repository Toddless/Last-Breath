namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Takes a share off an ability parameter, for the augments that state their reduction as a
    /// fraction instead of a number of their own. It sits at <see cref="Priority.Base"/> — the head of
    /// the parameter's chain — so the number the share is measured against is the ability's own base
    /// value and nothing else. Everything any other augment does to the parameter is applied on top of
    /// the result, which is what makes the reduction the same whatever order the player picked their
    /// augments in and however many times this one is taken off and put back on.
    ///
    /// What the share comes to is rounded to a whole unit, half away from zero, and never down to
    /// nothing while there is anything to take: costs are paid in whole points and cooldowns counted in
    /// whole turns, and an augment that rounds down to zero is chosen, worn, paid for and changes
    /// nothing — the silent refusal a share is worthless without. A base of nothing has nothing to give
    /// and is left alone, so a free and instant cast is not handed a negative price.
    /// </summary>
    public class AbilityParameterShareCut(string parameter, float share, string id, string source)
        : AbilityParameterDecorator(parameter, Priority.Base, id, source)
    {
        public override float Decorate(float baseValue) => baseValue - Cut(baseValue);

        private float Cut(float baseValue) =>
            baseValue <= 0f
                ? 0f
                : Math.Max(1f, MathF.Round(baseValue * share, MidpointRounding.AwayFromZero));
    }
}
