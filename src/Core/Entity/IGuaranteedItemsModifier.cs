namespace Core.Entity
{
    using System.Collections.Generic;

    public interface IGuaranteedItemsModifier: INpcModifier
    {
        List<string> Items { get; }
    }
}
