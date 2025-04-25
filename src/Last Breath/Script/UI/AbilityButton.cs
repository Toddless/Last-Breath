namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Abilities.Interfaces;

    public partial class AbilityButton : TextureButton
    {
        private const string UID = "uid://rxcf0iao3twp";
        private IAbility? _ability;
        private bool _canActivate;

        public static PackedScene InitializeButton() => ResourceLoader.Load<PackedScene>(UID);

        public void BindAbility(IAbility ability)
        {
            _ability = ability;
            _ability.AbilityUpdateState += UpdateActivationState;
            Pressed += OnAbilityPressed;
            MouseEntered += OnMouseEntered;
            if (ability.Icon != null)
                TextureNormal = ability.Icon;
        }

        // TODO: Update ability state after battle start
        // update ability state after activation
        public void UnbindAbility()
        {
            if (_ability != null)
            {
                _ability.AbilityUpdateState -= UpdateActivationState;
                _ability = null;
            }
            Pressed -= OnAbilityPressed;
            MouseEntered -= OnMouseEntered;
            TextureNormal = null;
        }

        public bool AbilitySet() => _ability != null;

        private void UpdateActivationState()
        {
            if (_ability == null) return;
            _canActivate = _ability.AbilityCanActivate();
            UpdateButtonState();
        }

        private void OnMouseEntered()
        {
            while (IsHovered())
            {
                // TODO: Add ability description
                // ShowAbilityDescription();
            }
        }

        private void OnAbilityPressed()
        {
            if (_canActivate)
            {
                GD.Print($"Aсtivating ability: {_ability?.GetType().Name}");
                _ability?.Activate();
                UpdateActivationState();
            }
        }

        private void UpdateButtonState() => Modulate = _canActivate ? Colors.White : Colors.Gray;
    }
}
