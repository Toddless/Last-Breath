namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Helpers;

    public partial class Digry : BaseNPC
    {
        private Dictionary<string, LocalizedText> _dialogs = [];
        public override void _Ready()
        {
            Collision = GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            Sprite = GetNode<Sprite2D>(nameof(Sprite2D));
            Area = GetNode<Area2D>(nameof(Area2D));
        }
    }
}
