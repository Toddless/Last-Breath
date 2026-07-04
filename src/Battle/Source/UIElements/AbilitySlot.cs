namespace Battle.Source.UIElements
{
    using System;
    using Core.Interfaces.Abilities;
    using Godot;

    public partial class AbilitySlot : Control
    {
        private const string UID = "uid://cbb427yap35dx";

        private IAbility? _ability;
        [Export] private TextureRect? _abilityIcon, _upgradeOne, _upgradeTwo, _upgradeThree;

        public event Action<IAbility>? AbilitySelected;
        // TODO:
        // При клике по слоту открываем полноценное описание способности и всех улучшений с возможностью выбора/смены улучшений.
        // Учитывая что на каждом уровне можно выбрать только одно улучшение из трех, radio button подойдет больше всего

        public override void _GuiInput(InputEvent @event)
        {
            // TODO:
            // if (@event is InputEventMouse)
            //     ShowShortAbilityDescription();
            // show description until mouse within this control node, remove when left
            if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } && _ability != null)
                AbilitySelected?.Invoke(_ability);
        }

        private void ShowShortAbilityDescription()
        {
        }

        public void SetAbility(IAbility ability)
        {
            _ability = ability;
            _abilityIcon?.Texture = _ability.Icon;
            // TODO:
            // Set upgrade icons for each tier
        }

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
