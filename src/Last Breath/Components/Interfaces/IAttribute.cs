namespace LastBreath.Components.Interfaces
{
    using System.Collections.Generic;

    public interface IAttribute
    {
        int InvestedPoints { get; }
        List<AttributeModifier> AttributeModifiers();
    }
}
