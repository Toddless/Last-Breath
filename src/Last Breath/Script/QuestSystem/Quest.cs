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
        public event Action<Quest>? QuestAccepted, QuestFailed, QuestCancelled, QuestCompleted;

        [Export]
        public string Id { get; set; } = string.Empty;
        [Export]
        public LocalizedString? NameKey { get; set; }
        [Export]
        public LocalizedString? DescriptionKey { get; set; }
        [Export]
        public LocalizedString? RewardId { get; set; }
        [Export]
        public Array<QuestCondition> Conditions { get; set; } = [];
        [Export(PropertyHint.Range, "1, 15")]
        public int RequiredConditions { get; set; }
        [Export]
        public bool AllConditionsMustMet { get; set; } = false;
        [Export]
        public bool ConfirmationRequired { get; set; } = true;

        public bool CanAcceptQuest(QuestManager manager) => manager.QuestCanBeAdded(Id);

        public void AcceptQuest() => QuestAccepted?.Invoke(this);
        public void CancelQuest() => QuestCancelled?.Invoke(this);
        public void FailedQuest() => QuestFailed?.Invoke(this);
        public void CompletedQuest() => QuestCompleted?.Invoke(this);

        public void _Validate()
        {
            if (string.IsNullOrEmpty(Id))
            {
                // log
                GD.PushError("Quest ID must be set!");
            }

            if (Conditions?.Count > 0 && RequiredConditions > Conditions.Count)
            {
                // log
                GD.PushWarning("RequiredConditions exceeds total conditions count");
            }

        }
    }
}
