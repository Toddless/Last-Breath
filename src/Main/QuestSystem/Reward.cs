namespace LastBreath.QuestSystem
{
    using Godot;
    using Godot.Collections;
    using Item = Items.Item;

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
