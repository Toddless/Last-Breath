namespace LootGeneration.Source
{
    using System;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Items;
    using Core.Views.UI;
    using Godot;
    using Godot.Collections;

    /// <summary>
    /// A single drop lying on the floor: rarity-colored beam/glow, category icon, drop/pickup
    /// animation and sound, hover name label. Deliberately dumb about WHO picks it up — a click
    /// only raises <see cref="PickedUp"/>; the orchestrator (or later a scavenger NPC through
    /// ILootOrchestrator.TryPickup) decides reach, inventory and batching.
    /// </summary>
    public partial class ItemOnGround : Node2D, IInitializable
    {
        private const string Uid = "uid://d4u6h8h88ft4";
        private const string AudioRoot = "res://Data/Shared/Assets/Audio/";
        private const float LabelAnimationDuration = 0.25f;
        private const float DropDurationMin = 0.25f;
        private const float DropDurationMax = 0.45f;

        [Export] private Area2D? _interactionArea;
        [Export] private Dictionary<Rarity, Color> _colors = [];
        [Export] private Dictionary<Rarity, AudioStream> _dropSounds = [];
        [Export] private AudioStream? _pickupSound;
        [Export] private Dictionary<EquipmentPiece, Texture2D> _equipGroundIcons = [];
        [Export] private Texture2D? _resourceIcon;
        [Export] private Texture2D? _craftingRecipeIcon;
        [Export] private Sprite2D? _lootIcon;
        [Export] private Sprite2D? _glowSprite;
        [Export] private MeshInstance2D? _itemBeam;
        [Export] private GpuParticles2D? _lootParticle;

        private float _animationDurationScale = 1f;
        private Vector2 _targetPosition;
        private Label? _itemName;
        private AudioStreamPlayer2D? _audio;
        private FloatingText? _activeNotice;
        private bool _pickupInProgress;
        private bool MouseInside { get; set; }

        public IItem? Item { get; private set; }
        public int Quantity { get; set; } = 1;
        public LootCategory Category { get; private set; } = LootCategory.Other;

        /// <summary>A pickup attempt (click on the item): the orchestrator decides reach and inventory.</summary>
        public event Action<ItemOnGround>? PickedUp;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(Uid);

        public override void _Ready()
        {
            _interactionArea?.MouseEntered += OnMouseEnter;
            _interactionArea?.MouseExited += OnMouseExit;
            _audio = new AudioStreamPlayer2D();
            AddChild(_audio);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
            if (!MouseInside || Item == null || _pickupInProgress) return;
            PickedUp?.Invoke(this);
        }

        public void SetItem(IItem item, int quantity = 1)
        {
            Item = item;
            Quantity = quantity;
            Category = LootCategoryResolver.Resolve(item);

            var color = RarityColor();
            // The scene's ShaderMaterials are shared between instances — duplicate before
            // tinting, otherwise every drop on the floor turns the color of the last one.
            if (_itemBeam?.Material is ShaderMaterial beam)
            {
                var own = (ShaderMaterial)beam.Duplicate();
                own.SetShaderParameter("beam_color", color);
                _itemBeam.Material = own;
            }

            if (_glowSprite?.Material is ShaderMaterial glow)
            {
                var own = (ShaderMaterial)glow.Duplicate();
                own.SetShaderParameter("glow_color", color);
                _glowSprite.Material = own;
            }

            if (_lootParticle != null) _lootParticle.Modulate = color;
            SetLootIcon(item);
        }

        public void SetPositionToTravelTo(Vector2 position, Vector2 targetPosition)
        {
            Position = position;
            _targetPosition = targetPosition;
        }

        public void SetAnimationDurationScale(float animationDurationScale) => _animationDurationScale = animationDurationScale;

        /// <summary>Toss from the spawn point to the landing spot, then flash particles and thud.</summary>
        public async Task AnimateAsync()
        {
            Scale = Vector2.Zero;
            var duration = Mathf.Max(0.1f, (float)GD.RandRange(DropDurationMin, DropDurationMax) * _animationDurationScale);

            var tween = CreateTween().SetParallel();
            tween.TweenProperty(this, "position", _targetPosition, duration)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(this, "scale", Vector2.One, duration)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            await ToSignal(tween, Tween.SignalName.Finished);

            OnLanded();
        }

        /// <summary>The item entered an inventory: pop sound, shrink out, free the node.</summary>
        public async Task ConfirmPickupAsync()
        {
            _pickupInProgress = true;
            PlaySound(_pickupSound ?? LoadDefaultSound("loot_pickup"));

            var tween = CreateTween().SetParallel();
            tween.TweenProperty(this, "scale", Vector2.Zero, 0.2f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
            tween.TweenProperty(this, "position:y", Position.Y - 25f, 0.2f);
            await ToSignal(tween, Tween.SignalName.Finished);

            QueueFree();
        }

        /// <summary>Localized "no room" notice above the drop; throttled while one is visible.</summary>
        public void NotifyInventoryFull()
        {
            if (IsInstanceValid(_activeNotice) && _activeNotice?.IsQueuedForDeletion() == false) return;
            _activeNotice = FloatingText.Spawn(this, Tr("Loot_Inventory_Full"), Colors.OrangeRed);
        }

        private void OnLanded()
        {
            PlaySound(_dropSounds.TryGetValue(Item?.Rarity ?? Rarity.Common, out var custom) ? custom : LoadDefaultSound(DefaultDropSoundName()));
            if (_lootParticle != null)
            {
                _lootParticle.Visible = true;
                _lootParticle.Emitting = true;
            }

            var squash = CreateTween();
            squash.TweenProperty(this, "scale", new Vector2(1.15f, 0.85f), 0.06f);
            squash.TweenProperty(this, "scale", Vector2.One, 0.12f);
        }

        private string DefaultDropSoundName() => (Item?.Rarity ?? Rarity.Common) switch
        {
            Rarity.Common or Rarity.Uncommon => "loot_drop_low",
            Rarity.Rare or Rarity.Epic => "loot_drop_mid",
            _ => "loot_drop_high",
        };

        private static AudioStream? LoadDefaultSound(string name)
        {
            var path = $"{AudioRoot}{name}.wav";
            return ResourceLoader.Exists(path) ? ResourceLoader.Load<AudioStream>(path) : null;
        }

        private void PlaySound(AudioStream? stream)
        {
            if (_audio == null || stream == null) return;
            _audio.Stream = stream;
            _audio.PitchScale = 0.95f + GD.Randf() * 0.1f;
            _audio.Play();
        }

        private void SetLootIcon(IItem item)
        {
            var icon = item switch
            {
                IEquipItem equipItem when _equipGroundIcons.TryGetValue(equipItem.EquipmentPiece, out var pieceIcon) => pieceIcon,
                Core.Crafting.ICraftingResource when _resourceIcon != null => _resourceIcon,
                Core.Crafting.ICraftingRecipe when _craftingRecipeIcon != null => _craftingRecipeIcon,
                // No category art assigned yet — the item's own inventory icon beats a blank.
                _ => item.Icon,
            };

            if (icon != null) _lootIcon?.Texture = icon;
        }

        private Color RarityColor() => Item != null && _colors.TryGetValue(Item.Rarity, out var color) ? color : Colors.White;

        private void OnMouseEnter()
        {
            MouseInside = true;
            if (Item == null) return;
            _itemName ??= CreateNameLabel();

            var tween = CreateTween();
            tween.TweenProperty(_itemName, "scale", Vector2.One, LabelAnimationDuration)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        private void OnMouseExit()
        {
            MouseInside = false;
            if (_itemName == null) return;
            var tween = CreateTween();
            tween.TweenProperty(_itemName, "scale", Vector2.Zero, LabelAnimationDuration);
        }

        private Label CreateNameLabel()
        {
            var rarityName = Tr($"Rarity_{Item!.Rarity}");
            var label = new Label
            {
                Text = Quantity > 1 ? $"{Item.DisplayName} [x{Quantity}]\n{rarityName}" : $"{Item.DisplayName}\n{rarityName}",
                HorizontalAlignment = HorizontalAlignment.Center,
                Scale = Vector2.Zero,
                ZIndex = 50,
            };
            label.AddThemeColorOverride("font_color", RarityColor());
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 4);
            AddChild(label);
            label.ResetSize();
            label.Position = new Vector2(-label.Size.X / 2f, -label.Size.Y - 45f);
            label.PivotOffset = new Vector2(label.Size.X / 2f, label.Size.Y);
            return label;
        }
    }
}
