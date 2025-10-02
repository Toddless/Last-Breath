namespace LastBreath.Addons.Crafting.Resources.Materials
{
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class MaterialModifier : Resource, IMaterialModifier
    {
        private float _value;
        private bool _isInitialized = false;
        [Export] public Parameter Parameter { get; set; }
        [Export] public ModifierType ModifierType { get; set; }
        [Export] public float BaseValue { get; private set; }
        [Export] public float Weight { get; set; }
        [Export]
        public float Value
        {
            get
            {

                if (!_isInitialized)
                {
                    _value = BaseValue;
                    _isInitialized = true;
                }
                return _value;
            }
            set => _value = value;

        }
    }
}
