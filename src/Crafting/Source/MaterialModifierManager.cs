namespace Crafting.Source
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Crafting;

    public class MaterialModifierManager 
    {
        private readonly string _pathToModifierData;
        private Dictionary<MaterialCategory, IMaterialModifier[]> _materialModifiers = [];

        public MaterialModifierManager(string path)
        {
            _pathToModifierData = path;
        }


        public IMaterialModifier[] GetMaterialModifiersByCategory(MaterialCategory category)
        {
            _materialModifiers.TryGetValue(category, out var modifier);
            return modifier ?? [];
        }

        public void LoadData()
        {

        }
    }
}
