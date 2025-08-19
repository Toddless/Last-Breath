namespace LastBreath.Addons.Crafting.Resources.Materials
{
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;
    using Godot;
    using Godot.Collections;
    using LastBreath.addons.Crafting.Resources.Materials;

    [Tool]
    [GlobalClass]
    public partial class MaterialType : Resource, IMaterialType
    {
        private IReadOnlyList<IMaterialModifier>? _cached;
        [Export] private MaterialCategory? _category;
        [Export] private Array<MaterialModifier> _modifiers = [];

        [Export] public string Id { get; set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];

        public string DisplayName => GetLocalizedMaterialName();

        public IMaterialCategory? MaterialCategory => _category;

        /// <summary>
        /// Combined modifiers
        /// </summary>
        public IReadOnlyList<IMaterialModifier> Modifiers
        {
            get
            {
                if(_cached != null) return _cached;
                var list = new List<IMaterialModifier>();

                if(MaterialCategory?.Modifiers is IReadOnlyList<IMaterialModifier> mods)
                {
                     list.AddRange(mods);
                }

                if(_modifiers.Count > 0)
                {
                    foreach (var item in _modifiers)
                    {
                        if(item != null)
                            list.Add(item);
                    }
                }
                _cached = list.AsReadOnly();

                return _cached;
            }
        }

   
        // we need to create a default, parameterless constructor in order to create a resource from within the Godot Editor.
        public MaterialType()
        {

        }
        /// <summary>
        /// Constructor to create a resource within code
        /// </summary>
        /// <param name="modifiers"></param>
        /// <param name="id"></param>
        /// <param name="tags"></param>
        /// <param name="category"></param>
        public MaterialType(Array<MaterialModifier> modifiers, string id, string[] tags, MaterialCategory category)
        {
            _modifiers = modifiers;
            Id = id;
            Tags = tags;
            _category = category;
        }


        private string GetLocalizedMaterialName() => TranslationServer.Translate(Id);
    }
}
