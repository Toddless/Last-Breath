namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Localization;
    using Core.Views;
    using Core.Views.UI;
    using Moq;

    /// <summary>
    /// The card an augment is read by. One copy is shown in three places — carried in the tray, offered
    /// by the picker an empty slot opens, and seated in a socket cell — and the player has to recognise
    /// it as the same augment in all three: the same name, the same tier, the same numbers.
    ///
    /// So the card is assembled once (<see cref="AugmentText"/>), and these walks are about what that one
    /// assembly says — including the two things only a SEATED copy can say: whose tier is printed beside
    /// it, and whether what it offers is actually running.
    /// </summary>
    [TestClass]
    public class AugmentCardTests
    {
        private const string Name = "Augment_Sharpened";
        private const string Description = "Bleeding lasts 2 turns longer";

        private const int SlotTier = 3;
        private const int RecordTier = 1;

        [TestInitialize]
        public void Setup()
        {
            var localization = new Mock<ILocalizationService>();
            localization.Setup(service => service.Localize(It.IsAny<string>())).Returns<string>(key => key);
            localization
                .Setup(service => service.Render(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<TextFormat>()))
                .Returns<string, IReadOnlyDictionary<string, object?>, TextFormat>(
                    (key, values, _) => $"{key}:{values[AugmentText.TierValue]}");

            Localization.Override(localization.Object);
        }

        [TestMethod]
        public void ASeatedCopyPrintsItsOwnTierAndNotTheSlotsOne()
        {
            // A tier-one copy dropped into a tier-three socket is still a tier-one copy. The slot's tier
            // is the badge on the cell — it is what a refusal about tiers is about — and a card borrowing
            // it would make one augment read one way in the bag and another in the socket.
            AugmentCard card = AugmentText.Card(Seated(AugmentActivity.Working));

            StringAssert.Contains(card.TierLine, RecordTier.ToString(),
                "the card of a seated copy does not print the tier the augment is written at");
            Assert.IsFalse(card.TierLine.Contains(SlotTier.ToString()),
                "the card borrowed the tier of the slot the copy happens to sit in");
        }

        [TestMethod]
        public void TheBagAndTheSocketShowTheSameAugmentTheSameWay()
        {
            // The whole point of one assembly. The two screens are handed different view models — a tray
            // tile knows nothing of slots — and the player must still recognise his augment.
            AugmentCard carried = AugmentText.Card(Carried());
            AugmentCard seated = AugmentText.Card(Seated(AugmentActivity.Working));

            Assert.AreEqual(carried.Name, seated.Name);
            Assert.AreEqual(carried.TierLine, seated.TierLine, "one copy is announced at two different tiers");
            Assert.AreEqual(carried.Description, seated.Description, "one copy describes itself two different ways");
            Assert.AreEqual(carried.RarityColor, seated.RarityColor, "one copy is painted in two different rarities");
        }

        [TestMethod]
        public void ADormantCopySaysSoInsteadOfPromisingItsNumbers()
        {
            // An augment every move of which lost to a stronger one gives nothing at all. A card that
            // printed its numbers and stopped there would be describing an augment the player is not
            // getting — and the mark on the cell says something happened without saying what.
            AugmentCard dormant = AugmentText.Card(Seated(AugmentActivity.Dormant));
            AugmentCard partly = AugmentText.Card(Seated(AugmentActivity.Partly));
            AugmentCard working = AugmentText.Card(Seated(AugmentActivity.Working));

            StringAssert.Contains(dormant.Description, Description, "the card lost the numbers it is about");
            StringAssert.Contains(dormant.Description, AugmentText.Dormant, "a dormant augment promised its numbers anyway");
            StringAssert.Contains(partly.Description, AugmentText.PartlyDormant, "a half-beaten augment said nothing about it");
            Assert.AreEqual(Description, working.Description, "a working augment was reported as asleep");
        }

        [TestMethod]
        public void ARemoveOnlyCellSaysTheSlotIsShutRatherThanWhoBeatWhom()
        {
            // The node behind the slot is gone: the numbers are not being handed out, which is the thing
            // the player has to read before he decides to leave the copy sitting there. Said in the gate's
            // own words — a drop on this slot would be refused for exactly this reason — and NOT as a lost
            // rivalry: no ability holds this copy up against anything any more, so there is no verdict to
            // repeat and repeating one would be inventing an answer nobody gave.
            AugmentCellView held = Seated(AugmentActivity.Dormant) with { Kind = AugmentCellKind.Held };

            string description = AugmentText.Card(held).Description;

            StringAssert.Contains(description, Description, "the card lost the numbers it is about");
            StringAssert.Contains(description, AugmentRefusalText.KeyFor(AugmentInstallOutcome.SocketClosed),
                "a slot that can only be emptied said nothing about being shut");
            Assert.IsFalse(description.Contains(AugmentText.Dormant),
                "a slot no ability owns any more reported an augment of its as beaten by a rival");
        }

        [TestMethod]
        public void WithoutARecordTheCardSimplyHasNoTierLine()
        {
            // A composition supplying no records cannot say what tier a copy is written at. Tiers start at
            // one, so the zero it answers with is an unknown tier — and "Tier 0" would be a number the
            // catalog never wrote.
            AugmentCard card = AugmentText.Card(Seated(AugmentActivity.Working) with { AugmentTier = 0 });

            Assert.AreEqual(string.Empty, card.TierLine, "a tier nobody declared was printed as tier zero");
            Assert.AreEqual(Description, card.Body, "the missing tier left a blank line above the description");
        }

        [TestMethod]
        public void ThePickersOneTextFieldCarriesBothHalvesOfTheCard()
        {
            // A picker row has a single stretch of text under the pointer where a tooltip has a title and
            // a body. It joins the two halves rather than choosing one of them.
            AugmentCard card = AugmentText.Card(Carried());

            StringAssert.Contains(card.Body, card.TierLine, "the picker row dropped the tier");
            StringAssert.Contains(card.Body, card.Description, "the picker row dropped what the augment does");
        }

        private static AugmentTrayTileView Carried() =>
            new("instance", Name, Name, Description, null, Rarity.Rare, RecordTier);

        private static AugmentCellView Seated(AugmentActivity activity) =>
            new("socket|ability|3", AugmentCellKind.Filled, SlotTier, RecordTier,
                Name, Name, Description, null, Rarity.Rare, activity);
    }
}
