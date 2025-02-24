namespace Playground.Script.QuestSystem
{
    using System.Collections.Generic;
    using Playground.Script.Items;
    public class Reward(int exp, int gold, params Item[] items)
    {
        public int Exp { get; } = exp;
        public int Gold { get; } = gold;
        public List<Item> Items { get; } = [.. items];
    }
}
