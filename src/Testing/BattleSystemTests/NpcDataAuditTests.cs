namespace LastBreathTest.BattleSystemTests
{
    using Core.Data;
    using Core.Data.DialogueData;
    using Core.Data.NpcData;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Audits the REAL Npc.json: enum names must parse (a typo is a silent drop at runtime),
    /// ids must be unique, and every canTalk species should have a dialogue in the catalog —
    /// that one is a warning (Inconclusive), the dialogue may simply not be written yet.
    /// </summary>
    [TestClass]
    public class NpcDataAuditTests
    {
        [TestMethod]
        public void NpcCatalog_EnumsParseAndIdsAreUnique()
        {
            var npcs = LoadNpcs();
            Assert.IsTrue(npcs.Count > 0, "Npc.json produced no entries");

            var ids = new HashSet<string>();
            foreach (var npc in npcs)
            {
                Assert.IsTrue(ids.Add(npc.Id), $"duplicate npc id '{npc.Id}'");
                EnumParser.ParseEnum<Fractions>(npc.Fraction);
                EnumParser.ParseEnum<EntityType>(npc.EntityType);
                EnumParser.ParseEnum<Core.Ai.AiIntellect>(npc.AiIntellect);
                Assert.IsTrue(npc.Stances.Count > 0, $"'{npc.Id}' has no stances");
                foreach (string stance in npc.Stances)
                    EnumParser.ParseEnum<Stance>(stance);
                foreach (string parameter in npc.BaseParameters.Keys)
                    EnumParser.ParseEnum<EntityParameter>(parameter);
                if (!string.IsNullOrEmpty(npc.Rarity))
                    EnumParser.ParseEnum<Rarity>(npc.Rarity);
                foreach (var reaction in npc.Reactions)
                    EnumParser.ParseEnum<ReactionTrigger>(reaction.Trigger);
            }
        }

        [TestMethod]
        public void TalkingSpecies_HaveADialogueInTheCatalog()
        {
            var talkers = LoadNpcs()
                .Where(npc => npc.Interaction is { CanTalk: true })
                .Select(npc => npc.Id)
                .ToList();
            var dialogues = LoadDialogueIds();

            var silent = talkers.Where(id => !dialogues.Contains(id)).ToList();
            if (silent.Count > 0)
                Assert.Inconclusive($"canTalk species without a dialogue yet (clicks will be silent): {string.Join(", ", silent)}");
        }

        private static List<NpcData> LoadNpcs()
        {
            string json = File.ReadAllText(Path.Combine(FindSharedData(), "Npc", "Npc.json"));
            return JsonConvert.DeserializeObject<NpcsData>(json)!.Npcs;
        }

        private static HashSet<string> LoadDialogueIds()
        {
            var ids = new HashSet<string>();
            foreach (string file in Directory.GetFiles(Path.Combine(FindSharedData(), "Dialogues"), "*.json"))
            {
                var data = JsonConvert.DeserializeObject<DialoguesData>(File.ReadAllText(file));
                foreach (var dialogue in data!.Dialogues)
                    ids.Add(dialogue.NpcId);
            }

            return ids;
        }

        private static string FindSharedData()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "SharedData");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("SharedData was not found above the test host directory");
        }
    }
}
