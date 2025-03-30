namespace Playground.Script.QuestSystem
{
    using Godot;

    [GlobalClass]
    public partial class QuestObjective : Resource
    {
        [Export]
        public ObjectiveType QuestObjectiveType { get; set; }
        [Export]
        public string TargetId { get; set; } = string.Empty;
        [Export]
        public int RequiredAmount { get; set; } = 1;

        public int CurrentAmount { get; set; }

        public bool IsCompleted => CurrentAmount >= RequiredAmount;

        public virtual bool IsEventMatching(object eventData)
        {
            if(eventData is string obj)
                return TargetId == obj;
            return false;
        }
    }
}
