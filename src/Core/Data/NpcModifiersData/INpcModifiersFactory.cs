namespace Core.Data.NpcModifiersData
{
    using System.Collections.Generic;
    using Interfaces.Entity;

    public interface INpcModifiersFactory
    {
        List<INpcModifier> CreateNpcModifiers(Dictionary<string, List<NpcModifierData>> data);
    }
}
