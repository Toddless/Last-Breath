namespace Playground.Script.NPC
{
    using System.Linq;
    using System.Text;
    using Godot;
    using Playground.Script.Enums;

    public partial class BaseNPC : CharacterBody2D
    {
        private CollisionShape2D? _collisionShape2D;
        private Sprite2D? _sprite2D;
        private Area2D? _area2D;
        private string _npcId = string.Empty;
        private Fractions _fraction;

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

        public string NpcId => _npcId;

        protected void SetFraction(Fractions fraction) => _fraction = fraction;

        protected void SetNpcId()
        {
            var builder = new StringBuilder();
            builder.AppendLine(this.Name);
            builder.AppendLine(this._fraction.ToString());
            _npcId = builder.ToString();
        }

        public override void _EnterTree()
        {
            if (string.IsNullOrEmpty(_npcId))
                SetNpcId();
            base._EnterTree();
        }

        public bool IsPlayerNearby() => Area?.GetOverlappingBodies().Any(x => x is Player) == true;
    }
}
