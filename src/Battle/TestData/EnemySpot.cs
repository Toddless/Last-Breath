namespace Battle.TestData
{
    using Godot;
    using Core.Interfaces.Entity;

    public partial class EnemySpot : Node2D
    {
        private bool _mouseInside, _isDead;
        [Export] private CharacterBody2D? _entity;
        [Export] private Area2D? _spotArea;

        [Signal]
        public delegate void EntityClickedEventHandler(CharacterBody2D entity);

        public override void _Ready()
        {
            _spotArea?.MouseEntered += OnMouseEntered;
            _spotArea?.MouseExited += OnMouseExited;
        }

        public void SetEntity(IEntity entity)
        {
            _isDead = false;
            entity.Dead += OnEntityDead;
            _entity = entity is CharacterBody2D body ? body : null;
            if (_entity == null)
                return;
            _entity.Position = new Vector2(0, 0);
            CallDeferred(Node.MethodName.AddChild, _entity);
        }

        private void OnEntityDead(IFightable obj)
        {
            _isDead = true;
            var entity = _entity as IEntity;
            entity?.Dead -= OnEntityDead;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_isDead) return;
            if (@event is InputEventMouseButton { Pressed : true } eventMouseButton && _mouseInside && _entity != null && eventMouseButton.ButtonIndex == MouseButton.Left)
                EmitSignal(SignalName.EntityClicked, _entity);
        }

        private void OnMouseEntered()
        {
            _mouseInside = true;
            if (_entity is not IEntity entity) return;
            GD.Print($"Entity has health: {entity.CurrentHealth}");
        }

        private void OnMouseExited()
        {
            _mouseInside = false;
        }
    }
}
