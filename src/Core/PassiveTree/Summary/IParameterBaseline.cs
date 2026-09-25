namespace Core.PassiveTree.Summary
{
    using Enums;

    /// <summary>The character the tree is measured on top of, reduced to one question: what a parameter is
    /// worth before the tree touches it. An interface rather than a type of its own so the game can answer
    /// from a live fighter and the authoring tool from an editable profile, neither becoming the other.</summary>
    public interface IParameterBaseline
    {
        float Of(EntityParameter parameter);
    }

    /// <summary>A character who is nothing but his tree. What a panel showing an allocation's CONTRIBUTION
    /// reads against, so "+15 Strength, +20% armour" is readable with nothing underneath it.</summary>
    public sealed class NoBaseline : IParameterBaseline
    {
        public static readonly NoBaseline Instance = new();

        private NoBaseline()
        {
        }

        public float Of(EntityParameter parameter) => 0f;
    }
}
