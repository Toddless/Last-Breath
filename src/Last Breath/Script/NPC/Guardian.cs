namespace Playground.Script.NPC
{
    using Playground.Script.QuestSystem;
    using System.Collections.Generic;
    using Godot;

    public partial class Guardian : BaseNPC
    {
        private List<Quest>? _sideQuests;
        private List<Quest>? _randomQuests;
        public override void _Ready()
        {
            Collision = GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            Sprite = GetNode<Sprite2D>(nameof(Sprite2D));
            Area = GetNode<Area2D>(nameof(Area2D));

            SetDialogs("Localization/guardianDialogs.json");
        }
    }
}
