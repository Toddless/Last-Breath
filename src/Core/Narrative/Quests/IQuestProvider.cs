namespace Core.Narrative.Quests
{
    using System.Collections.Generic;

    public interface IQuestProvider
    {
        IReadOnlyCollection<QuestDefinition> All { get; }

        QuestDefinition? Get(string questId);
    }
}
