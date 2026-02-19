namespace LootGeneration.Source
{
    using Godot;
    using Core.Enums;
    using Godot.Collections;
    using Core.Interfaces.UI;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Crafting;

    public partial class ItemOnGround : Node2D, IInitializable
    {
        private const string Uid = "uid://d4u6h8h88ft4";
        private const float AnimationDuration = 0.4f;
        private const float GrabRange = 350f;

        private float _animationDurationScale = 1f;
        private Label? _itemName;
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
            switch (@event)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
                    PickUpItem();
                    break;
                case InputEventKey { Keycode: Key.Alt, Pressed: true }:
                    QueueFree();
                    break;
                case InputEventKey key when key.IsEcho():
                    ShowAllInfo();
                    break;
                case InputEventKey key2 when key2.IsReleased():
                    RemoveAllInfo();
                    break;
                default:
                    return;
            }
        }

        // Later add sound bus as parameter
        public async Task AnimateAsync()
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "position", _targetPosition, Mathf.Lerp(0f, GD.RandRange(0.1, AnimationDuration), GD.Randf()));
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
            SetLootIcon(item);
        }

        private void SetLootIcon(IItem item)
        {
            Texture2D? icon = null;
            switch (true)
            {
                case var _ when item is IEquipItem equipItem:
                    _equipGroundIcons.TryGetValue(equipItem.EquipmentPart, out icon);
                    break;
                case var _ when item is ICraftingResource:
                    icon = _resourceIcon;
                    break;
                case var _ when item is ICraftingRecipe:
                    icon = _craftingRecipeIcon;
                    break;
            }

            if (icon != null) _lootIcon?.Texture = icon;
        }

        public void SetAnimationDurationScale(float animationDurationScale) => _animationDurationScale = animationDurationScale;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(Uid);

        private void PickUpItem()
        {
        }

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
            if (Item == null || _itemName == null) return;
            var tween = CreateTween();
            tween.TweenProperty(_itemName, "scale", Vector2.Zero, AnimationDuration);
        }

        private void OnMouseEnter()
        {
            if (Item == null) return;
            if (_itemName == null)
            {
                _colors.TryGetValue(Item.Rarity, out var color);
                var label = new Label();
                label.Text = Quantity > 1 ? $"{Item.DisplayName} [x{Quantity}]\n{Item.Rarity}" : $"{Item.DisplayName}\n{Item.Rarity}";
                label.AddThemeColorOverride("font_color", color);
                label.Scale = Vector2.Zero;
                label.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
                _itemName = label;
                AddChild(_itemName);
            }

            var tween = CreateTween();
            tween.TweenProperty(_itemName, "scale", new Vector2(1f, 1f), AnimationDuration);
        }


        private void ShowItemName()
        {
        }

        private void OnBodyExit(Node2D body)
        {
        }

        private void OnBodyEnter(Node2D body)
        {
        }
    }
}
