namespace Battle.Source.UIElements
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Abilities;

    public partial class EffectSlot : Control, IInitializable
    {
        private const string UID = "uid://mrcrwvwn0w25";
        private IEffect? _effect;
        [Export] private TextureRect? _effectIcon;
        [Export] private Label? _effectDuration;


        // TODO: Custom popup

        public void AddEffect(IEffect effect)
        {
            if (_effect != null)
                _effect.DurationChanged -= OnEffectDurationChanges;
            _effect = effect;
            _effect.DurationChanged += OnEffectDurationChanges;
            _effectIcon?.Texture = _effect.Icon;
            _effectDuration?.Text = $"{_effect.Duration}";
        }

        public void RemoveEffect()
        {
            _effect?.DurationChanged -= OnEffectDurationChanges;
            _effectDuration?.Text = string.Empty;
            _effectIcon?.Texture = null;
            _effect = null;
            QueueFree();
        }

        public bool HasEffect(IEffect effect) => (!effect.InstanceId.Equals(_effect?.InstanceId));

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnEffectDurationChanges(int obj) => _effectDuration?.Text = obj < 2 ? string.Empty : $"{obj}";
    }
}
