namespace Crafting.Source.UIElements
{
    using System;
    using Core.Interfaces.UI;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class ClickableRequirement : Control, IInitializable, IRequirementUi
    {
        private const string UID = "uid://naq3akxs2d3o";
        private IRequirementUiConfiguration? _configuration;
        [Export] private Color _enoughQuantityTextColor, _notEnoughQuantityTextColor;
        [Export] private int _resourceNameTextSize, _resourceQuantityTextSize;
        [Export] private Label? _text, _quantity;
        [Export] private TextureRect? _icon;

        // TODO: Change style when its clickable
        [Export] public bool IsClickable { get; set; } = true;
        public bool IsResourcesEnough { get; private set; }
        public bool IsResource { get; set; }

        public string Id
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                RequirementChanges?.Invoke();
            }
        } = string.Empty;

        public int Amount { get; private set; }
        public event Action<IRequirementUi>? LeftClick, RightClick;
        public event Action? RequirementChanges;

        public override void _Ready()
        {
            _quantity?.LabelSettings = new LabelSettings { FontSize = _resourceQuantityTextSize, };
            _text?.LabelSettings = new LabelSettings { FontSize = _resourceNameTextSize, };
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mb) return;
            if (!IsClickable) return;
            switch (true)
            {
                case var _ when mb is { ButtonIndex: MouseButton.Left, Pressed: true }:
                    LeftClick?.Invoke(this);
                    break;
                case var _ when mb is { ButtonIndex: MouseButton.Right, Pressed: true }:
                    RightClick?.Invoke(this);
                    break;
                default:
                    return;
            }

            AcceptEvent();
        }

        public void SetConfiguration(IRequirementUiConfiguration configuration)
        {
            _configuration = configuration;
            _configuration.Configure(this);
        }

        public void ClearConfiguration()
        {
            _configuration?.Dispose();
            _configuration = null;
            _text?.Text = string.Empty;
            _quantity?.Text = string.Empty;
            _icon?.Texture = null;
            Amount = 0;
            Id = string.Empty;
        }

        public void SetDisplayText(string displayText, int have, int need)
        {
            string amountNeed = need > 0 ? $"x{need}" : string.Empty;
            _text?.Text = $"{displayText} {amountNeed}";
            IsResourcesEnough = have >= need;
            Amount = need;
            _quantity?.LabelSettings.FontColor = IsResourcesEnough ? _enoughQuantityTextColor : _notEnoughQuantityTextColor;
            _quantity?.Text = $"({have})";
        }

        public void SetIcon(Texture2D? icon) => _icon?.Texture = icon;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
