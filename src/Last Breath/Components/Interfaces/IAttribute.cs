namespace LastBreath.Components.Interfaces
{
    using System.Collections.Generic;
    using LastBreath.Script.Attribute;

    public interface IAttribute
    {
        int InvestedPoints { get; }
        List<AttributeModifier> AttributeModifiers();
    }
}
