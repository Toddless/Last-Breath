namespace LastBreathTest.WorldTesting
{
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.NpcData;
    using Core.Enums;
    using Core.Services;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The catalog behind "how many": per-EntityType ladders of slot chances plus the rarity multiplier
    /// that lifts them. These tests hold the SHAPE of the answer — that it falls slot by slot, that
    /// rarity moves it, that the two tables are two tables — where <see cref="NpcProviderTests"/> holds
    /// what the dice built on top of it actually produce.
    /// </summary>
    [TestClass]
    public class NpcSpawnRollsTests
    {
        private const string CatalogFile = "NpcSpawnRolls.json";

        /// <summary>The type whose ladder every shape test below reads: it names all three fields with
        /// numbers that are neither 0 nor 1, so a result of 0 or 1 is a lost read and not a rounding.</summary>
        private const EntityType LadderType = EntityType.Elit;

        /// <summary>Slots a shape claim is read over. Four is the deepest ceiling the calibration point
        /// has, and every ladder answers past its own ceiling anyway — the arithmetic does not stop.</summary>
        private const int SlotsRead = 4;

        /// <summary>The reason the whole mechanic exists: each further slot is a longer shot than the one
        /// before it. Drop the decay and the tail goes flat, and this is the line that says so — for every
        /// ladder the file ships, because a flat tail on one type is as broken as a flat tail on all of
        /// them and only the type under test would have noticed. Read at epic, where the rarity multiplier
        /// is 1: a higher one clamps the top of a ladder and would compare two certainties.</summary>
        [TestMethod]
        public void EveryFurtherSlotIsLessLikelyThanTheOneBeforeIt()
        {
            var rolls = ShippedRolls();

            foreach ((string name, var chance) in EveryShippedLadder(rolls))
                for (int slot = 1; slot < SlotsRead; slot++)
                    Assert.IsTrue(chance(slot) < chance(slot - 1),
                        $"{name}: slot {slot} is {chance(slot):P2}, no less likely than slot {slot - 1} at {chance(slot - 1):P2}");
        }

        /// <summary>Every ladder the file names is a real ladder: three fields, all read, none of them
        /// quietly standing at the certainty the parser would have to invent for a missing one. Goes red
        /// on a renamed field — that is the whole point of it.</summary>
        [TestMethod]
        public void EveryShippedLadderNamesAllThreeOfItsFields()
        {
            var rolls = ShippedRolls();

            foreach ((string name, var chance) in EveryShippedLadder(rolls))
            {
                Assert.IsTrue(chance(0) is > 0f and <= 1f, $"{name}: the first slot chance reads {chance(0)}");
                Assert.IsTrue(chance(1) is > 0f and < 1f, $"{name}: the second slot chance reads {chance(1)} — a certainty is what a lost read looks like");
                Assert.AreNotEqual(chance(1), chance(2), $"{name}: the decay reads as 1, which is what a lost read looks like");
            }
        }

        /// <summary>A ladder that names two of its three fields is refused rather than completed by the
        /// parser. There is no honest default: the only value the arithmetic could take is 1, and 1 hands
        /// that type back the always-maximum spawn the catalog exists to end. A misspelled field name is
        /// the same event and reaches the same refusal.</summary>
        [TestMethod]
        public void ALadderMissingAField_IsRefused_NotSilentlyTreatedAsCertain()
        {
            var rolls = new NpcSpawnRollsProvider();

            Assert.ThrowsException<FormatException>(() => rolls.Apply(
                DataCatalog.NpcSpawnRolls,
                new GameDataFile(CatalogFile, """{"modifiers":{"Elit":{"firstSlotChance":0.9,"nextSlotChance":0.4}}}""")));
            Assert.ThrowsException<FormatException>(() => rolls.Apply(
                DataCatalog.NpcSpawnRolls,
                new GameDataFile(CatalogFile, """{"abilities":{"Elit":{"firstSlotChanse":0.95,"nextSlotChance":0.55,"decay":0.75}}}""")));
        }

        /// <summary>A refused file leaves nothing of itself behind. Half a balance file is the one state
        /// this participant must never reach: the types that did not make it answer "every slot is
        /// certain" and read exactly like an authored exemption.</summary>
        [TestMethod]
        public void ARefusedFile_LeavesNoHalfOfItselfLoaded()
        {
            var rolls = new NpcSpawnRollsProvider();

            Assert.ThrowsException<FormatException>(() => rolls.Apply(
                DataCatalog.NpcSpawnRolls,
                new GameDataFile(CatalogFile, """
                    {"modifiers":{
                      "Regular":{"firstSlotChance":0.25,"nextSlotChance":0.15,"decay":0.7},
                      "Elit":{"firstSlotChance":0.9,"nextSlotChance":0.4}}}
                    """)));

            Assert.AreEqual(1f, rolls.ModifierSlotChance(EntityType.Regular, Rarity.Epic, 0),
                "the ladder that parsed before the broken one was kept, so the file half-applied");
        }

        /// <summary>Rarity is a multiplier on the whole ladder, so it moves the first slot as surely as
        /// the last one. Ignore it and both of these collapse into one number.</summary>
        [TestMethod]
        public void RarityMovesTheWholeLadder()
        {
            var rolls = ShippedRolls();
            var poor = Ladders(rolls, Rarity.Uncommon).ToList();
            var rich = Ladders(rolls, Rarity.Epic).ToList();

            for (int table = 0; table < poor.Count; table++)
            {
                (string name, var uncommon) = poor[table];
                (_, var epic) = rich[table];

                Assert.IsTrue(uncommon(0) < epic(0), $"{name}: an uncommon spawn is offered the first slot as readily as an epic one");
                Assert.IsTrue(uncommon(1) < epic(1), $"{name}: rarity does not reach past the first slot");
            }
        }

        /// <summary>Modifiers and abilities are two questions with two tables. An elite is measurably
        /// freer with casts than with modifiers, which is also what tells a swapped read apart.</summary>
        [TestMethod]
        public void AbilitiesAndModifiersReadDifferentTables()
        {
            var rolls = ShippedRolls();

            Assert.AreNotEqual(
                rolls.ModifierSlotChance(LadderType, Rarity.Epic, 1),
                rolls.AbilitySlotChance(LadderType, Rarity.Epic, 1),
                "the two ladders answer the same number, so nothing would notice one reading the other");
        }

        /// <summary>A rarity multiplier above one runs the top of a ladder past certainty; certainty is
        /// where it stops, because a chance above 1 read as a probability is nonsense.</summary>
        [TestMethod]
        public void AChanceLiftedPastCertainty_IsCertaintyAndNotMore()
        {
            var rolls = ShippedRolls();

            Assert.IsTrue(rolls.ModifierSlotChance(LadderType, Rarity.Epic, 0) > 0.85f,
                "precondition: the elite's first modifier slot sits close enough to 1 for a rarity above epic to overshoot it");
            Assert.AreEqual(1f, rolls.ModifierSlotChance(LadderType, Rarity.Legendary, 0),
                "a multiplier that runs a chance past certainty must land ON certainty, not carry a probability above 1 downstream");
        }

        /// <summary>The failure mode worth choosing on purpose: a type nobody wrote a ladder for keeps the
        /// behaviour that predates this catalog — every slot the ceiling offers is taken. A data folder
        /// someone forgot to ship then makes last month's NPCs and not NPCs stripped of everything.</summary>
        [TestMethod]
        public void ATypeTheCatalogDoesNotName_FillsEverySlotAsItAlwaysDid()
        {
            var rolls = new NpcSpawnRollsProvider();

            Assert.AreEqual(1f, rolls.ModifierSlotChance(EntityType.Archon, Rarity.Common, 5));
            Assert.AreEqual(1f, rolls.AbilitySlotChance(EntityType.Regular, Rarity.Mythic, 0));
        }

        [TestMethod]
        public void AMisspelledTypeOrRarity_IsRefused_NotSilentlyIgnored()
        {
            var rolls = new NpcSpawnRollsProvider();

            Assert.ThrowsException<FormatException>(() => rolls.Apply(
                DataCatalog.NpcSpawnRolls,
                new GameDataFile(CatalogFile, """{"modifiers":{"Elite":{"firstSlotChance":0.5,"nextSlotChance":0.2,"decay":0.8}}}""")));
            Assert.ThrowsException<FormatException>(() => rolls.Apply(
                DataCatalog.NpcSpawnRolls,
                new GameDataFile(CatalogFile, """{"rarityMultipliers":{"Legendery":1.2}}""")));
        }

        /// <summary>Every rollable type has to be in the file. A type left out silently reverts to taking
        /// every slot, which is exactly the "always maximum" this catalog was written to end — a quiet
        /// hole in a balance file is worse than a loud one.</summary>
        [TestMethod]
        public void TheShippedFileNamesEveryTypeThatRollsAndEveryRarity()
        {
            var shipped = ShippedJson();

            foreach (var type in Enum.GetValues<EntityType>())
                Assert.IsNotNull(shipped["modifiers"]?[type.ToString()], $"no modifier ladder shipped for '{type}'");

            // Abilities are the exception the code states: Boss and Archon take the whole stance pool
            // (NpcTypeDefaults.AllAbilities), an authored promise with no slots to roll against.
            foreach (var type in Enum.GetValues<EntityType>())
            {
                bool authored = NpcTypeDefaults.DefaultAbilityCount(type) == NpcTypeDefaults.AllAbilities;
                var ladder = shipped["abilities"]?[type.ToString()];
                if (authored) Assert.IsNull(ladder, $"'{type}' takes the whole pool, so an ability ladder for it is dead weight");
                else Assert.IsNotNull(ladder, $"no ability ladder shipped for '{type}'");
            }

            foreach (var rarity in Enum.GetValues<Rarity>())
                Assert.IsNotNull(shipped["rarityMultipliers"]?[rarity.ToString()], $"no rarity multiplier shipped for '{rarity}'");
        }

        private static NpcSpawnRollsProvider ShippedRolls()
        {
            var provider = new NpcSpawnRollsProvider();
            provider.Apply(DataCatalog.NpcSpawnRolls, new GameDataFile(
                CatalogFile,
                File.ReadAllText(Path.Combine(SharedData.Catalog(DataCatalog.NpcSpawnRolls), CatalogFile))));
            return provider;
        }

        /// <summary>Both ladders of one type at one rarity, so a shape claim is made about each of them
        /// and neither can quietly stop holding it.</summary>
        private static IEnumerable<(string Name, Func<int, float> Chance)> Ladders(
            INpcSpawnRollsProvider rolls, Rarity rarity = Rarity.Epic) =>
        [
            ("modifiers", slot => rolls.ModifierSlotChance(LadderType, rarity, slot)),
            ("abilities", slot => rolls.AbilitySlotChance(LadderType, rarity, slot)),
        ];

        /// <summary>Every ladder the shipped file actually names, read at epic where the rarity multiplier
        /// is 1 — the section list comes from the FILE, so a ladder added later is judged by these shape
        /// tests without anyone remembering to add it here.</summary>
        private static IEnumerable<(string Name, Func<int, float> Chance)> EveryShippedLadder(INpcSpawnRollsProvider rolls)
        {
            var shipped = ShippedJson();
            List<(string, Func<int, float>)> ladders = [];

            foreach (var named in shipped["modifiers"]?.Children<JProperty>() ?? [])
            {
                var type = Enum.Parse<EntityType>(named.Name);
                ladders.Add(($"modifiers/{named.Name}", slot => rolls.ModifierSlotChance(type, Rarity.Epic, slot)));
            }

            foreach (var named in shipped["abilities"]?.Children<JProperty>() ?? [])
            {
                var type = Enum.Parse<EntityType>(named.Name);
                ladders.Add(($"abilities/{named.Name}", slot => rolls.AbilitySlotChance(type, Rarity.Epic, slot)));
            }

            Assert.IsTrue(ladders.Count > 1, "the shipped file names no ladders, so these shape tests are checking nothing");
            return ladders;
        }

        private static JObject ShippedJson() =>
            JObject.Parse(File.ReadAllText(Path.Combine(SharedData.Catalog(DataCatalog.NpcSpawnRolls), CatalogFile)));
    }
}
