using System.Linq;

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
        [Export]
        public bool AllConditionsMustMet { get; set; } = false;
        [Export]
        public int MinimumConditionsRequirement { get; set; } = 0;

        public bool CheckConditions(Player player)
        {
            if (Conditions.Count == 0) return true;
            int cnt = Conditions.Where(item => item.IsMet(player.Progress)).Count();
            return AllConditionsMustMet
                ? cnt == Conditions.Count
                : cnt >= MinimumConditionsRequirement;
        }
    }
}
