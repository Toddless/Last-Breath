namespace Battle.TestData
{
    using Godot;
    using Stateless;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;

    public partial class EntitySpot : Node2D
    {
        private enum State
        {
            CanBeSelected,
            CandidateForAbility,
            CannotBeSelected
        }

        private enum Trigger
        {
            SetCanBeSelected,
            SetAsCandidateForAbility,
            SetCannotBeSelected
        }

        private readonly StateMachine<State, Trigger> _stateMachine = new(State.CanBeSelected);
        private readonly StateMachine<State, Trigger>.TriggerWithParameters<string> _candidateForAbility = new(Trigger.SetAsCandidateForAbility);
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
                .PermitReentry(Trigger.SetCanBeSelected)
                .Permit(Trigger.SetCannotBeSelected, State.CannotBeSelected)
                .Permit(Trigger.SetAsCandidateForAbility, State.CandidateForAbility);

            _stateMachine.Configure(State.CandidateForAbility)
                .OnEntryFrom(_candidateForAbility, id => { _selectionId = id; })
                .OnExit(() => { _selectionId = string.Empty; })
                .PermitReentry(Trigger.SetAsCandidateForAbility)
                .Permit(Trigger.SetCanBeSelected, State.CanBeSelected);

            _stateMachine.Configure(State.CannotBeSelected)
                .Permit(Trigger.SetCanBeSelected, State.CanBeSelected);
        }

        public void RemoveEntityFromSpot()
        {
            if (_entity == null) return;
            _entity.Dead -= OnEntityDead;
            _entity.DamageTaken -= OnDamageTaken;
            _entity.Effects.EffectAdded -= OnEffectAdded;
            var node = _entity as Node;
            RemoveChild(node);
        }

        public void SetEntity(IEntity entity)
        {
            entity.Dead += OnEntityDead;
            entity.DamageTaken += OnDamageTaken;
            entity.Effects.EffectAdded += OnEffectAdded;
            var body = entity as CharacterBody2D;
            _entity = entity;
            body?.Position = Vector2.Zero;
            CallDeferred(Node.MethodName.AddChild, body);
        }

        private void OnEffectAdded(IEffect obj)
        {
            _stateMachine.Fire((_entity!.StatusEffects & StatusEffects.Vanished) != 0 ? Trigger.SetCannotBeSelected : Trigger.SetCanBeSelected);
        }

        public void RemoveBattleEventBus()
        {
            _eventBus = null;
        }

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            _eventBus = battleEventBus;
            _eventBus.Subscribe<PlayerSelectingTargetForAbilityEvent>(OnPlayerSelectingAbilityTarget);
            _eventBus.Subscribe<AbilityActivatedEvent>(OnAbilityActivated);
            _eventBus.Subscribe<CancelSelectionEvent>(OnSelectionCancel);
        }

        private void OnSelectionCancel(CancelSelectionEvent obj)
        {
            if (_stateMachine.State is not State.CandidateForAbility || _selectionId != obj.SelectionId) return;
            _stateMachine.Fire(Trigger.SetCanBeSelected);
        }

        private void OnAbilityActivated(AbilityActivatedEvent obj)
        {
            if (_entity == null || _stateMachine.State is not State.CandidateForAbility) return;
            if (obj.Targets.Contains(_entity) || obj.Targets.Count == (int)obj.Ability.MaxTargets) return;
            obj.Targets.Add(_entity);
            _stateMachine.Fire(Trigger.SetCanBeSelected);
        }

        private void OnPlayerSelectingAbilityTarget(PlayerSelectingTargetForAbilityEvent evnt)
        {
            if (_entity == null || _stateMachine.State is State.CannotBeSelected) return;
            if (evnt.Ability.AbilityType is AbilityType.SelfCast) return;
            _stateMachine.Fire(_candidateForAbility, evnt.SelectionId);
        }

        private void OnDamageTaken(float damage, DamageType type, bool isCrit)
        {
            var damageNumber = DamageNumber.Initialize().Instantiate<DamageNumber>();
            damageNumber.Play(Mathf.RoundToInt(damage), type, isCrit);
            CallDeferred(Node.MethodName.AddChild, damageNumber);
        }

        private void OnEntityDead(IFightable obj)
        {
            _stateMachine.Fire(Trigger.SetCannotBeSelected);
            _entity?.DamageTaken -= OnDamageTaken;
            _entity?.Dead -= OnEntityDead;
            _entity = null;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_stateMachine.State is State.CannotBeSelected || _entity == null || !_mouseInside) return;
            if (@event is not InputEventMouseButton { Pressed : true, ButtonIndex: MouseButton.Left }) return;
            if (!string.IsNullOrWhiteSpace(_selectionId)) return;

            _eventBus?.Publish<AttackTargetSelectedEvent>(new(_entity));
            GetViewport().SetInputAsHandled();
        }

        private void OnMouseEntered() => _mouseInside = true;

        private void OnMouseExited() => _mouseInside = false;
    }
}
