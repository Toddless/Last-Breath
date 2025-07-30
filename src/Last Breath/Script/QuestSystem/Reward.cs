namespace LastBreath.Script.QuestSystem
{
    using Godot;
    using Godot.Collections;
    using LastBreath.Script.Items;

    [GlobalClass]
    public partial class Reward : Resource
    {
        [Export]
        public string Id { get; set; } = string.Empty;
        [Export]
        public int Exp { get; set; }
        [Export]
        public int Gold { get; set; }
        [Export]
        public Array<Item> Items { get; set; } = [];
    }
}
