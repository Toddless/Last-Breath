namespace Battle.Source
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Godot;

    /// <summary>
    /// Holds one fighter on the battlefield and reports clicks. Selection state is driven by
    /// <see cref="TargetSelectionController"/> (highlight of valid targets); the spot only reports
    /// clicks and paints the placeholder highlight ring.
    /// </summary>
    public partial class EntitySpot : Node2D
    {
        private const float HighlightRadius = 125f;
        private const float HighlightWidth = 5f;
        private static readonly Color s_validAttackColor = new(0.95f, 0.3f, 0.25f, 0.9f);
        private static readonly Color s_validAbilityColor = new(0.3f, 0.8f, 1f, 0.9f);
        private static readonly Color s_chosenColor = new(0.35f, 0.9f, 0.4f, 1f);

        private enum SelectionState
        {
            Idle,
            Selectable,
            Chosen,
            Unavailable
        }

        private IBattleEventBus? _eventBus;
        private SelectionState _state = SelectionState.Idle;
        private string _selectionId = string.Empty;
        private bool _forAttack;
        [Export] private Area2D? _spotArea;

        public IFightable? Entity { get; private set; }

        public override void _Ready() => _spotArea?.InputEvent += OnInputEvent;

        public override void _Draw()
        {
            if (_state is SelectionState.Idle or SelectionState.Unavailable) return;
            var color = _state == SelectionState.Chosen ? s_chosenColor : _forAttack ? s_validAttackColor : s_validAbilityColor;
            DrawArc(Vector2.Zero, HighlightRadius, 0f, Mathf.Tau, 48, color, HighlightWidth, true);
        }

        /// <summary>Highlight the spot as a valid pick for the current selection.</summary>
        public void MarkSelectable(string selectionId, bool forAttack)
        {
            if (_state == SelectionState.Unavailable || Entity == null) return;
            _selectionId = selectionId;
            _forAttack = forAttack;
            _state = SelectionState.Selectable;
            QueueRedraw();
        }

        /// <summary>Toggle the "chosen" look for a Few-target selection.</summary>
        public void SetChosen(bool chosen)
        {
            if (_state is SelectionState.Unavailable or SelectionState.Idle) return;
            _state = chosen ? SelectionState.Chosen : SelectionState.Selectable;
            QueueRedraw();
        }

        /// <summary>Drop any selection highlight (unless the spot is unavailable).</summary>
        public void ClearSelection()
        {
            if (_state == SelectionState.Unavailable) return;
            _state = SelectionState.Idle;
            _selectionId = string.Empty;
            QueueRedraw();
        }

        public void RemoveEntityFromSpot()
        {
            if (Entity == null) return;
            Entity.Effects.EffectAdded -= OnEffectAdded;
            var node = Entity as Node;
            RemoveChild(node);
        }

        public void SetEntity(IFightable entity)
        {
            entity.Effects.EffectAdded += OnEffectAdded;
            var body = entity as CharacterBody2D;
            Entity = entity;
            body?.Position = Vector2.Zero;
            CallDeferred(Node.MethodName.AddChild, body);
        }

        public bool HasEntityInit() => Entity != null;

        public void RemoveBattleEventBus() => _eventBus = null;

        public void SetBattleEventBus(IBattleEventBus battleEventBus)
        {
            _eventBus = battleEventBus;
            _eventBus.Subscribe<EntityDiedEvent>(OnEntityDead);
        }

        private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            if (Entity == null || _state is SelectionState.Idle or SelectionState.Unavailable) return;

            if (_forAttack) _eventBus?.Publish<AttackTargetSelectedEvent>(new(Entity));
            else _eventBus?.Publish<AbilityTargetPickedEvent>(new(_selectionId, Entity));

            GetViewport().SetInputAsHandled();
        }

        private void OnEffectAdded(IEffect obj)
        {
            bool vanished = (Entity!.StatusEffects & StatusEffects.Vanished) != 0;
            if (vanished) SetUnavailable();
            else if (_state == SelectionState.Unavailable && Entity.IsAlive) ClearToIdle();
        }

        private void OnEntityDead(EntityDiedEvent @event)
        {
            // For now just "cannot be selected". Resurrection abilities may revisit this later.
            if (@event.Entity.InstanceId != Entity?.InstanceId) return;
            SetUnavailable();
        }

        private void SetUnavailable()
        {
            _state = SelectionState.Unavailable;
            _selectionId = string.Empty;
            QueueRedraw();
        }

        private void ClearToIdle()
        {
            _state = SelectionState.Idle;
            _selectionId = string.Empty;
            QueueRedraw();
        }
    }
}
