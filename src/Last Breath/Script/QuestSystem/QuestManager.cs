namespace Playground.Script.QuestSystem
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;

    public class QuestManager
    {
        private List<Quest> _allQuests = [];
        private ObservableCollection<Quest> _quests = [];
        public ObservableCollection<Quest> QuestsId => _quests;

        public bool AddNewQuest(string questId)
        {
            if (_quests.Any(x => x.Id == questId))
            {
                return false;
            }
            return true;
        }

        private bool QuestConditionsFulfilled(string questId)
        {
            return true;
        }




        private void LoadAllQuests()
        {
            var data = JsonSerializer.Deserialize<QuestData>(File.ReadAllText(ResourcePath.QuestData));
            foreach (var quest in data.Quests)
            {

            }
        }
    }
}
