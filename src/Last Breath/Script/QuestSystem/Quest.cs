namespace Playground.Resource.Quests
{
    using System;
    using Godot;
    using Godot.Collections;
    using Playground.Localization;
    using Playground.Script.QuestSystem;

    [GlobalClass]
    public partial class Quest : Resource
    {
        /// <summary>
        /// For ques accepting, cancelling etc. use respective methods
        /// </summary>
        public event Action<Quest>? QuestAccepted, QuestFailed, QuestCancelled, QuestCompleted;

        [Export]
        public string Id { get; private set; } = string.Empty;
        [Export]
        public LocalizedString? NameKey { get; set; }
        [Export]
        public LocalizedString? DescriptionKey { get; set; }
        [Export]
        public string RewardId { get; set; } = string.Empty;
        [Export]
        public Array<QuestCondition> Conditions { get; set; } = [];
        [Export(PropertyHint.Range, "0, 15")]
        public int RequiredConditions { get; set; }
        [Export]
        public bool AllConditionsMustMet { get; set; } = false;
        [Export]
        public bool ConfirmationRequired { get; set; } = true;
        [Export]
        public string NpcId { get; set; } = string.Empty;

        public bool QuestCanBeAccepted(QuestManager manager) => manager.QuestCanBeAccepted(this);
        public void AcceptQuest() => QuestAccepted?.Invoke(this);
        public void CancelQuest() => QuestCancelled?.Invoke(this);
        public void FailedQuest() => QuestFailed?.Invoke(this);
        public void CompletedQuest() => QuestCompleted?.Invoke(this);
        public override int GetHashCode() => Id?.GetHashCode() ?? 0;
        public override bool Equals(object? obj) => obj is Quest q && Id == q.Id;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                QuestAccepted = null;
                QuestFailed = null;
                QuestCancelled = null;
                QuestCompleted = null;
            }
            base.Dispose(disposing);
        }
    }
}
