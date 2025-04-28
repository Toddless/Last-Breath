namespace Playground.Components.Interfaces
{
    using System.Collections.Generic;
    using Playground.Script.Attribute;

    public interface IAttribute
    {
        int InvestedPoints { get; }
        List<AttributeModifier> AttributeModifiers();
    }
}
