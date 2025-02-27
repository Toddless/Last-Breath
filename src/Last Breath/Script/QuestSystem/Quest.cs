namespace Playground.Resource.Quests
{
    using Godot;
    using Godot.Collections;
    using Playground.Script.QuestSystem;

    [GlobalClass]
    public partial class Quest : Resource
    {
        [Export]
        public string Id { get; set; } = string.Empty;
        [Export]
        public string? NameKey { get; set; }
        [Export]
        public string? DescriptionKey { get; set; }
        [Export]
        public string? RewardId { get; set; }
        [Export]
        public Array<QuestCondition> Conditions { get; set; } = [];
        [Export(PropertyHint.Range, "1, 15")]
        public int RequiredConditions {  get; set; }
        [Export]
        public bool AllConditionsMustMet {  get; set; } = false;

        public void _Validate()
        {
            if (string.IsNullOrEmpty(Id))
            {
                // log
                GD.PushError("Quest ID must be set!");
            }

            if(Conditions?.Count > 0 && RequiredConditions > Conditions.Count)
            {
                // log
                GD.PushWarning("RequiredConditions exceeds total conditions count");
            }

        }
    }
}
