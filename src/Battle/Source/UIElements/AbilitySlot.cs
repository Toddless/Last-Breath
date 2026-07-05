namespace Battle.Source.UIElements
{
    using System;
    using Core.Views;
    using Godot;

    /// <summary>
    /// One node of the stance mastery tree. Knows only the ability id and its unlock state (no domain
    /// object). Available slots are clickable and report their id up; locked slots are dimmed and inert.
    /// </summary>
    public partial class AbilitySlot : Control
    {
        private const string UID = "uid://cbb427yap35dx";
        private static readonly Color s_lockedTint = new(0.4f, 0.4f, 0.4f);

        [Export] private TextureRect? _abilityIcon, _upgradeOne, _upgradeTwo, _upgradeThree;
        [Export] private Label? _unlockLevel;

        private string _abilityId = string.Empty;
        private AbilityState _state = AbilityState.Locked;

        public event Action<string>? AbilitySelected;

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }
                && _state == AbilityState.Available
                && !string.IsNullOrEmpty(_abilityId))
                AbilitySelected?.Invoke(_abilityId);
        }

        public void SetView(AbilitySlotView view)
        {
            _abilityId = view.AbilityId;
            _state = view.State;
            _abilityIcon?.Texture = view.Icon;
            Modulate = view.State == AbilityState.Available ? Colors.White : s_lockedTint;

            _unlockLevel?.Visible = view.State == AbilityState.Locked;
            _unlockLevel?.Text = view.State == AbilityState.Locked ? view.UnlockLevel.ToString() : string.Empty;
        }

        // TODO: react on upgrade change to reflect chosen upgrades on the slot.
        public void SetUpgradeIcon(Texture2D icon, int tier)
        {
            _ = tier switch
            {
                1 => _upgradeOne?.Texture = icon,
                2 => _upgradeTwo?.Texture = icon,
                3 => _upgradeThree?.Texture = icon,
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
            };
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
