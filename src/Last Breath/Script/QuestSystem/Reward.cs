namespace Playground.Script.QuestSystem
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Items;

    [GlobalClass]
    public partial class Reward : Resource
    {
        [Export]
        public int Exp { get; set; }
        [Export]
        public int Gold { get; set; }
        public List<Item>? Items { get; set; }

        public Reward(int exp, int gold, params Item[] items)
        {
            Exp = exp;
            Gold = gold;
            Items = [.. items];
        }
        public Reward()
        {

        }
    }
}
