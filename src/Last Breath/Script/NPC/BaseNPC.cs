namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Localization;

    public partial class BaseNPC : CharacterBody2D
    {
        private CollisionShape2D? _collisionShape2D;
        private Sprite2D? _sprite2D;
        private Area2D? _area2D;
        private Dictionary<string, DialogueNode> _dialogs = [];

        public bool FirstTimeMeetPlayer = true;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;

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

        protected void SetDialogs(string path)
        {
            var dialogueData = ResourceLoader.Load<DialogueData>(path);
            if (dialogueData.Dialogs == null) return;
            foreach (var item in dialogueData.Dialogs)
            {
                _dialogs.Add(item.Key, item.Value);
            }
        }

        public bool IsPlayerNearby() => Area?.GetOverlappingBodies().Any(x => x is Player) == true;
    }
}
