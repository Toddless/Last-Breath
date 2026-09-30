namespace Core.Narrative.Facts
{
    using System.Linq;
    using Enums;

    /// <summary>Key builders for the facts the code writes itself; free-form keys (dialogue
    /// SetFact actions) live in the data and don't need a builder.</summary>
    /// <remarks>Each head is a constant because the family it names is declared beside the code that
    /// writes and reads it, and a head spelt twice is a family the registry answers for by halves.</remarks>
    public static class FactKeys
    {
        /// <summary>What separates the head of a key from the record it is about.</summary>
        public const char Separator = ':';

        public const string LocationDiscoveredHead = "Location_Discovered";

        public const string KillCountHead = "Kill_Count";

        public const string FactionKillCountHead = "Kill_Count_Faction";

        public const string NpcFinalDeathHead = "Npc_Final_Death";

        public const string BossRespawnDeathsHead = "Boss_Respawn_Deaths";

        public const string NpcTalkedHead = "Npc_Talked";

        public const string DialogueOptionUsedHead = "Dialogue_Used";

        public const string SpeechCheckRewardedHead = "Speech_Exp";

        public const string QuestOfferRollUntilHead = "Quest_Offer_Until";

        public const string QuestOfferRollPassedHead = "Quest_Offer_Passed";

        public const string ItemEquippedHead = "Item_Equipped";

        /// <summary>A family of one: the key IS the head, because "wearing anything at all" has no record
        /// to be about.</summary>
        public const string ItemEquippedAnyHead = "Item_Equipped_Any";

        public static string LocationDiscovered(string locationId) => Key(LocationDiscoveredHead, locationId);

        /// <summary>Player kills of an NPC definition (counted per npc data id, not per instance).</summary>
        public static string KillCount(string npcId) => Key(KillCountHead, npcId);

        public static string FactionKillCount(Fractions faction) => Key(FactionKillCountHead, faction.ToString());

        /// <summary>The NPC definition is finally dead (burned/despawned for good) — bosses gate on it.</summary>
        public static string NpcFinalDeath(string npcId) => Key(NpcFinalDeathHead, npcId);

        /// <summary>Faction deaths accumulated toward the boss's next respawn (BossSpawnPoint counter).</summary>
        public static string BossRespawnDeaths(string bossId) => Key(BossRespawnDeathsHead, bossId);

        public static string NpcTalked(string npcId) => Key(NpcTalkedHead, npcId);

        /// <summary>A once-per-game dialogue option already chosen.</summary>
        public static string DialogueOptionUsed(string npcId, string nodeId, string optionId) =>
            Key(DialogueOptionUsedHead, npcId, nodeId, optionId);

        /// <summary>A speech check that already paid its Influence exp (once per option).</summary>
        public static string SpeechCheckRewarded(string npcId, string nodeId, string optionId) =>
            Key(SpeechCheckRewardedHead, npcId, nodeId, optionId);

        /// <summary>World-clock minute until which the cached quest-offer roll outcome holds.</summary>
        public static string QuestOfferRollUntil(string questId) => Key(QuestOfferRollUntilHead, questId);

        /// <summary>1 = the cached quest-offer roll passed.</summary>
        public static string QuestOfferRollPassed(string questId) => Key(QuestOfferRollPassedHead, questId);

        /// <summary>The player has ever worn gear of this kind. A ring answers under
        /// <see cref="EquipmentPiece.Ring"/> whichever of the two slots took it.</summary>
        public static string ItemEquipped(EquipmentPiece piece) => Key(ItemEquippedHead, piece.ToString());

        /// <summary>How many pieces of gear the player has ever put on — raised beside the slot's own
        /// flag, so "equip something" is one key instead of a clause over every slot.</summary>
        public static string ItemEquippedAny() => Key(ItemEquippedAnyHead);

        /// <summary>One key, written the one way every key of every family is written.</summary>
        private static string Key(string head, params string[] parts) =>
            string.Join(Separator, parts.Prepend(head));
    }
}
