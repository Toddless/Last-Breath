namespace LastBreath.Resource
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using LastBreath.Script.Helpers;
    using LastBreath.Resource.Quests;
    using LastBreath.Script.QuestSystem;

    public partial class QuestsTable : Table<Quest>
    {
        private static QuestsTable? s_instance;

        public static QuestsTable Instance => s_instance ??= new();

        protected override void LoadData()
        {
            var data = ResourceLoader.Load<QuestData>(ResourcePath.QuestData);
            foreach (var quest in data.Quests)
            {
                if (Elements.ContainsKey(quest.Id)) continue;

                Elements[quest.Id] = quest;
            }
        }

        public override void _EnterTree()
        {
            s_instance ??= this;
            LoadData();
        }

        public override void AddNewElement(Quest quest) => Elements.TryAdd(quest.Id, quest);

        public override List<string> GetAllElements(string id) => Elements.Where(x => x.Value.NpcId == id).Select(x => x.Value.Id).ToList();
    }
}
