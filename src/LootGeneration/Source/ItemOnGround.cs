namespace LootGeneration.Source
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Items;
    using Core.Interfaces.UI;
    using Godot;
    using Godot.Collections;

    public partial class ItemOnGround : Node2D, IInitializable
    {
        private const string Uid = "uid://d4u6h8h88ft4";
        private const float AnimationDuration = 0.1f;

        private float _animationDurationScale = 1f;
        private Label? _label;
        private bool MouseInside { get; set; }
        private Vector2 _targetPosition;
        [Export] private Area2D? _interactionArea;
        [Export] private Dictionary<Rarity, Color> _colors = [];
        [Export] private Dictionary<Rarity, AudioStream> _dropSounds = [];
        [Export] private Dictionary<EquipmentType, Texture2D> _equipGroundIcons = [];
        [Export] private Texture2D? _resourceIcon;
        [Export] private Texture2D? _craftingRecipeIcon;
        [Export] private Sprite2D? _lootIcon;
        [Export] private MeshInstance2D? _itemBeam;
        [Export] private GpuParticles2D? _lootParticle;

        public int Quantity { get; set; } = 1;
        public IItem? Item { get; private set; }

        public override void _Ready()
        {
            _interactionArea?.MouseEntered += OnMouseEnter;
            _interactionArea?.MouseExited += OnMouseExit;
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey { Keycode: Key.Alt, Pressed: true })
                QueueFree();

            switch (@event)
            {
                case InputEventKey key when key.IsEcho():
                    ShowAllInfo();
                    break;
                case InputEventKey key2 when key2.IsReleased():
                    RemoveAllInfo();
                    break;
            }
        }

        public async Task AnimateAsync()
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "position", _targetPosition, _animationDurationScale * AnimationDuration);
            await ToSignal(tween, "finished");
        }

        public void SetPositionToTravelTo(Vector2 position, Vector2 targetPosition)
        {
            Position = position;
            _targetPosition = targetPosition;
        }

        public void SetItem(IItem item, int quantity = 1)
        {
            Item = item;
            Quantity = quantity;
            if (_itemBeam?.Material is ShaderMaterial shaderMaterial)
                shaderMaterial.SetShaderParameter("MainColor", _colors[Item.Rarity]);
        }

        public void SetAnimationDurationScale(float animationDurationScale) => _animationDurationScale = animationDurationScale;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(Uid);

        private void RemoveAllInfo()
        {
            OnMouseExit();
        }

        private void ShowAllInfo()
        {
            OnMouseEnter();
        }

        private void OnMouseExit()
        {
            if (_label == null) return;
            var tween = CreateTween();
            tween.TweenProperty(_label, "scale", Vector2.Zero, AnimationDuration);
        }

        private void OnMouseEnter()
        {
            if (Item == null) return;
            if (_label == null)
            {
                _colors.TryGetValue(Item.Rarity, out var color);
                var label = new Label();
                label.Text = Quantity > 1 ? $"{Item.DisplayName} [x{Quantity}]\n{Item.Rarity}" : $"{Item.DisplayName}\n{Item.Rarity}";
                label.AddThemeColorOverride("font_color", color);
                label.Scale = Vector2.Zero;
                label.SetAnchorsPreset(Control.LayoutPreset.CenterTop);

                AddChild(label);
                _label = label;
            }

            var tween = CreateTween();
            tween.TweenProperty(_label, "scale", new Vector2(1f, 1f), AnimationDuration);
        }

        private void OnBodyExit(Node2D body)
        {
        }

        private void OnBodyEnter(Node2D body)
        {
        }
    }
}
