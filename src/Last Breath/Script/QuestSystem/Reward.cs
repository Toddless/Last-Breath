namespace Playground.Script.QuestSystem
{
    using System.Collections.Generic;
    using Playground.Script.Items;
    public class Reward
    {
        public int Exp { get; }
        public int Gold { get; }
        public List<Item> Items { get; }

        public Reward(int exp, int gold, params Item[] items)
        {
            Exp = exp;
            Gold = gold;
            Items = [.. items];
        }
    }
}
