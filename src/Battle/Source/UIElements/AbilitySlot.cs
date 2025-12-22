namespace Battle.Source.UIElements
{
    using Godot;
    using System;
    using Stateless;
    using Core.Interfaces.UI;
    using Core.Interfaces.Events;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    [GlobalClass]
    public partial class AbilitySlot : Control, IInitializable
    {
        private const string UID = "uid://dubbkx1imyqop";

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
        }

        private readonly StateMachine<State, Trigger> _stateMachine = new(State.NotAvailable);
        private bool _isMouseInside = false, _isOnCooldown = false, _isEnoughResources = false;
        private IAbility? _ability;
        private string _selectionId = string.Empty;
        private IBattleEventBus? _battleEventBus;
        [Export] private TextureRect? _background, _icon, _frame;

        public override void _Input(InputEvent @event)
        {
            if (_ability == null) return;
            if (@event is InputEventKey { Keycode: Key.Escape, Pressed: true } && _stateMachine.State is State.SelectingTargets)
            {
                CancelTargetSelecting();
                AcceptEvent();
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            // TODO: Shortcuts
            if (!_isMouseInside || _ability == null || !CanActivate()) return;
            // TODO: Something usefully on RMB?
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                switch (_stateMachine.State)
                {
                    case State.Ready:
                        _stateMachine.Fire(Trigger.SelectingTargets);
                        break;
                    case State.SelectingTargets:
                        _stateMachine.Fire(Trigger.Activate);
                        break;
                    case State.NotAvailable:
                    case State.Activate:
                        return;
                }

                AcceptEvent();
            }
        }

        public override void _Ready()
        {
            MouseEntered += OnMouseEnter;
            MouseExited += OnMouseExit;

            ConfigureStateMachine();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<BattleEndGameEvent>(OnBattleEnd);
        }

        public void SetAbility(IAbility ability)
        {
            _ability?.AbilityResourceChanges -= OnAbilityResourceChanges;
            _ability?.CooldownLeftChanges -= OnCooldownChanges;
            _ability = ability;
            _ability.CooldownLeftChanges += OnCooldownChanges;
            _ability.AbilityResourceChanges += OnAbilityResourceChanges;
            if (_ability.IsEnoughResource()) _isEnoughResources = true;
            if (_ability.CooldownLeft > 0) _isOnCooldown = true;
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
                .OnEntry(() =>
                {
                    // TODO: Change frame to show ability is ready
                })
                .PermitReentry(Trigger.Ready)
                .PermitIf(Trigger.SelectingTargets, State.SelectingTargets, CanActivate);

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
                    var targets = new List<IEntity>();
                    _battleEventBus?.Publish<AbilityActivatedGameEvent>(new(_ability, targets));
                    _ability.Activate(targets);
                    _selectionId = string.Empty;
                })
                .Permit(Trigger.Ready, State.Ready);

            _stateMachine.Configure(State.NotAvailable)
                .OnEntry(() => { })
                .Permit(Trigger.Ready, State.Ready);
        }

        private bool CanActivate()
        {
            return _isEnoughResources && !_isOnCooldown;
        }

        private void OnMouseExit() => _isMouseInside = false;

        private void OnMouseEnter() => _isMouseInside = true;

        private void OnBattleEnd(BattleEndGameEvent obj)
        {
            _ability?.AbilityResourceChanges -= OnAbilityResourceChanges;
            _ability?.CooldownLeftChanges -= OnCooldownChanges;
            _battleEventBus?.Unsubscribe<BattleEndGameEvent>(OnBattleEnd);
            _ability = null;
            _battleEventBus = null;
        }

        private void OnCooldownChanges(IAbility abi, int cooldown)
        {
            if (abi.InstanceId != _ability?.InstanceId) return;
            GD.Print($"Ability cooldown left:{cooldown}");
            if (cooldown == 0) _isOnCooldown = false;
        }

        private void OnAbilityResourceChanges(IAbility obj, bool isEnough)
        {
            if (obj.InstanceId != _ability?.InstanceId) return;
            _isEnoughResources = isEnough;
        }
    }
}
