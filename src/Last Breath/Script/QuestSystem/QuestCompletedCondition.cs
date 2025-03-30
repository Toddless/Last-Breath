namespace Playground.Script.QuestSystem
{
    using Godot;
    using Playground.Components;

    [GlobalClass]
    public partial class QuestCompletedCondition : Condition
    {
        [Export]
        public string QuestId { get; set; } = string.Empty;
        [Export]
        public QuestStatus RequiredStatus {  get; set; } = QuestStatus.Completed;

        public override bool IsMet(PlayerProgress progress) => progress.CompletedQuests.Contains(QuestId);
    }
}
