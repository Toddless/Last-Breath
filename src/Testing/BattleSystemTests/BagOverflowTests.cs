namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.Items;

    /// <summary>
    /// What a full bag answers. The bag used to take the item, drop whatever found no slot and tell
    /// the caller it had all been taken — so the thing sat in no slot, therefore in no save file, and
    /// yet stayed in the register the bag resolves instances through: a search found it, a reload did
    /// not, and nothing ever cleared it because clearing runs off the slot it never had. Everything
    /// that hands the player something reads that answer, and the ground lets go of an item only once
    /// the bag says it took it — which it always did.
    /// <para>
    /// The bag is a grid of engine nodes and cannot stand up outside the runtime, so the rules are
    /// walked on <see cref="SlottedBag"/>, which mirrors them.
    /// </para>
    /// </summary>
    [TestClass]
    public class BagOverflowTests
    {
        private const string Ore = "Crafting_Resource_Iron";

        private const string Coal = "Crafting_Resource_Coal";

        private const string SearchedTag = "Crafting";

        private const int Stack = 20;

        [TestMethod]
        public void AFullBagSaysNoInsteadOfSayingItTookTheItem()
        {
            // The answer is the whole contract: a caller that gets true has handed the item over and
            // stops owning it — the ground clears the pile, the shelf keeps the gold.
            var bag = new SlottedBag(slots: 1);
            bag.TryAddItem(Resource(Coal));

            Assert.IsFalse(bag.TryAddItem(Resource(Ore)), "the bag claimed a place for an item it has nowhere to put");
        }

        [TestMethod]
        public void ARefusedItemLeavesNothingOfItselfBehind()
        {
            // A refusal has to be clean, or the item lives on as a phantom: held by no slot, written
            // to no save, and still answering when the bag is asked what it carries.
            var bag = new SlottedBag(slots: 1);
            bag.TryAddItem(Resource(Coal));
            CraftingResource refused = Resource(Ore);

            bag.TryAddItem(refused);

            Assert.IsNull(bag.GetItem<IItem>(refused.InstanceId), "the refused item stayed in the register as a phantom");
            List<string> found = bag.GetAllItemIdsWithTag(SearchedTag);
            CollectionAssert.Contains(found, Coal, "the search finds nothing at all, so it proves nothing below");
            CollectionAssert.DoesNotContain(found, Ore, "a search still turns up the item the bag refused");
            Assert.AreEqual(1, bag.OccupiedSlots.Count, "the refused item took a slot after all");
            Assert.AreEqual(0, bag.GetTotalItemAmount(Ore));
        }

        [TestMethod]
        public void AnAmountThatDoesNotFitWholeIsNotPouredInPartway()
        {
            // Half the amount placed and half dropped is the same silent loss as all of it dropped,
            // only smaller — and the caller, told yes, lets go of the half that never arrived.
            var bag = new SlottedBag(slots: 2);
            bag.TryAddItem(Resource(Ore), 10);               // ten of a stack of twenty, one slot left
            CraftingResource tooMuch = Resource(Ore);        // room is the ten missing there plus one whole stack

            Assert.IsFalse(bag.TryAddItem(tooMuch, 31), "the bag took on an amount it cannot hold whole");
            Assert.AreEqual(10, bag.GetTotalItemAmount(Ore), "part of the refused amount was poured in anyway");
            Assert.AreEqual(1, bag.OccupiedSlots.Count, "the refused amount opened a slot it was never placed in");
            Assert.IsNull(bag.GetItem<IItem>(tooMuch.InstanceId), "the refused item stayed in the register as a phantom");
        }

        [TestMethod]
        public void AnAmountThatFitsExactlyIsStillTaken()
        {
            // The other edge of the same line: room counted short would turn the fix into a bag that
            // refuses what it can hold, and every reward would start bouncing off a bag with space.
            var bag = new SlottedBag(slots: 2);
            bag.TryAddItem(Resource(Ore), 10);

            Assert.IsTrue(bag.TryAddItem(Resource(Ore), 30), "the bag refused the exact amount it has room for");
            Assert.AreEqual(40, bag.GetTotalItemAmount(Ore));
            Assert.AreEqual(2, bag.OccupiedSlots.Count, "the arrival did not top up the open stack before taking the empty slot");
        }

        [TestMethod]
        public void ATopUpTooBigForWhatIsLeftIsRefusedWholeAsWell()
        {
            // The other door into the same placement: topping up an id already held drops its overflow
            // in exactly the same way, and the crafting refund reads that answer to decide whether it
            // still has to open a stack of its own.
            var bag = new SlottedBag(slots: 2);
            bag.TryAddItem(Resource(Ore), 10);

            Assert.IsFalse(bag.TryAddItemStacks(Ore, 31), "the top-up claimed room for more than the bag can hold");
            Assert.AreEqual(10, bag.GetTotalItemAmount(Ore), "part of the refused top-up went in anyway");
        }

        /// <summary>A plain template of the kind the bag piles up, tagged so a search can look for it.</summary>
        private static CraftingResource Resource(string id) => new(id, Stack, [SearchedTag], null!, Rarity.Common);
    }
}
