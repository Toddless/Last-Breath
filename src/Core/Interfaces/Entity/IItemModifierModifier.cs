namespace Core.Interfaces.Entity
{
    using System.Collections.Generic;

    public interface IItemModifierModifier: INpcModifier
    {
        Dictionary<string, int> Modifiers { get; set; }
    }
}
