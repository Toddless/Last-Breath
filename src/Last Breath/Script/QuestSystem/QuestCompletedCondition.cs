namespace Playground.Script.QuestSystem
{
    using Godot;
    using Playground.Components;

    public partial class QuestCompletedCondition : Condition
    {
        [Export]
        public string QuestId { get; set; } = string.Empty;

        public override bool IsMet(PlayerProgress progress) => progress.CompletedQuests.Contains(QuestId);
    }
}
