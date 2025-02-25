namespace Playground.Resource.Quests
{
    using Playground.Components;

    public class NpcInteractionCondition : QuestCondition
    {
        public string NpcId {  get; set; } = string.Empty;

        public override bool IsMet(PlayerProgress progress) => progress.InteractedNpcs.Contains(NpcId);
    }
}
