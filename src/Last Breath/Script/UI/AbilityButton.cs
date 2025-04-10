namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Abilities.Interfaces;

    public partial class AbilityButton : TextureButton
    {
        private const string UID = "uid://rxcf0iao3twp";
        private IAbility? _ability;
        private ICharacter? _target;
        // On changing this bool we change ability state (for example ability will be gray if false)
        private bool _canActivate;
        // TODO: I can potentially  update ability activate state on target change

        public static PackedScene InitializeButton() => ResourceLoader.Load<PackedScene>(UID);

        public void BindAbility(IAbility ability)
        {
            _ability = ability;
            Pressed += OnAbilityPressed;
            MouseEntered += OnMouseEntered;
            UpdateActivationState();
        }

        // we update ability state on target change
        public void UpdateAbilityTarget(ICharacter target)
        {
            if (_ability == null) return;
            _target = target;
            UpdateActivationState();
        }

        private void UpdateActivationState()
        {
            if (_ability == null) return;
            if (_target == null)
            {
                _canActivate = false;
                UpdateButtonState();
                return;
            }
            _canActivate = _ability.AbilityCanActivate(_target);
            UpdateButtonState();
        }

        public void UpdateAbilityCooldown()
        {
            _ability?.UpdateCooldown();
            UpdateActivationState();
        }

        public void UnbindAbility()
        {
            Pressed -= OnAbilityPressed;
            MouseEntered -= OnMouseEntered;
            _target = null;
            _ability = null;
        }

        public bool AbilitySet() => _ability != null;
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
            if (!_canActivate || _target == null)
            {
                //TODO: For now just return, later change icon (more gray)
                return;
            }
            GD.Print($"ACtivating ability: {_ability?.GetType().Name}");
            _ability?.Activate(_target);
            UpdateActivationState();
        }

        private void UpdateButtonState() => Modulate = _canActivate ? Colors.White : Colors.Gray;
    }
}
