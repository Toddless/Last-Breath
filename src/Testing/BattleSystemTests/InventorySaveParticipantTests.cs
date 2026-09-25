namespace LastBreathTest.BattleSystemTests
{
    using Core.Data;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Save;
    using Core.Save.Participants;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The bag as a file holds it. How many of a thing was put away is as much the bag as which thing it
    /// was: a half-spent stack that came back full would make saving a way to refill it. And what the bag
    /// holds is a list of independent entries — an id the game has since retired costs its own line and
    /// leaves the rest of the bag standing, because the alternative is a player losing everything he
    /// owned over one renamed resource.
    /// </summary>
    [TestClass]
    public class InventorySaveParticipantTests
    {
        private const string Held = "Crafting_Resource_Coal";

        private const string Retired = "Crafting_Resource_No_Data_Declares";

        private const int BagSlots = 20;

        [TestMethod]
        public void APartlySpentStackComesBackTheSizeItWasSavedAt()
        {
            var bag = new SlottedBag(BagSlots);
            bag.TryAddItem(Stackable(Held), 9);
            bag.RemoveItemById(Held, 4);
            var participant = Participant(bag);

            JToken written = JToken.Parse(participant.Capture().ToString(Formatting.None)); // through the file, not around it
            participant.Restore(written, participant.Version);

            Assert.AreEqual(5, bag.GetTotalItemAmount(Held), "the stack came back a different size than it was saved at");
        }

        [TestMethod]
        public void AnIdTheGameNoLongerHoldsCostsItsOwnEntryAndNotTheBag()
        {
            var bag = new SlottedBag(BagSlots);
            var participant = Participant(bag);

            participant.Restore(JToken.Parse(
                $$"""{"Items":[{"Amount":2,"ResourceId":"{{Retired}}"},{"Amount":3,"ResourceId":"{{Held}}"}]}"""),
                participant.Version);

            Assert.AreEqual(0, bag.GetTotalItemAmount(Retired), "the store has nothing to rebuild a retired id from");
            Assert.AreEqual(3, bag.GetTotalItemAmount(Held), "one retired id emptied the whole bag");
        }

        private static InventorySaveParticipant Participant(SlottedBag bag) =>
            new(bag, ItemData(), new EquipItemSaveConverter(new GrantFactory(() => null, () => null, () => null)));

        /// <summary>The item store as it answers a load: a template for what it still declares, and a
        /// throw — its own way of saying "no such id" — for what it does not.</summary>
        private static IItemDataProvider ItemData()
        {
            var itemData = new Mock<IItemDataProvider>();
            itemData.Setup(data => data.CopyItem(It.IsAny<string>()))
                .Returns((string id) => id == Held ? Stackable(id) : throw new System.ArgumentNullException(id));
            return itemData.Object;
        }

        private static CraftingResource Stackable(string id) => new(id, 20, [], null!, Rarity.Common);
    }
}
