namespace Core.Interfaces.Entity
{
    using System.Collections.Generic;

    public interface IAdditionalItemsModifier : INpcModifier
    {
        Dictionary<int, string> Items { get; }
    }
}
