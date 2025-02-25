namespace Playground.Script.NPC
{
    using Godot;

    public partial class Digry : BaseNPC, ISpeaking
    {
        public override void _Ready()
        {
            Collision = GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            Sprite = GetNode<Sprite2D>(nameof(Sprite2D));
            Area = GetNode<Area2D>(nameof(Area2D));
            SetDialogs("Resource/Dialogues/digryDialogues.json");
        }
    }
}
