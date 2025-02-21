namespace Playground.Script.NPC
{
    using System.Linq;
    using Godot;
    public partial class BaseNPC : CharacterBody2D
    {
        private CollisionShape2D? _collisionShape2D;
        private Sprite2D? _sprite2D;
        private Area2D? _area2D;

        protected Area2D? Area
        {
            get => _area2D;
            set => _area2D = value;
        }

        protected Sprite2D? Sprite
        {
            get => _sprite2D;
            set => _sprite2D = value;
        }

        protected CollisionShape2D? Collision
        {
            get => _collisionShape2D;
            set => _collisionShape2D = value;
        }

        public bool IsPlayerNearby() => Area?.GetOverlappingBodies().Any(x => x is Player) == true;
    }
}
