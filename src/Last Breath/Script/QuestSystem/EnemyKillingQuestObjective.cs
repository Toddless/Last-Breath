namespace Playground.Script.QuestSystem
{
    using Godot;
    using Playground.Script.Enemy;

    [GlobalClass]
    public partial class EnemyKillingQuestObjective : QuestObjective
    {
        [Export]
        public EnemyType TargetEnemyType { get; set; } = EnemyType.Any;

        public override bool IsEventMatching(object eventData)
        {
            if (eventData is EnemyKilledEventArgs args)
            {
                if (TargetEnemyType == EnemyType.Any)
                    return true;
                if (TargetEnemyType != EnemyType.Any && args.EnemyType == TargetEnemyType)
                    return true;
                if (!string.IsNullOrEmpty(TargetId) && args.EnemyId == TargetId)
                    return true;
            }
            return false;
        }
    }
}
