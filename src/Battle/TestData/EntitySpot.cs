namespace Battle.TestData
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Entity;

    public partial class EntitySpot : Node2D
    {
        private bool _mouseInside, _isDead;
        private IEntity? _entity;
        [Export] private Area2D? _spotArea;

        [Signal]
        public delegate void EntityClickedEventHandler(CharacterBody2D entity);

        public override void _Ready()
        {
            _spotArea?.MouseEntered += OnMouseEntered;
            _spotArea?.MouseExited += OnMouseExited;
        }

        public void RemoveEntityFromSpot()
        {
            if (_entity == null) return;
            var node = _entity as Node2D;
            CallDeferred(Node.MethodName.RemoveChild, node);
        }

        public void SetEntity(IEntity entity)
        {
            _isDead = false;
            entity.Dead += OnEntityDead;
            entity.DamageTaken += OnDamageTaken;
            var body = entity as CharacterBody2D;
            _entity = entity;
            body?.Position = Vector2.Zero;
            CallDeferred(Node.MethodName.AddChild, body);
        }

        private void OnDamageTaken(float damage, DamageType type, bool isCrit)
        {
            var damageNumber = DamageNumber.Initialize().Instantiate<DamageNumber>();
            damageNumber.Play(Mathf.RoundToInt(damage), type, isCrit);
            CallDeferred(Node.MethodName.AddChild, damageNumber);
        }

        private void OnEntityDead(IFightable obj)
        {
            _isDead = true;
            _entity?.DamageTaken -= OnDamageTaken;
            _entity?.Dead -= OnEntityDead;
            _entity = null;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_isDead) return;
            if (_entity is not Node2D node) return;
            if (@event is InputEventMouseButton { Pressed : true } eventMouseButton && _mouseInside && _entity != null && eventMouseButton.ButtonIndex == MouseButton.Left)
            {
                EmitSignal(SignalName.EntityClicked, node);
                GetViewport().SetInputAsHandled();
            }
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
