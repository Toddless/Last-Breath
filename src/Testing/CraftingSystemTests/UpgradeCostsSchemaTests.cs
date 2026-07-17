namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;

    /// <summary>
    /// The rarity dimension of UpgradeCosts (owner decisions 2026-07-16): amounts and ids may vary
    /// per item rarity via byRarity, defaults cover the rest, and a broken line drops WHOLE with a
    /// report — never a half-parsed cost. The shipped SharedData file is validated against the
    /// design reference: runes 1..4 by rarity, recraft dust + a category/rarity main resource.
    /// </summary>
    [TestClass]
    public class UpgradeCostsSchemaTests
    {
        private static readonly DataParser s_parser = new(new LootGeneration.Internal.ItemGameDataFactory());

        [TestMethod]
        public void ByRarityAmounts_ResolvePerRarity_SharedIdFromDefaults()
        {
            var costs = s_parser.ParseUpgradeCosts("""
            {
                "upgrade": [
                    {
                        "category": "Weapon",
                        "requirements": [
                            {
                                "type": "Resource",
                                "id": "Rune",
                                "byRarity": {
                                    "Common": { "amount": 1 },
                                    "Uncommon": { "amount": 1 },
                                    "Rare": { "amount": 2 },
                                    "Epic": { "amount": 3 },
                                    "Legendary": { "amount": 4 }
                                }
                            }
                        ]
                    }
                ]
            }
            """);

            var line = costs[CraftingMode.Upgrade][EquipmentCategory.Weapon].Single();
            foreach ((var rarity, int expected) in (ReadOnlySpan<(Rarity, int)>)
                     [(Rarity.Common, 1), (Rarity.Uncommon, 1), (Rarity.Rare, 2), (Rarity.Epic, 3), (Rarity.Legendary, 4)])
            {
                var resolved = line.Resolve(rarity);
                Assert.IsNotNull(resolved, $"{rarity} must resolve");
                Assert.AreEqual("Rune", resolved.Id);
                Assert.AreEqual(expected, resolved.Amount, $"{rarity}");
            }
        }

        [TestMethod]
        public void ByRarityIds_ResolvePerRarity_SharedAmountFromDefaults()
        {
            var costs = s_parser.ParseUpgradeCosts("""
            {
                "recraft": [
                    {
                        "category": "Jewellery",
                        "requirements": [
                            {
                                "type": "Resource",
                                "amount": 1,
                                "byRarity": {
                                    "Epic": { "id": "Topaz" },
                                    "Legendary": { "id": "Diamond" }
                                }
                            }
                        ]
                    }
                ]
            }
            """);

            var line = costs[CraftingMode.Recraft][EquipmentCategory.Jewellery].Single();
            var epic = line.Resolve(Rarity.Epic);
            Assert.IsNotNull(epic);
            Assert.AreEqual("Topaz", epic.Id);
            Assert.AreEqual(1, epic.Amount);
            Assert.AreEqual("Diamond", line.Resolve(Rarity.Legendary)!.Id);
            // No default id and no override for this rarity: the line defines nothing there.
            Assert.IsNull(line.Resolve(Rarity.Common));
        }

        [TestMethod]
        public void PlainRequirementWithoutByRarity_ResolvesTheSameForEveryRarity()
        {
            var costs = s_parser.ParseUpgradeCosts("""
            {
                "recraft": [
                    {
                        "category": "Armor",
                        "requirements": [
                            { "type": "Resource", "id": "Dust", "amount": 2 }
                        ]
                    }
                ]
            }
            """);

            var line = costs[CraftingMode.Recraft][EquipmentCategory.Armor].Single();
            foreach (var rarity in Enum.GetValues<Rarity>())
            {
                var resolved = line.Resolve(rarity);
                Assert.IsNotNull(resolved, $"{rarity}");
                Assert.AreEqual("Dust", resolved.Id);
                Assert.AreEqual(2, resolved.Amount);
            }
        }

        [TestMethod]
        public void RarityMissingFromByRarity_FallsBackToCompleteDefaults()
        {
            var costs = s_parser.ParseUpgradeCosts("""
            {
                "upgrade": [
                    {
                        "category": "Weapon",
                        "requirements": [
                            {
                                "type": "Resource",
                                "id": "Rune",
                                "amount": 1,
                                "byRarity": { "Legendary": { "amount": 4 } }
                            }
                        ]
                    }
                ]
            }
            """);

            var line = costs[CraftingMode.Upgrade][EquipmentCategory.Weapon].Single();
            Assert.AreEqual(4, line.Resolve(Rarity.Legendary)!.Amount);
            Assert.AreEqual(1, line.Resolve(Rarity.Rare)!.Amount); // defaults cover unauthored rarities
        }

        [TestMethod]
        public void BrokenLines_AreDroppedWhole_HealthySiblingsSurvive()
        {
            var costs = s_parser.ParseUpgradeCosts("""
            {
                "upgrade": [
                    {
                        "category": "Weapon",
                        "requirements": [
                            { "type": "Resource", "id": "Rune", "byRarity": { "Shiny": { "amount": 1 } } },
                            { "type": "NotARequirementType", "id": "X", "amount": 1 },
                            { "type": "Resource", "id": "Rune", "byRarity": { "Rare": { "amount": 0 } } },
                            { "type": "Resource", "byRarity": { "Rare": { "amount": 2 } } },
                            { "type": "Resource", "id": "Healthy", "amount": 1 }
                        ]
                    }
                ]
            }
            """);

            // Bad rarity key, bad type, non-positive override amount, override without any id — all
            // four drop whole; the healthy line survives alone... plus nothing half-parsed.
            var lines = costs[CraftingMode.Upgrade][EquipmentCategory.Weapon];
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("Healthy", lines[0].Resolve(Rarity.Common)!.Id);
        }

        [TestMethod]
        public void LineWithNoDefaultsAndNoOverrides_IsDropped()
        {
            var costs = s_parser.ParseUpgradeCosts("""
            {
                "ascend": [
                    {
                        "category": "Weapon",
                        "requirements": [ { "type": "Resource" } ]
                    }
                ]
            }
            """);

            Assert.AreEqual(0, costs[CraftingMode.Ascend][EquipmentCategory.Weapon].Count);
        }

        // ---------------------------------------------------------------- shipped data

        [TestMethod]
        public void SharedData_UpgradeRunes_FollowTheDesignReferencePerRarity()
        {
            var costs = LoadSharedCosts();

            // Reference: 1 rune for Uncommon up to 4 for Legendary; Common pays like Uncommon,
            // Unique/Mythic like Legendary.
            foreach (var category in Enum.GetValues<EquipmentCategory>())
            {
                foreach ((var rarity, int expected) in (ReadOnlySpan<(Rarity, int)>)
                         [(Rarity.Common, 1), (Rarity.Uncommon, 1), (Rarity.Rare, 2), (Rarity.Epic, 3), (Rarity.Legendary, 4), (Rarity.Unique, 4), (Rarity.Mythic, 4)])
                {
                    var rune = Resolve(costs[CraftingMode.Upgrade][category], rarity).Single();
                    Assert.IsTrue(rune.Id.Contains("Rune"), $"{category}/{rarity}: expected a rune, got {rune.Id}");
                    Assert.AreEqual(expected, rune.Amount, $"{category}/{rarity}");
                }
            }
        }

        [TestMethod]
        public void SharedData_Recraft_TakesDustPlusMainResourceByCategoryAndRarity()
        {
            var costs = LoadSharedCosts();

            // The design examples verbatim: a Legendary weapon/armor asks for Adamantite ore,
            // an Epic jewellery piece asks for Topaz.
            AssertRecraft(costs, EquipmentCategory.Weapon, Rarity.Legendary, "Upgrade_Resource_Weapon_Dust", "Crafting_Resource_Adamantite_Ore");
            AssertRecraft(costs, EquipmentCategory.Armor, Rarity.Legendary, "Upgrade_Resource_Armor_Dust", "Crafting_Resource_Adamantite_Ore");
            AssertRecraft(costs, EquipmentCategory.Jewellery, Rarity.Epic, "Upgrade_Resource_Jewellery_Dust", "Crafting_Resource_Topaz_Gem");
            AssertRecraft(costs, EquipmentCategory.Weapon, Rarity.Common, "Upgrade_Resource_Weapon_Dust", "Crafting_Resource_Copper_Ore");
        }

        [TestMethod]
        public void SharedData_EveryResolvedCostId_ExistsInTheResourceCatalog()
        {
            var knownIds = File.ReadAllText(Path.Combine(FindSharedDataRoot(), "Resources", "CraftingResources.json"));
            var costs = LoadSharedCosts();

            foreach (var (mode, categories) in costs)
            foreach (var (category, lines) in categories)
            foreach (var rarity in Enum.GetValues<Rarity>())
            foreach (var requirement in Resolve(lines, rarity))
                Assert.IsTrue(knownIds.Contains($"\"{requirement.Id}\""),
                    $"{mode}/{category}/{rarity}: cost id '{requirement.Id}' is not in the resource catalog");
        }

        [TestMethod]
        public void SharedData_AscendSection_CostsOneMarkOfTheCategory()
        {
            var costs = LoadSharedCosts();
            foreach (var category in Enum.GetValues<EquipmentCategory>())
            {
                // The reference price: exactly ONE master's mark of the item's category.
                var line = Resolve(costs[CraftingMode.Ascend][category], Rarity.Legendary).Single();
                Assert.AreEqual(1, line.Amount, $"{category}");
                StringAssert.Contains(line.Id, "_Mark", $"{category}: the ascend cost must be a master's mark, got '{line.Id}'");
            }
        }

        private static void AssertRecraft(
            Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<CostRequirement>>> costs,
            EquipmentCategory category, Rarity rarity, string expectedDust, string expectedMain)
        {
            var resolved = Resolve(costs[CraftingMode.Recraft][category], rarity);
            Assert.AreEqual(2, resolved.Count, $"{category}/{rarity}: dust + main resource expected");
            Assert.IsTrue(resolved.Any(requirement => requirement.Id == expectedDust), $"{category}/{rarity}: no {expectedDust}");
            Assert.IsTrue(resolved.Any(requirement => requirement.Id == expectedMain), $"{category}/{rarity}: no {expectedMain}");
        }

        private static List<IRequirement> Resolve(List<CostRequirement> lines, Rarity rarity) =>
            lines.Select(line => line.Resolve(rarity)).OfType<IRequirement>().ToList();

        private static Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<CostRequirement>>> LoadSharedCosts() =>
            s_parser.ParseUpgradeCosts(File.ReadAllText(Path.Combine(FindSharedDataRoot(), "UpgradeCosts", "UpgradeCosts.json")));

        /// <summary>Walks up from the test binaries to the Testing project's Data/Shared symlink —
        /// the same link pattern every game project uses (see ReputationPipeline).</summary>
        private static string FindSharedDataRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Data", "Shared");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"Data/Shared symlink not found above {AppContext.BaseDirectory}");
        }
    }
}
