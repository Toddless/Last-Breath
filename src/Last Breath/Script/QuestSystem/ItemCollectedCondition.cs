namespace Playground.Script.QuestSystem
{
    using Godot;
    using Playground.Components;

    [GlobalClass]
    public partial class ItemCollectedCondition : Condition
    {
        [Export]
        public string ItemId { get; set; } = string.Empty;

        [Export]
        public int Amount { get; set; } = 1;

        public override bool IsMet(PlayerProgress progress) => progress.QuestItems[ItemId] == Amount;
    }
}
