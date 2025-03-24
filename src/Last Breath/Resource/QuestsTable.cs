namespace Playground.Resource
{
    using System.Collections.Generic;
    using System.Linq;
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

        public static void AddNewQuest(Quest quest) => Elements.TryAdd(quest.Id, quest);

        public static List<string> GetAllQuests(string npcId) => Elements.Where(x => x.Value.NpcId == npcId).Select(x => x.Value.Id).ToList();

        public static Quest? GetQuest(string id) => TryGetElement(id, out Quest? quest) ? quest : null;

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
