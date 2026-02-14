namespace Core.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces.Entity;

    public interface INpcModifierProvider
    {
        INpcModifier GetModifier(string id);
        List<string> GetAllModifierIds();
        IReadOnlyList<INpcModifier> GetAllModifiers();
        void LoadDataAsync();
    }
}
