namespace LastBreath.Source.QuestSystem
{
    using System;
    using Core.Enums;
    using Godot;
    using LastBreath.Localization;

    [GlobalClass]
    public partial class Quest : Resource
    {
        private QuestStatus _questStatus = QuestStatus.NotAccepted;
        public event Action<string, QuestStatus>? StatusChanged;

        [Export]
        public string Id { get; set; } = string.Empty;
        [Export]
        public LocalizedString? NameKey { get; set; }
        [Export]
        public LocalizedString? DescriptionKey { get; set; }
        [Export]
        public string RewardId { get; set; } = string.Empty;
        [Export]
        public int RequiredConditions { get; set; }
        [Export]
        public bool IsTriggerQuest { get; set; } = true;
        // to witch npcs quest belong
        [Export]
        public string NpcId { get; set; } = string.Empty;
        [Export]
        public string DialogueIdOnComplete { get; set; } = string.Empty;
        [Export]
        public QuestType Type { get; set; }
        [Export]
        public bool IsNpcNeededForQuestCompletion { get; set; } = false;
        [Export]
        public QuestObjective? QuestObjective { get; set; }
        /// <summary>
        /// For tracking and changing on UI
        /// </summary>
        public QuestStatus QuestStatus
        {
            get => _questStatus;
            set
            {
                if (_questStatus == value) return;

                _questStatus = value;
                StatusChanged?.Invoke(Id, _questStatus);
            }
        }

        public override int GetHashCode() => Id?.GetHashCode() ?? 0;

        public override bool Equals(object? obj) => obj is Quest q && Id == q.Id;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StatusChanged = null;
            }
            base.Dispose(disposing);
        }
    }
}
