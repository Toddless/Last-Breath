namespace Playground.Script.QuestSystem
{
    using Godot;
    using Playground.Components;

    [GlobalClass]
    public partial class DialogueCompletedCondition : QuestCondition
    {
        [Export]
        public string DialogueId { get; set; } = string.Empty;
        public override bool IsMet(PlayerProgress progress) => progress.CompletedDialogues.Contains(DialogueId);
    }
}
