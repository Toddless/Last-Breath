namespace Battle.Source.UIElements
{
    using System;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;
    using Core.Views.UI;
    using Godot;
    using Stateless;

    public partial class AbilityButton : Control, IInitializable
    {
        private const string UID = "uid://bcq7vkx5c2o4";

        private enum State
        {
            Ready,
            Charging,
            SelectingTargets,
            NotAvailable,
        }

        private enum Trigger
        {
            Ready,
            Charging,
            SelectingTargets,
            NotAvailable,
        }

        /// <summary>Hold-to-charge pace for <see cref="IChargedAbility"/>: one stage per this many seconds.</summary>
        private const float ChargeSecondsPerStage = 0.5f;

        // Icon tints that tell the two "not available" causes apart: on cooldown vs. not enough resource.
        private static readonly Color s_cooldownTint = new(0.35f, 0.35f, 0.4f, 1f);
        private static readonly Color s_noResourceTint = new(0.5f, 0.55f, 0.85f, 1f);

        private readonly StateMachine<State, Trigger> _stateMachine = new(State.NotAvailable);
        private bool _isMouseInside, _isEnoughResources;
        private float _chargeTime;

        private int _chargeStage;

        // The player input window (see BattleHud.ApplyInputWindow): closed outside the player's
        // turn and while animations play. Blocks activation only — tooltips keep working.
        private bool _inputEnabled;
        private IAbility? _ability;
        private Key _slotNumber;

        /// <summary>Read access for the HUD's hover tooltip.</summary>
        public IAbility? CurrentAbility => _ability;

        private string _selectionId = string.Empty;
        private IBattleEventBus? _battleEventBus;
        [Export] private TextureRect? _background, _icon, _frame;
        [Export] private Label? _number, _cooldownLabel;

        public override void _Input(InputEvent @event)
        {
            if (!_inputEnabled || _ability == null) return;
            switch (@event)
            {
                case InputEventKey { Pressed: true, Echo: false } key when key.Keycode == _slotNumber:
                    OnActivationPressed();
                    break;
                case InputEventKey { Pressed: false } key when key.Keycode == _slotNumber && _stateMachine.State is State.Charging:
                    FinishCharging();
                    break;
                // The release may happen anywhere on screen, so it is handled globally, not in _GuiInput.
                case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left } when _stateMachine.State is State.Charging:
                    FinishCharging();
                    break;
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (!_inputEnabled || !_isMouseInside || _ability == null || _stateMachine.State is State.NotAvailable) return;
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                OnActivationPressed();
        }

        public override void _Ready()
        {
            MouseEntered += OnMouseEnter;
            MouseExited += OnMouseExit;
            ConfigureStateMachine();
            SetProcess(false);
        }

        public override void _Process(double delta)
        {
            if (_stateMachine.State is not State.Charging || _ability is not IChargedAbility charged)
            {
                SetProcess(false);
                return;
            }

            _chargeTime += (float)delta;
            int stage = Mathf.Min(1 + (int)(_chargeTime / ChargeSecondsPerStage), Mathf.Min(charged.MaxStage, charged.MaxAffordableStage));
            if (stage == _chargeStage) return;
            _chargeStage = stage;
            ShowChargeStage();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            DetachEventBus();
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
            _battleEventBus.Subscribe<TargetSelectionResolvedEvent>(OnSelectionResolved);
        }

        // Symmetric to SetBattleEventBus: a bus outliving the button (teardown without a battle
        // end) must not keep handlers on a removed node.
        private void DetachEventBus()
        {
            _battleEventBus?.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            _battleEventBus?.Unsubscribe<TargetSelectionResolvedEvent>(OnSelectionResolved);
            _battleEventBus = null;
        }

        public override void _ExitTree()
        {
            DetachEventBus();
            DetachCurrentAbility();
        }

        public void SetAbility(IAbility ability)
        {
            DetachCurrentAbility();
            _ability = ability;
            _ability.CooldownLeftChanges += OnCooldownChanges;
            _ability.AbilityResourceChanges += OnAbilityResourceChanges;
            _isEnoughResources = ability.IsEnoughResource();
            _icon?.Texture = _ability.Icon;
            CheckAbilityAvailable();
        }

        public void ClearAbility()
        {
            DetachCurrentAbility();
            _icon?.Texture = null;
            UpdateVisuals();
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
            if (enabled) return;
            if (_stateMachine.State is State.SelectingTargets) CancelTargetSelecting();
            if (_stateMachine.State is State.Charging) _stateMachine.Fire(Trigger.Ready);
        }

        public void SetNumber(int number)
        {
            _slotNumber = number.GetKeyAssociatedWithNumber();
            _number?.Text = number.ToString();
        }

        /// <summary>Charged abilities enter the hold-to-charge phase first; everything else activates directly.</summary>
        private void OnActivationPressed()
        {
            if (_stateMachine.State is State.Ready && _ability is IChargedAbility)
            {
                StartCharging();
                AcceptEvent();
                return;
            }

            ActivateOrConfirm();
        }

        /// <summary>
        /// First press opens the target-selection phase; a second press confirms it (commits Few/auto
        /// modes; the controller treats a confirm with nothing picked as a cancel). Single-target modes
        /// commit on the target click, so a second press there just cancels.
        /// </summary>
        private void ActivateOrConfirm()
        {
            switch (_stateMachine.State)
            {
                case State.Ready:
                    _stateMachine.Fire(Trigger.SelectingTargets);
                    break;
                case State.SelectingTargets:
                    _battleEventBus?.Publish<ConfirmSelectionEvent>(new(_selectionId));
                    break;
            }

            AcceptEvent();
        }

        private void StartCharging()
        {
            _chargeTime = 0f;
            _chargeStage = 1;
            _stateMachine.Fire(Trigger.Charging);
            ShowChargeStage();
            SetProcess(true);
        }

        /// <summary>Button released: the stage is locked in and the usual target-selection phase begins.</summary>
        private void FinishCharging()
        {
            if (_ability is IChargedAbility charged) charged.PendingStage = _chargeStage;
            _stateMachine.Fire(Trigger.SelectingTargets);
            AcceptEvent();
        }

        private void ShowChargeStage()
        {
            _cooldownLabel?.Show();
            _cooldownLabel?.Text = _chargeStage.ToString();
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
                .Permit(Trigger.Charging, State.Charging)
                .Permit(Trigger.SelectingTargets, State.SelectingTargets);

            _stateMachine.Configure(State.Charging)
                .OnExit(() =>
                {
                    SetProcess(false);
                    UpdateVisuals();
                })
                .Permit(Trigger.SelectingTargets, State.SelectingTargets)
                .Permit(Trigger.Ready, State.Ready)
                .Permit(Trigger.NotAvailable, State.NotAvailable);

            _stateMachine.Configure(State.SelectingTargets)
                .OnEntry(() =>
                {
                    if (_ability == null) return;
                    _selectionId = Guid.NewGuid().ToString();
                    _battleEventBus?.Publish<PlayerSelectingTargetForAbilityEvent>(new(_ability, _selectionId));
                })
                .Permit(Trigger.Ready, State.Ready)
                .Permit(Trigger.NotAvailable, State.NotAvailable);

            _stateMachine.Configure(State.NotAvailable)
                .OnEntry(() => { _icon?.SetModulate(new Color(1, 1, 1, 0.7f)); })
                .OnExit(() => { _icon?.SetModulate(Colors.White); })
                .Permit(Trigger.Ready, State.Ready);
        }

        /// <summary>The controller ended the selection (commit or cancel): reset and re-check availability.</summary>
        private void OnSelectionResolved(TargetSelectionResolvedEvent evt)
        {
            if (evt.SelectionId != _selectionId || _stateMachine.State is not State.SelectingTargets) return;
            _selectionId = string.Empty;
            _stateMachine.Fire(Trigger.Ready);
            CheckAbilityAvailable();
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

            UpdateVisuals();
            return isAvailable;
        }

        /// <summary>Cooldown countdown over the icon; icon tint distinguishes cooldown from lack of resource.</summary>
        private void UpdateVisuals()
        {
            if (_ability == null)
            {
                _cooldownLabel?.Hide();
                _icon?.SetModulate(Colors.White);
                return;
            }

            int cooldownLeft = _ability.CooldownLeft;
            if (cooldownLeft > 0)
            {
                _cooldownLabel?.Show();
                _cooldownLabel?.Text = cooldownLeft.ToString();
                _icon?.SetModulate(s_cooldownTint);
                return;
            }

            _cooldownLabel?.Hide();
            _icon?.SetModulate(_isEnoughResources ? Colors.White : s_noResourceTint);
        }

        private void OnMouseExit() => _isMouseInside = false;

        private void OnMouseEnter() => _isMouseInside = true;

        private void OnBattleEnd(BattleEndEvent obj)
        {
            DetachCurrentAbility();
            DetachEventBus();
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
