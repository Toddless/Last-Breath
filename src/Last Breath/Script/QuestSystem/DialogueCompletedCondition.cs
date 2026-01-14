namespace LastBreath.Script.QuestSystem
{
    using Godot;
    using LastBreath.Components;

    [GlobalClass]
    public partial class DialogueCompletedCondition : Condition
    {
        [Export]
        public string DialogueId { get; set; } = string.Empty;
        public override bool IsMet(PlayerProgress progress) => progress.CompletedDialogues.Contains(DialogueId);
    }
}
