namespace LastBreath.Script.UI
{
    using System.Collections.Generic;
    using Godot;
    using LastBreath.Script.Enums;

    public partial class ResourceProgressBar : TextureProgressBar
    {
        private ResourceType _currentType;

        private class ResourceValue
        {
            public double Current;
            public double Max;
        }

        [Export]
        public Godot.Collections.Dictionary<ResourceType, Texture2D> TextureByResourceType { get; set; } = [];

        private readonly Dictionary<ResourceType, ResourceValue> _resourceValues = [];

        // Initial setup. Value and MaxValue are set in BattleUI
        public void SetResource(ResourceType type)
        {
            _currentType = type;
            UpdateTexture(type);
        }

        public void SetNewResource(ResourceType type, double current, double maxValue)
        {
            SaveCurrentValues();

            if (!_resourceValues.TryGetValue(type, out var value))
            {
                value = new ResourceValue();
                _resourceValues[type] = value;
            }
            value.Current = current;
            value.Max = maxValue;

            Value = current;
            MaxValue = maxValue;
            UpdateTexture(type);
            _currentType = type;
        }

        private void SaveCurrentValues()
        {
            if (_resourceValues.TryGetValue(_currentType, out var value))
            {
                value.Current = Value;
                value.Max = MaxValue;
                return;
            }

            _resourceValues[_currentType] = new ResourceValue
            {
                Current = Value,
                Max = MaxValue
            };
        }

        private void UpdateTexture(ResourceType type)
        {
            if (TextureByResourceType.TryGetValue(type, out var texture))
            {
                TextureProgress = texture;
                return;
            }

            // log
            TextureProgress = null;
        }
    }
}
