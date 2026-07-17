namespace LastBreathTest.BattleSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Items;
    using Core.Modifiers;
    using LootGeneration.Internal;

    /// <summary>Template parsing invariants. Uses the LootGeneration factory — plain domain objects,
    /// no Godot native. Equip templates parse into blueprints (descriptors, no materialization) —
    /// minting behavior lives in <see cref="EquipItemMinterTests"/>.</summary>
    [TestClass]
    public class DataParserTests
    {
        [TestMethod]
        public void ParseEquipItems_ReturnsBlueprintWithUnmaterializedDescriptors()
        {
            const string json = """
            {
                "items": [
                    {
                        "id": "Test_Chest",
                        "equipmentPart": "Body",
                        "rarity": "Rare",
                        "updateLevel": 3,
                        "maxUpdateLevel": 12,
                        "tags": ["armor"],
                        "implicits": [
                            { "parameter": "Armor", "modifierType": "flat", "value": 50 }
                        ],
                        "modifiers": [
                            { "parameter": "Health", "modifierType": "flat", "value": 100 }
                        ]
                    }
                ]
            }
            """;

            var blueprint = CreateParser().ParseEquipItems(json).Single();

            Assert.AreEqual("Test_Chest", blueprint.Id);
            Assert.AreEqual(Core.Enums.EquipmentPiece.Body, blueprint.Piece);
            Assert.IsNull(blueprint.Weapon);
            Assert.AreEqual(Core.Enums.Rarity.Rare, blueprint.Rarity);
            Assert.AreEqual(3, blueprint.UpdateLevel.Min);
            Assert.IsTrue(blueprint.UpdateLevel.IsFixed);
            Assert.AreEqual(12, blueprint.MaxUpdateLevel);
            CollectionAssert.AreEqual(new[] { "armor" }, blueprint.Tags);
            var implicitLine = (ParameterDescriptor)blueprint.Implicits.Single();
            Assert.AreEqual(Core.Enums.EntityParameter.Armor, implicitLine.Parameter);
            Assert.AreEqual(50f, implicitLine.Value.Min, 0.001f);
            var modifierLine = (ParameterDescriptor)blueprint.Modifiers.Single();
            Assert.AreEqual(Core.Enums.EntityParameter.Health, modifierLine.Parameter);
            Assert.AreEqual(100f, modifierLine.Value.Min, 0.001f);
        }

        [TestMethod]
        public void ParseEquipItems_WeaponTemplate_CarriesTheWeaponBlock()
        {
            const string json = """
            {
                "items": [
                    {
                        "id": "Test_Sword",
                        "equipmentPart": "Weapon",
                        "weaponType": "Sword",
                        "handedness": "OneHanded",
                        "damage": 220,
                        "criticalChance": 0.06,
                        "criticalDamage": 1.6,
                        "rarity": "Common",
                        "updateLevel": 0,
                        "maxUpdateLevel": 12,
                        "tags": [],
                        "implicits": [],
                        "modifiers": []
                    }
                ]
            }
            """;

            var blueprint = CreateParser().ParseEquipItems(json).Single();

            var weapon = blueprint.Weapon;
            Assert.IsNotNull(weapon);
            Assert.AreEqual(Core.Enums.WeaponType.Sword, weapon.WeaponType);
            Assert.AreEqual(Core.Enums.Handedness.OneHanded, weapon.Handedness);
            Assert.AreEqual(220f, weapon.Damage, 0.001f);
            Assert.AreEqual(0.06f, weapon.CriticalChance, 0.001f);
            Assert.AreEqual(1.6f, weapon.CriticalDamage, 0.001f);
        }

        [TestMethod]
        public void ParseEquipItems_UpdateLevelRange_IsKeptUnrolled()
        {
            const string json = """
            {
                "items": [
                    {
                        "id": "Test_Chest",
                        "equipmentPart": "Body",
                        "rarity": "Rare",
                        "updateLevel": { "min": 2, "max": 5 },
                        "maxUpdateLevel": 12,
                        "tags": [],
                        "implicits": [],
                        "modifiers": []
                    }
                ]
            }
            """;

            var blueprint = CreateParser().ParseEquipItems(json).Single();

            // The range is minter food: parsing never rolls it.
            Assert.AreEqual(2, blueprint.UpdateLevel.Min);
            Assert.AreEqual(5, blueprint.UpdateLevel.Max);
        }

        [TestMethod]
        public void ParseEquipItems_UpdateLevelRangeBeyondMax_SkipsWholeItem()
        {
            const string json = """
            {
                "items": [
                    {
                        "id": "Test_Chest",
                        "equipmentPart": "Body",
                        "rarity": "Rare",
                        "updateLevel": { "min": 2, "max": 20 },
                        "maxUpdateLevel": 12,
                        "tags": [],
                        "implicits": [],
                        "modifiers": []
                    }
                ]
            }
            """;

            Assert.AreEqual(0, CreateParser().ParseEquipItems(json).Count);
        }

        [TestMethod]
        public void ParseEquipItems_AffixOnAuthoredLine_DropsThatLineOnly()
        {
            const string json = """
            {
                "items": [
                    {
                        "id": "Test_Chest",
                        "equipmentPart": "Body",
                        "rarity": "Rare",
                        "updateLevel": 0,
                        "maxUpdateLevel": 12,
                        "tags": [],
                        "implicits": [],
                        "modifiers": [
                            { "parameter": "Health", "modifierType": "flat", "value": 100, "affix": "Prefix" },
                            { "parameter": "Armor", "modifierType": "flat", "value": 50 }
                        ]
                    }
                ]
            }
            """;

            var blueprint = CreateParser().ParseEquipItems(json).Single();

            // Authored lines never enter an affix roll — claiming a slot family there is a data error.
            var line = (ParameterDescriptor)blueprint.Modifiers.Single();
            Assert.AreEqual(Core.Enums.EntityParameter.Armor, line.Parameter);
        }

        [TestMethod]
        public void ParseResources_ValueAsNumberAndAsRange_ParsesBothForms()
        {
            var descriptors = ParseMaterialDescriptors("""
                { "parameter": "Health", "modifierType": "flat", "value": 100, "weight": 10, "affix": "Prefix" },
                { "parameter": "Damage", "modifierType": "flat", "value": { "min": 10, "max": 20 }, "weight": 10, "affix": "Prefix" }
            """);

            Assert.AreEqual(2, descriptors.Count);
            var fixedLine = (ParameterDescriptor)descriptors[0];
            Assert.IsTrue(fixedLine.Value.IsFixed);
            Assert.AreEqual(100f, fixedLine.Value.Min, 0.001f);
            var ranged = (ParameterDescriptor)descriptors[1];
            Assert.AreEqual(10f, ranged.Value.Min, 0.001f);
            Assert.AreEqual(20f, ranged.Value.Max, 0.001f);
        }

        [TestMethod]
        public void ParseResources_RangeMinAboveMax_SkipsEntry()
        {
            var descriptors = ParseMaterialDescriptors("""
                { "parameter": "Health", "modifierType": "flat", "value": { "min": 30, "max": 10 }, "weight": 10, "affix": "Prefix" }
            """);

            Assert.AreEqual(0, descriptors.Count);
        }

        [TestMethod]
        public void ParseResources_RangeMissingBound_SkipsEntry()
        {
            var descriptors = ParseMaterialDescriptors("""
                { "parameter": "Health", "modifierType": "flat", "value": { "min": 30 }, "weight": 10, "affix": "Prefix" }
            """);

            Assert.AreEqual(0, descriptors.Count);
        }

        [TestMethod]
        public void ParseResources_AffixParsesStrictly()
        {
            var descriptors = ParseMaterialDescriptors("""
                { "parameter": "Health", "modifierType": "flat", "value": 100, "weight": 10, "affix": "Suffix" },
                { "parameter": "Armor", "modifierType": "flat", "value": 50, "weight": 10, "affix": "Prefiks" }
            """);

            // A typo never falls back silently: the bad entry is dropped, the valid one keeps its kind.
            var line = (ParameterDescriptor)descriptors.Single();
            Assert.AreEqual(Core.Enums.AffixKind.Suffix, line.Affix);
        }

        [TestMethod]
        public void ParseResources_MissingAffix_SkipsEntry()
        {
            var descriptors = ParseMaterialDescriptors("""
                { "parameter": "Health", "modifierType": "flat", "value": 100, "weight": 10, "affix": "Prefix" },
                { "parameter": "Armor", "modifierType": "flat", "value": 50, "weight": 10 }
            """);

            // Rollable pools are annotated now: an entry without a slot family could never occupy
            // a slot, so absence is a data error (reported and dropped), not a silent None.
            var line = (ParameterDescriptor)descriptors.Single();
            Assert.AreEqual(Core.Enums.EntityParameter.Health, line.Parameter);
        }

        [TestMethod]
        public void ParseResources_ExplicitNoneAffix_SkipsEntry()
        {
            var descriptors = ParseMaterialDescriptors("""
                { "parameter": "Health", "modifierType": "flat", "value": 100, "weight": 10, "affix": "None" }
            """);

            Assert.AreEqual(0, descriptors.Count);
        }

        [TestMethod]
        public void ParseEquipItemModifierPools_ReturnsAffixedDescriptors()
        {
            const string json = """
            {
                "pools": [
                    {
                        "id": "Test_Pool",
                        "modifiersPool": [
                            { "parameter": "Health", "modifierType": "flat", "value": 100, "weight": 50, "affix": "Prefix" },
                            { "parameter": "Strength", "modifierType": "inc", "value": 0.2, "weight": 30, "affix": "Suffix" },
                            { "parameter": "Armor", "modifierType": "flat", "value": 40, "weight": 10 }
                        ]
                    }
                ]
            }
            """;

            var pools = CreateParser().ParseEquipItemModifierPools(json);

            // The unmarked Armor entry is dropped by the strict affix policy; the rest parse into descriptors.
            var pool = pools["Test_Pool"];
            Assert.AreEqual(2, pool.Count);
            Assert.AreEqual(Core.Enums.AffixKind.Prefix, pool[0].Affix);
            Assert.AreEqual(Core.Enums.AffixKind.Suffix, pool[1].Affix);
            Assert.AreEqual(0.2f, ((ParameterDescriptor)pool[1]).Value.Min, 0.001f);
        }

        [TestMethod]
        public void ParseResources_AffixOnCompositePart_SkipsWholeEntry()
        {
            var descriptors = ParseMaterialDescriptors("""
                {
                    "weight": 10,
                    "affix": "Prefix",
                    "parts": [
                        { "parameter": "Health", "modifierType": "flat", "value": 100, "affix": "Suffix" },
                        { "parameter": "Armor", "modifierType": "flat", "value": 50 }
                    ]
                }
            """);

            Assert.AreEqual(0, descriptors.Count);
        }

        [TestMethod]
        public void ParseResources_AffixOnCompositeRoot_IsCarriedByTheRoot()
        {
            var descriptors = ParseMaterialDescriptors("""
                {
                    "weight": 10,
                    "affix": "Prefix",
                    "parts": [
                        { "parameter": "Health", "modifierType": "flat", "value": 100 },
                        { "parameter": "Armor", "modifierType": "flat", "value": 50 }
                    ]
                }
            """);

            var composite = (CompositeDescriptor)descriptors.Single();
            Assert.AreEqual(Core.Enums.AffixKind.Prefix, composite.Affix);
            Assert.AreEqual(2, composite.Parts.Count);
        }

        /// <summary>Runs the given modifier lines through the material path of ParseResources —
        /// the descriptor pipeline every rollable pool uses.</summary>
        private static IReadOnlyList<IModifierDescriptor> ParseMaterialDescriptors(string modifiersJson)
        {
            string json = $$"""
            {
                "materialCategories": [
                    { "id": "Category_Test", "modifiers": [] }
                ],
                "upgradeResources": [],
                "craftingResources": [
                    {
                        "id": "Test_Resource",
                        "material": {
                            "id": "Test_Material",
                            "categoryId": "Category_Test",
                            "modifiers": [ {{modifiersJson}} ]
                        },
                        "maxStackSize": 99,
                        "tags": [],
                        "rarity": "Uncommon"
                    }
                ]
            }
            """;

            var resource = (ICraftingResource)CreateParser().ParseResources(json).Single();
            return resource.Material!.Modifiers;
        }

        private static DataParser CreateParser() => new(new ItemGameDataFactory());
    }
}
