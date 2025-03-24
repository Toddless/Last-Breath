namespace Playground.Resource.Quests
{
    using System;
    using Godot;
    using Godot.Collections;
    using Playground.Localization;
    using Playground.Script.Enums;
    using Playground.Script.QuestSystem;

    [GlobalClass]
    public partial class Quest : Resource
    {
        /// <summary>
        /// For ques accepting, cancelling etc. use respective methods
        /// </summary>
        public event Action<Quest>? QuestAccepted, QuestFailed, QuestCancelled, QuestCompleted;

        [Export]
        public string Id { get; set; } = string.Empty;
        [Export]
        public LocalizedString? NameKey { get; set; }
        [Export]
        public LocalizedString? DescriptionKey { get; set; }
        [Export]
        public string RewardId { get; set; } = string.Empty;
        [Export]
        public Array<Condition> Conditions { get; set; } = [];
        [Export(PropertyHint.Range, "0, 15")]
        public int RequiredConditions { get; set; }
        // for accepting quest due dialogue
        [Export]
        public bool ConfirmationRequired { get; set; } = true;
        // to witch npcs quest belong
        [Export]
        public string NpcId { get; set; } = string.Empty;
        [Export]
        public QuestType Type { get; set; }
        [Export]
        public QuestObjective? QuestObjective { get; set; }

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
