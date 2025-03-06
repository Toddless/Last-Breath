namespace Playground.Resource
{
    using Godot;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;
    using Playground.Script.QuestSystem;

    public partial class QuestsTable : Table<Quest>
    {
        public static QuestsTable Instance { get; private set; } = new();

        public override void _EnterTree()
        {
            Instance ??= this;
            LoadData();
        }

        protected override void LoadData()
        {
            var data = ResourceLoader.Load<QuestData>(ResourcePath.QuestData);
            foreach (var quest in data.Quests)
            {
                if (Elements.ContainsKey(quest.Id)) continue;

                Elements[quest.Id] = quest;
            }
        }
    }
}
