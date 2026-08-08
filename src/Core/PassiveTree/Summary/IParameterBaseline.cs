namespace Core.PassiveTree.Summary
{
    using Enums;

    /// <summary>
    /// The character the tree is measured on top of, reduced to the one question a summary asks: what a
    /// parameter is worth before the tree touches it. An interface rather than a type of its own so the
    /// game can answer from a live fighter and the authoring tool from an editable profile, neither
    /// having to become the other.
    /// </summary>
    public interface IParameterBaseline
    {
        float Of(EntityParameter parameter);
    }

    /// <summary>A character who is nothing but his tree. What a panel showing the CONTRIBUTION of an
    /// allocation reads against: the buckets are reported separately from the resolved total, so
    /// "+15 Strength, +20% armour" is readable with nothing underneath it.</summary>
    public sealed class NoBaseline : IParameterBaseline
    {
        public static readonly NoBaseline Instance = new();

        private NoBaseline()
        {
        }

        public float Of(EntityParameter parameter) => 0f;
    }
}
