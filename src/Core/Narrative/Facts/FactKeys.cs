namespace Core.Narrative.Facts
{
    using Enums;

    /// <summary>Key builders for the facts the code writes itself; free-form keys (dialogue
    /// SetFact actions) live in the data and don't need a builder.</summary>
    public static class FactKeys
    {
        public static string LocationDiscovered(string locationId) => $"Location_Discovered:{locationId}";

        /// <summary>Player kills of an NPC definition (counted per npc data id, not per instance).</summary>
        public static string KillCount(string npcId) => $"Kill_Count:{npcId}";

        public static string FactionKillCount(Fractions faction) => $"Kill_Count_Faction:{faction}";

        public static string NpcTalked(string npcId) => $"Npc_Talked:{npcId}";

        /// <summary>A once-per-game dialogue option already chosen.</summary>
        public static string DialogueOptionUsed(string npcId, string nodeId, string optionId) => $"Dialogue_Used:{npcId}:{nodeId}:{optionId}";

        /// <summary>A speech check that already paid its Influence exp (once per option).</summary>
        public static string SpeechCheckRewarded(string npcId, string nodeId, string optionId) => $"Speech_Exp:{npcId}:{nodeId}:{optionId}";

        /// <summary>World-clock minute until which the cached quest-offer roll outcome holds.</summary>
        public static string QuestOfferRollUntil(string questId) => $"Quest_Offer_Until:{questId}";

        /// <summary>1 = the cached quest-offer roll passed.</summary>
        public static string QuestOfferRollPassed(string questId) => $"Quest_Offer_Passed:{questId}";
    }
}
