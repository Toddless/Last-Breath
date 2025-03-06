namespace Playground.Localization
{
    using Godot;
    using Godot.Collections;
    using Playground.Script.QuestSystem;

    [GlobalClass]
    public partial class DialogueOption : Resource
    {
        [Export]
        public LocalizedString? OptionName { set; get; }

        [Export]
        public string TargetNode { get; set; } = string.Empty;

        [Export]
        public int RelationEffect { get; set; } = 0;
        [Export]
        public bool UsePlayerSource { get; set; } = true;
        [Export]
        public Array<Condition> Conditions { get; set; } = [];

        public bool AllConditionsMet(Player player)
        {
            if (Conditions.Count == 0) return true;
            int cnt = 0;

            foreach (var item in Conditions)
            {
                if (item.IsMet(player.Progress))
                    cnt++;
            }

            return cnt == Conditions.Count;
        }

        public bool MinConditionsMet(Player player, int amount)
        {
            if (Conditions.Count == 0) return true;
            int cnt = 0;
            foreach (var item in Conditions)
            {
                if(item.IsMet(player.Progress))
                    cnt++;
            }

            return cnt >= amount;
        }
    }
}
