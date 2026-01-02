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
        [Export] private Label? _effectStacks;

        public int Stacks
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                UpdateStacks();
            }
        }

        public override GodotObject _MakeCustomTooltip(string forText)
        {
            var richText = new RichTextLabel { Text = _effect?.Description, AutowrapMode = TextServer.AutowrapMode.Off, FitContent = true, BbcodeEnabled = true};
            return richText;
        }

        public void AddEffect(IEffect effect)
        {
            _effect = effect;
            _effectIcon?.Texture = _effect.Icon;
            _effectStacks?.Text = string.Empty;
        }

        public void RemoveEffect()
        {
            _effectStacks?.Text = string.Empty;
            _effectIcon?.Texture = null;
            _effect = null;
            QueueFree();
        }

        public bool HasEffect(IEffect effect) => effect.Id.Equals(_effect?.Id);
        public bool HasOwner() => _effect?.Owner != null;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void UpdateStacks() => _effectStacks?.Text = Stacks <= 1 ? string.Empty : $"{Stacks}";
    }
}
