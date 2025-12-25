namespace Battle.TestData
{
    using Godot;
    using Stateless;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;

    public partial class EntitySpot : Node2D
    {
        private enum State
        {
            CanBeSelected,
            SelectedForAbility,
            CannotBeSelected
        }

        private enum Trigger
        {
            CanBeSelected,
            SelectForAbility,
            CannotBeSelected
        }

        private StateMachine<State, Trigger> _stateMachine = new(State.CanBeSelected);
        private bool _mouseInside;
        private IEntity? _entity;
        private string _selectionId = string.Empty;
        private IBattleEventBus? _eventBus;
        [Export] private Area2D? _spotArea;

        public override void _Ready()
        {
            _spotArea?.MouseEntered += OnMouseEntered;
            _spotArea?.MouseExited += OnMouseExited;
            ConfigureStateMachine();
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.CanBeSelected)
                .OnEntry(() => { })
                .PermitReentry(Trigger.CanBeSelected)
                .Permit(Trigger.CannotBeSelected, State.CannotBeSelected)
                .Permit(Trigger.SelectForAbility, State.SelectedForAbility);

            _stateMachine.Configure(State.SelectedForAbility)
                .OnEntry(() => { })
                .Permit(Trigger.CanBeSelected, State.CanBeSelected);

            _stateMachine.Configure(State.CannotBeSelected)
                .OnEntry(() => { })
                .Permit(Trigger.CanBeSelected, State.CanBeSelected);
        }

        public void RemoveEntityFromSpot()
        {
            if (_entity is not { IsAlive: true }) return;
            _entity.Dead -= OnEntityDead;
            _entity.DamageTaken -= OnDamageTaken;
            var node = _entity as Node;
            CallDeferred(Node.MethodName.RemoveChild, node);
        }

        public void SetEntity(IEntity entity)
        {
            entity.Dead += OnEntityDead;
            entity.DamageTaken += OnDamageTaken;
            var body = entity as CharacterBody2D;
            _entity = entity;
            body?.Position = Vector2.Zero;
            CallDeferred(Node.MethodName.AddChild, body);
        }

        public void RemoveBattleEventBus()
        {
            _eventBus = null;
        }

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            _eventBus = battleEventBus;
            _eventBus.Subscribe<PlayerSelectingTargetForAbilityEvent>(OnPlayerSelectingAbilityTarget);
            _eventBus.Subscribe<AbilityActivatedGameEvent>(OnAbilityActivated);
            _eventBus.Subscribe<CancelSelectionEvent>(OnSelectionCancel);
        }

        private void OnSelectionCancel(CancelSelectionEvent obj)
        {
            if (string.IsNullOrWhiteSpace(_selectionId) || obj.SelectionId != _selectionId) return;
            _stateMachine.Fire(Trigger.CanBeSelected);
            _selectionId = string.Empty;
        }

        private void OnAbilityActivated(AbilityActivatedGameEvent obj)
        {
            if (_entity == null || _stateMachine.State is not State.SelectedForAbility) return;
            if (obj.Targets.Contains(_entity)) return;
            obj.Targets.Add(_entity);
            _selectionId = string.Empty;
            _stateMachine.Fire(Trigger.CanBeSelected);
        }

        private void OnPlayerSelectingAbilityTarget(PlayerSelectingTargetForAbilityEvent evnt)
        {
            if (_entity == null) return;
            var ability = evnt.Ability;
            _selectionId = evnt.SelectionId;
            if (_stateMachine.State is not State.CanBeSelected)
                _stateMachine.Fire(Trigger.CanBeSelected);
        }

        private void OnDamageTaken(float damage, DamageType type, bool isCrit)
        {
            var damageNumber = DamageNumber.Initialize().Instantiate<DamageNumber>();
            damageNumber.Play(Mathf.RoundToInt(damage), type, isCrit);
            CallDeferred(Node.MethodName.AddChild, damageNumber);
        }

        private void OnEntityDead(IFightable obj)
        {
            _entity?.DamageTaken -= OnDamageTaken;
            _entity?.Dead -= OnEntityDead;
            _entity = null;
            _selectionId = string.Empty;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_stateMachine.State is State.CannotBeSelected || _entity == null || !_mouseInside) return;
            if (@event is not InputEventMouseButton { Pressed : true, ButtonIndex: MouseButton.Left }) return;

            if (string.IsNullOrWhiteSpace(_selectionId))
                _eventBus?.Publish<AttackTargetSelectedGameEvent>(new(_entity));
            else
                _stateMachine.Fire(Trigger.SelectForAbility);
            GetViewport().SetInputAsHandled();
        }

        private void OnMouseEntered()
        {
            _mouseInside = true;
        }

        private void OnMouseExited()
        {
            _mouseInside = false;
        }
    }
}
