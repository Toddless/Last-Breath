namespace Core.Save.Participants
{
    using System.Linq;
    using Crafting;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;

    /// <summary>Only scroll-learned ids are stored; mastery-derived knowledge re-derives from the
    /// mastery level (its own section) and the recipe data every query.</summary>
    public class RecipeKnowledgeSaveParticipant(IRecipeKnowledge knowledge) : ISaveParticipant
    {
        public string SectionId => "recipeKnowledge";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Mastery;

        public JToken Capture() => JToken.FromObject(new RecipeKnowledgeSaveData { Learned = knowledge.LearnedIds.ToArray() });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<RecipeKnowledgeSaveData>();
            if (saved == null) return;
            knowledge.RestoreState(saved.Learned);
        }
    }
}
