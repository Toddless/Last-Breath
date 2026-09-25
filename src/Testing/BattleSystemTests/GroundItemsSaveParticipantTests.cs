namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Core.Save;
    using Core.Session;
    using LootGeneration.Source;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The floor as a file holds it. Loot is the player's the moment it falls, so what he has not walked
    /// over yet has to survive a reload — the thing itself, whole, and the spot it lies on: a sword that
    /// came back re-rolled, or lying somewhere else, would be a different sword found in a different place.
    /// The floor is scene state and nothing resets it between playthroughs, so a file that says nothing
    /// about drops has to say it out loud: bare ground.
    /// </summary>
    [TestClass]
    public class GroundItemsSaveParticipantTests
    {
        private const string Resource = "Crafting_Resource_Coal";

        private const string Retired = "Crafting_Resource_No_Data_Declares";

        [TestMethod]
        public void EveryShapeOfDropComesBackItselfOnTheSpotItWasLeftOn()
        {
            var chest = Chest("Ground_Chest", armor: 210f);
            var augment = AugmentItem(Copy());
            var floor = new Floor();
            floor.Lay(chest, 1, -120.5f, 42.25f);
            floor.Lay(augment, 1, 17f, -8.5f);
            floor.Lay(Stackable(Resource), 7, 300.75f, 900.125f);

            var reloaded = Reloaded(floor);

            CollectionAssert.AreEqual(
                new[] { Signature(chest), Signature(augment), $"{Resource}" },
                reloaded.Placements.Select(drop => Signature(drop.Item)).ToArray(),
                "a drop came back a different item than the one that fell");
            CollectionAssert.AreEqual(
                new[] { (-120.5f, 42.25f), (17f, -8.5f), (300.75f, 900.125f) },
                reloaded.Placements.Select(drop => (drop.X, drop.Y)).ToArray(),
                "a drop came back lying somewhere other than where it was left");
            Assert.AreEqual(7, reloaded.Placements[2].Quantity, "the pile came back a different size than it was left");
        }

        [TestMethod]
        public void DropsRestoreBesideTheOtherPlacesHoldingItemInstances()
        {
            // Neighbourhood, not dependency: nothing has to precede the floor, but the section that
            // rebuilds item instances belongs with the two others that do, not ahead of them.
            Assert.IsTrue(Core.Save.RestoreOrder.GroundItems > Core.Save.RestoreOrder.Items);
            Assert.IsTrue(Core.Save.RestoreOrder.GroundItems > Core.Save.RestoreOrder.TraderShelf);
            Assert.AreEqual(Core.Save.RestoreOrder.GroundItems, Participant(new Floor()).RestoreOrder);
        }

        /// <summary>The round trip is real; the taking is not. What takes a drop off the floor in the world
        /// is <c>LootOrchestrator.TryPickup</c>, which needs a live node — that a picked-up drop is gone
        /// from the floor is a Godot checklist item, not this. Here the store is emptied by hand, and the
        /// pin is the section's half: it writes down what the store holds at capture, and lays back nothing
        /// the store had already let go of.</summary>
        [TestMethod]
        public void ADropTheStoreLetGoOfBeforeTheSaveIsNeitherWrittenNorLaidBack()
        {
            var taken = Stackable(Resource);
            var left = Chest("Ground_Chest", armor: 30f);
            var floor = new Floor();
            floor.Lay(taken, 3, 10f, 10f);
            floor.Lay(left, 1, 20f, 20f);
            floor.LetGo(taken);

            var reloaded = Reloaded(floor);

            Assert.AreEqual(1, reloaded.Placements.Count, "a drop the floor no longer held was written down as still lying there");
            Assert.AreEqual(Signature(left), Signature(reloaded.Placements[0].Item));
        }

        [TestMethod]
        public void ASectionThatNamesNoDropsLaysNone()
        {
            var floor = new Floor();
            floor.Lay(Chest("Ground_Chest", armor: 30f), 1, 10f, 10f);
            var participant = Participant(floor);

            participant.Restore(JToken.Parse("""{"items":[]}"""), participant.Version);

            Assert.AreEqual(0, floor.Placements.Count,
                "a playthrough where everything was picked up was loaded onto the drops of the one being left behind");
        }

        [TestMethod]
        public void AFileThatSaysNothingAboutDropsLeavesBareGround()
        {
            var floor = new Floor();
            floor.Lay(Chest("Ground_Chest", armor: 30f), 1, 10f, 10f);

            var scope = new LoadScope();
            var manager = new SaveManager(scope, () => new SessionResetService(scope));
            manager.Register(Participant(floor));
            manager.Restore(new SaveFile());

            Assert.AreEqual(0, floor.Placements.Count,
                "the loot of the playthrough being left behind stayed on the floor of the one loaded over it");
        }

        [TestMethod]
        public void ADropTheGameCanNoLongerRebuildCostsItsOwnSpotAndNotTheFloor()
        {
            var floor = new Floor();
            var participant = Participant(floor);

            participant.Restore(JToken.Parse(
                $$"""
                  {"items":[
                    {"item":{"Amount":2,"ResourceId":"{{Retired}}"},"x":1.0,"y":2.0},
                    {"item":{"Amount":3,"ResourceId":"{{Resource}}"},"x":3.0,"y":4.0}]}
                  """),
                participant.Version);

            Assert.AreEqual(1, floor.Placements.Count, "one retired id swept the whole floor");
            Assert.AreEqual(Resource, floor.Placements[0].Item.Id);
            Assert.AreEqual((3f, 4f), (floor.Placements[0].X, floor.Placements[0].Y),
                "the surviving drop took the retired one's spot");
        }

        /// <summary>Through real JSON text and onto a floor that never saw the drops: the coordinates and
        /// the rolled numbers have to survive being written down, not be handed over as live objects.</summary>
        private static Floor Reloaded(Floor floor)
        {
            var source = Participant(floor);
            JToken written = JToken.Parse(source.Capture().ToString(Formatting.None));
            var fresh = new Floor();
            Participant(fresh).Restore(written, source.Version);
            return fresh;
        }

        private static GroundItemsSaveParticipant Participant(Floor floor) =>
            new(floor, ItemData(), Converter(), Minter());

        private static EquipItemSaveConverter Converter() =>
            new(new GrantFactory(() => null, () => null, () => null));

        /// <summary>What the player was looking at on the floor: the piece and its rolled lines. Two mints
        /// of one blueprint differ here and nowhere in their ids.</summary>
        private static string Signature(IItem item) => item is not IEquipItem equip
            ? $"{item.Id}"
            : $"{equip.Id}|{equip.Rarity}|{string.Join(',', equip.Modifiers.Select(line => $"{line.EntityParameter}:{line.Value}"))}";

        /// <summary>The item store as it answers a load: a template for what it still declares, and a throw
        /// — its own way of saying "no such id" — for what it does not.</summary>
        private static IItemDataProvider ItemData()
        {
            var itemData = new Mock<IItemDataProvider>();
            itemData.Setup(data => data.CopyItem(It.IsAny<string>()))
                .Returns((string id) => id == Resource ? Stackable(id) : throw new System.ArgumentNullException(id));
            return itemData.Object;
        }

        /// <summary>The augment seam a floor goes through: the copy's numbers come back untouched, never
        /// drawn again.</summary>
        private static IAugmentItemMinter Minter()
        {
            var minter = new Mock<IAugmentItemMinter>();
            minter.Setup(m => m.Remembered(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, float>>(), It.IsAny<Rarity?>(), It.IsAny<string>()))
                .Returns((string id, IReadOnlyDictionary<string, float> values, Rarity? rarity, string effect) =>
                    new AugmentInstance(id, values, rarity ?? Rarity.Common, effect));
            minter.Setup(m => m.Restore(It.IsAny<AugmentInstance>()))
                .Returns((AugmentInstance copy) => AugmentItem(copy));
            return minter.Object;
        }

        private static AugmentInstance Copy() =>
            new("Augment_Test", new Dictionary<string, float> { ["Power"] = 7f }, Rarity.Rare, "Effect_Test");

        private static IAugmentItem AugmentItem(AugmentInstance copy)
        {
            var item = new Mock<IAugmentItem>();
            item.SetupGet(i => i.Augment).Returns(copy);
            item.SetupGet(i => i.Id).Returns(copy.AugmentId);
            item.SetupGet(i => i.MaxStackSize).Returns(1);
            return item.Object;
        }

        private static IItem Stackable(string id) =>
            new CraftingResource(id, 999, [], Mock.Of<Core.Crafting.IMaterial>(), Rarity.Common);

        private static IEquipItem Chest(string id, float armor)
        {
            var item = new EquipItem(EquipmentPiece.Body, id, ["armor"]) { Rarity = Rarity.Rare };
            item.SetModifiers([new SimpleModifier(EntityParameter.Armor, ModifierValueType.Flat, armor, item.InstanceId)]);
            return item;
        }

        /// <summary>The floor without the scene: the drops a save section is offered and the ones it lays
        /// back down. <see cref="LetGo"/> stands in for every way the world takes a drop off the floor —
        /// it repeats none of their rules, it only leaves the store holding one drop fewer.</summary>
        private sealed class Floor : IGroundItemStore
        {
            private List<GroundItemPlacement> _placements = [];

            public IReadOnlyList<GroundItemPlacement> Placements => _placements;

            public IReadOnlyList<GroundItemPlacement> CaptureGroundItems() => _placements;

            public void RestoreGroundItems(IReadOnlyList<GroundItemPlacement> items) => _placements = [.. items];

            public void Lay(IItem item, int quantity, float x, float y) =>
                _placements.Add(new GroundItemPlacement(item, quantity, x, y));

            public void LetGo(IItem item) => _placements.RemoveAll(placement => placement.Item == item);
        }
    }
}
