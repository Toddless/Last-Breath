namespace Battle.Source.UIElements
{
    using Godot;
    using Stateless;
    using Core.Interfaces.UI;
    using Core.Interfaces.Abilities;

    [GlobalClass]
    public partial class AbilitySlot : Control, IInitializable
    {
        private enum State
        {
            Ready,
            SelectingTargets,
            Activate,
            OnCooldown,
            NotEnoughResource,
        }

        private enum Trigger
        {
            Ready,
            SelectingTargets,
            Activate,
            OnCooldown,
            NotEnoughResource,
        }


        private readonly StateMachine<State, Trigger> _stateMachine = new(State.Ready);
        private bool _isMouseInside = false;
        private const string UID = "uid://dubbkx1imyqop";
        private IAbility? _ability;
        [Export] private TextureRect? _background, _icon, _frame;

        [Signal]
        public delegate void SelectingTargetsEventHandler();

        public override void _GuiInput(InputEvent @event)
        {
            if (!_isMouseInside) return;
            if (_stateMachine.State is State.OnCooldown or State.NotEnoughResource) return;
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;

            _stateMachine.Fire(_stateMachine.State == State.Ready ? Trigger.SelectingTargets : Trigger.Activate);
        }

        public override void _Ready()
        {
            MouseEntered += OnMouseEnter;
            MouseExited += OnMouseExit;

            ConfigureStateMachine();
        }

        private void ConfigureStateMachine()
        {
        }

        private void OnMouseExit()
        {
            _isMouseInside = false;
        }

        private void OnMouseEnter()
        {
            _isMouseInside = true;
        }

        public void SetAbility(IAbility ability)
        {
            _ability = ability;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
