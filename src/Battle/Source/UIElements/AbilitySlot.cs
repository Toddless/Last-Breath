namespace Battle.Source.UIElements
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.UI;
    using Godot;
    using Stateless;

    public partial class AbilitySlot : Control, IInitializable
    {
        private const string UID = "uid://bcq7vkx5c2o4";

        private enum State
        {
            Ready,
            SelectingTargets,
            Activate,
            NotAvailable,
        }

        private enum Trigger
        {
            Ready,
            SelectingTargets,
            Activate,
            NotAvailable,
        }

        private readonly StateMachine<State, Trigger> _stateMachine = new(State.NotAvailable);
        private bool _isMouseInside = false, _isOnCooldown = false, _isEnoughResources = false;
        // The player input window (see BattleHud.ApplyInputWindow): closed outside the player's
        // turn and while animations play. Blocks activation only — tooltips keep working.
        private bool _inputEnabled;
        private IAbility? _ability;
        private Key _slotNumber;
        private string _selectionId = string.Empty;
        private IBattleEventBus? _battleEventBus;
        [Export] private TextureRect? _background, _icon, _frame;
        [Export] private Label? _number;

        public override void _Input(InputEvent @event)
        {
            if (!_inputEnabled || _ability == null) return;
            switch (true)
            {
                case var _ when @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && _stateMachine.State is State.SelectingTargets:
                    CancelTargetSelecting();
                    AcceptEvent();
                    break;
                case var _ when @event is InputEventKey { Pressed: true } input && input.Keycode == _slotNumber:
                    TryActivateAbility();
                    break;
            }
        }


        public override void _GuiInput(InputEvent @event)
        {
            // TODO: Shortcuts
            if (!_inputEnabled || !_isMouseInside || _ability == null || _stateMachine.State is State.NotAvailable) return;
            //  Something usefully on RMB?
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                TryActivateAbility();
        }

        public override void _Ready()
        {
            MouseEntered += OnMouseEnter;
            MouseExited += OnMouseExit;
            ConfigureStateMachine();
        }

        public override GodotObject? _MakeCustomTooltip(string forText)
        {
            if (_ability == null) return null;

            var abilityDescription = AbilityDescription.Initialize().Instantiate<AbilityDescription>();
            abilityDescription.SetupFullAbilityDescription(_ability);
            return abilityDescription;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        public void SetAbility(IAbility ability)
        {
            DetachCurrentAbility();
            _ability = ability;
            _ability.CooldownLeftChanges += OnCooldownChanges;
            _ability.AbilityResourceChanges += OnAbilityResourceChanges;
            _icon?.Texture = _ability.Icon;
            CheckAbilityAvailable();
        }

        public void ClearAbility()
        {
            DetachCurrentAbility();
            _icon?.Texture = null;
            if (_stateMachine.State is not State.NotAvailable && _stateMachine.CanFire(Trigger.NotAvailable))
                _stateMachine.Fire(Trigger.NotAvailable);
        }

        private void DetachCurrentAbility()
        {
            _ability?.AbilityResourceChanges -= OnAbilityResourceChanges;
            _ability?.CooldownLeftChanges -= OnCooldownChanges;
            _ability = null;
        }

        /// <summary>Closing the window mid target selection cancels it, otherwise the selection would hang.</summary>
        public void SetInputEnabled(bool enabled)
        {
            if (_inputEnabled == enabled) return;
            _inputEnabled = enabled;
            if (!enabled && _stateMachine.State is State.SelectingTargets)
                CancelTargetSelecting();
        }

        public void SetNumber(int number)
        {
            _slotNumber = number.GetKeyAssociatedWithNumber();
            _number?.Text = number.ToString();
        }

        private void TryActivateAbility()
        {
            switch (_stateMachine.State)
            {
                case State.Ready:
                    _stateMachine.Fire(Trigger.SelectingTargets);
                    break;
                case State.SelectingTargets:
                    _stateMachine.Fire(Trigger.Activate);
                    break;
            }

            AcceptEvent();
        }

        private void CancelTargetSelecting()
        {
            if (_selectionId == string.Empty) return;

            _battleEventBus?.Publish<CancelSelectionEvent>(new(_selectionId));
            _selectionId = string.Empty;
            _stateMachine.Fire(Trigger.Ready);
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.Ready)
                .Permit(Trigger.NotAvailable, State.NotAvailable)
                .Permit(Trigger.Activate, State.Activate)
                .Permit(Trigger.SelectingTargets, State.SelectingTargets);

            _stateMachine.Configure(State.SelectingTargets)
                .OnEntry(() =>
                {
                    if (_ability == null) return;
                    _selectionId = Guid.NewGuid().ToString();
                    _battleEventBus?.Publish<PlayerSelectingTargetForAbilityEvent>(new(_ability, _selectionId));
                })
                .Permit(Trigger.Ready, State.Ready)
                .Permit(Trigger.Activate, State.Activate);

            _stateMachine.Configure(State.Activate)
                .OnEntry(() =>
                {
                    if (_ability == null) return;
                    _battleEventBus?.Publish<AbilityActivationEvent>(new(_ability, _selectionId));
                    _selectionId = string.Empty;
                })
                .Permit(Trigger.NotAvailable, State.NotAvailable);

            _stateMachine.Configure(State.NotAvailable)
                .OnEntry(() => { _frame?.SetModulate(new Color(1, 1, 1, 0.7f)); })
                .OnExit(() => { _frame?.SetModulate(new Color(1, 1, 1, 0)); })
                .Permit(Trigger.Ready, State.Ready);
        }

        private bool CheckAbilityAvailable()
        {
            if (_ability == null) return false;
            bool isAvailable = _ability.CanActivate();
            switch (isAvailable)
            {
                case true when _stateMachine.State is not State.Ready:
                    if (_stateMachine.CanFire(Trigger.Ready))
                        _stateMachine.Fire(Trigger.Ready);
                    break;
                case false when _stateMachine.State is not State.NotAvailable:
                    if (_stateMachine.CanFire(Trigger.NotAvailable))
                        _stateMachine.Fire(Trigger.NotAvailable);
                    break;
            }

            return isAvailable;
        }

        private void OnMouseExit() => _isMouseInside = false;

        private void OnMouseEnter() => _isMouseInside = true;

        private void OnBattleEnd(BattleEndEvent obj)
        {
            _ability?.AbilityResourceChanges -= OnAbilityResourceChanges;
            _ability?.CooldownLeftChanges -= OnCooldownChanges;
            _ability = null;
            _battleEventBus = null;
        }

        private void OnCooldownChanges(IAbility abi, int cooldown)
        {
            if (abi.InstanceId != _ability?.InstanceId) return;
            CallDeferred(nameof(CheckAbilityAvailable));
        }

        private void OnAbilityResourceChanges(IAbility obj, bool isEnough)
        {
            if (obj.InstanceId != _ability?.InstanceId) return;
            _isEnoughResources = isEnough;
            CallDeferred(nameof(CheckAbilityAvailable));
        }
    }
}
