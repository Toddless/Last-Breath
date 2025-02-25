namespace Playground.Resource.Quests
{
    using Playground.Components;

    public class DialogueCompletedCondition : QuestCondition
    {
        public string DialogueId { get; set; } = string.Empty;
        public override bool IsMet(PlayerProgress progress) => progress.CompletedDialogues.Contains(DialogueId);
    }
}
