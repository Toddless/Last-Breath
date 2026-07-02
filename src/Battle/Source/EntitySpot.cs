namespace Battle.Source
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Godot;
    using PassiveSkills;
    using Stateless;

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
        private IBattleEventBus? _eventBus;
        [Export] private Area2D? _spotArea;

        public string SelectionId { get; private set; } = string.Empty;
        public IFightable? Entity { get; private set; }

        public override void _Ready()
        {
            _spotArea?.InputEvent += OnInputEvent;
            ConfigureStateMachine();
        }

        private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (@event is not InputEventMouseButton { Pressed : true, ButtonIndex: MouseButton.Left }) return;
            if (_stateMachine.State is State.CannotBeSelected || Entity == null)
            {
                GD.Print($"First if return. State: {_stateMachine.State}, entity is null: {Entity}");
                return;
            }

            if (!string.IsNullOrWhiteSpace(SelectionId))
            {
                GD.Print($"Second if return. Selection id is null: {string.IsNullOrWhiteSpace(SelectionId)}");
                return;
            }

            _eventBus?.Publish<AttackTargetSelectedEvent>(new(Entity));
            GetViewport().SetInputAsHandled();
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.CanBeSelected)
                .PermitReentry(Trigger.SetCanBeSelected)
                .Permit(Trigger.SetCannotBeSelected, State.CannotBeSelected)
                .Permit(Trigger.SetAsCandidateForAbility, State.CandidateForAbility);

            _stateMachine.Configure(State.CandidateForAbility)
                .OnEntryFrom(_candidateForAbility, id => { SelectionId = id; })
                .OnExit(() => { SelectionId = string.Empty; })
                .PermitReentry(Trigger.SetAsCandidateForAbility)
                .Permit(Trigger.SetCanBeSelected, State.CanBeSelected);

            _stateMachine.Configure(State.CannotBeSelected)
                .Permit(Trigger.SetCanBeSelected, State.CanBeSelected);
        }

        public void RemoveEntityFromSpot()
        {
            if (Entity == null) return;
            Entity.Dead -= OnEntityDead;
            Entity.Effects.EffectAdded -= OnEffectAdded;
            var node = Entity as Node;
            RemoveChild(node);
        }

        public void SetEntity(IFightable entity)
        {
            entity.Dead += OnEntityDead;
            entity.Effects.EffectAdded += OnEffectAdded;
            var body = entity as CharacterBody2D;
            Entity = entity;
            body?.Position = Vector2.Zero;
            CallDeferred(Node.MethodName.AddChild, body);
        }

        public bool HasEntityInit() => Entity != null;

        private void OnEffectAdded(IEffect obj)
        {
            _stateMachine.Fire((Entity!.StatusEffects & StatusEffects.Vanished) != 0 ? Trigger.SetCannotBeSelected : Trigger.SetCanBeSelected);
        }

        public void RemoveBattleEventBus()
        {
            _eventBus = null;
        }

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            _eventBus = battleEventBus;
            _eventBus.Subscribe<PlayerSelectingTargetForAbilityEvent>(OnPlayerSelectingAbilityTarget);
            _eventBus.Subscribe<CancelSelectionEvent>(OnSelectionCancel);
            _eventBus.Subscribe<DamageTakenEvent>(OnDamageTaken);
            _eventBus.Subscribe<EntityHealedEvent>(OnHealed);
        }

        private void OnHealed(EntityHealedEvent obj)
        {
            // TODO: Why some character after evade attack them self?
            if (Entity?.InstanceId != obj.Healed.InstanceId) return;
            int healed = Mathf.RoundToInt(obj.Amount);

            var numbers = FlyNumbers.Initialize().Instantiate<FlyNumbers>();
            numbers.PlayHealNumbers(healed);
            CallDeferred(Node.MethodName.AddChild, numbers);
        }

        private void OnSelectionCancel(CancelSelectionEvent obj)
        {
            if (_stateMachine.State is not State.CandidateForAbility || SelectionId != obj.SelectionId) return;
            _stateMachine.Fire(Trigger.SetCanBeSelected);
        }

        private void OnPlayerSelectingAbilityTarget(PlayerSelectingTargetForAbilityEvent evnt)
        {
            if (Entity == null || _stateMachine.State is State.CannotBeSelected) return;
            _stateMachine.Fire(_candidateForAbility, evnt.SelectionId);
        }

        private void OnDamageTaken(DamageTakenEvent evnt)
        {
            if (Entity?.InstanceId != evnt.Target.InstanceId) return;
            float damage = evnt.Context.Damage;
            var type = evnt.Context.Type;
            bool isCrit = evnt.Context.IsCrit;

            var flyNumbers = FlyNumbers.Initialize().Instantiate<FlyNumbers>();
            flyNumbers.PlayDamageNumbers(Mathf.RoundToInt(damage), type, isCrit);
            CallDeferred(Node.MethodName.AddChild, flyNumbers);
        }

        private void OnEntityDead(IFightable obj)
        {
            _stateMachine.Fire(Trigger.SetCannotBeSelected);
            Entity?.Dead -= OnEntityDead;
            Entity = null;
        }
    }
}
