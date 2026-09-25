namespace Core.Data
{
    using System.Collections.Generic;
    using Entity;

    public interface INpcModifierProvider
    {
        INpcModifier GetModifier(string id);
        List<string> GetAllModifierIds();
        IReadOnlyList<INpcModifier> GetAllModifiers();
    }
}
