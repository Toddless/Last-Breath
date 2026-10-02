namespace LastBreathTest.Narrative
{
    using Battle.Source;
    using Core.Battle;
    using Core.MessageBus;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.PassiveTree.Allocation;
    using Core.Save.Participants;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The narrative "here are passive tree points" reward. The tree budget is the fifty points the
    /// curve pays plus whatever quests hand out on top, and the tree itself keeps no budget — it is
    /// told a total. So the tests pin where a granted point lives (mastery), that it reaches the
    /// wheel the moment it is granted, that it survives a save, that a new playthrough starts
    /// without it, and that an entry naming a broken amount is refused rather than rounded.
    /// </summary>
    [TestClass]
    public class GrantTreePointsActionTests
    {
        private const string TrialEntry = """{"type": "GrantTreePoints", "amount": 3}""";

        [TestMethod]
        public void GrantedPoints_GrowTheTotalAndReachTheTree()
        {
            var tree = new TreePointsSpy();
            var mastery = CreateMastery(tree.Service);
            mastery.AddExperience(mastery.ExpToNextLevelTotal()); // one earned level under the reward
            int before = mastery.TotalPoints;

            Action(mastery, 3).Execute(NarrativeContext.Empty);

            Assert.AreEqual(before + 3, mastery.TotalPoints, "a reward of three points must be worth three points");
            Assert.AreEqual(3, mastery.BonusPoints);
            Assert.AreEqual(mastery.TotalPoints, tree.TotalPoints, "the wheel must hear the new total at once, like a level up");
        }

        [TestMethod]
        public void GrantsAndLevels_AddUpIntoOneBudget()
        {
            var tree = new TreePointsSpy();
            var mastery = CreateMastery(tree.Service);

            Action(mastery, 4).Execute(NarrativeContext.Empty);
            while (mastery.CurrentLevel < mastery.MaximumLevel)
                mastery.AddExperience(mastery.ExpToNextLevelTotal());

            Assert.AreEqual(mastery.MaximumLevel + 4, tree.TotalPoints,
                "the budget is the curve plus the rewards — levelling must not overwrite what a quest paid");
        }

        [TestMethod]
        public void TheSameActionPaysAgainEveryTimeItRuns()
        {
            var mastery = CreateMastery();
            var action = Action(mastery, 5);

            action.Execute(NarrativeContext.Empty);
            action.Execute(NarrativeContext.Empty);

            Assert.AreEqual(10, mastery.BonusPoints,
                "narrative actions do not deduplicate themselves — an unrepeatable quest is what pays once");
        }

        [TestMethod]
        public void GrantedPoints_SurviveASaveAndLoad()
        {
            var source = CreateMastery();
            source.AddExperience(500); // enough for a few earned levels
            Action(source, 7).Execute(NarrativeContext.Empty);
            int earnedLevel = source.CurrentLevel;

            var captured = new MasterySaveParticipant(source).Capture();

            var tree = new TreePointsSpy();
            var target = CreateMastery(tree.Service);
            new MasterySaveParticipant(target).Restore(captured, savedVersion: 2);

            Assert.AreEqual(7, target.BonusPoints);
            Assert.AreEqual(earnedLevel + 7, target.TotalPoints);
            Assert.AreEqual(target.TotalPoints, tree.TotalPoints,
                "mastery restores before the allocation that spends the points, so the total must be stated on load");
        }

        [TestMethod]
        public void ASectionWrittenBeforeTheRewardExisted_RestoresNoBonus()
        {
            var mastery = CreateMastery();
            Action(mastery, 6).Execute(NarrativeContext.Empty);

            // Written out by hand rather than through MasterySaveData: the record now carries the
            // field, so a captured object would test a version 2 section holding a zero — not a
            // section that never had the property at all, which is what those files hold.
            var legacy = JObject.Parse("""{"baseLevel": 2, "experience": 0}""");
            new MasterySaveParticipant(mastery).Restore(legacy, savedVersion: 1);

            Assert.AreEqual(0, mastery.BonusPoints, "the restore states the whole granted budget, it does not add to the session's");
            Assert.AreEqual(2, mastery.TotalPoints);
        }

        [TestMethod]
        public void ResetSession_TakesTheGrantedPointsBack()
        {
            var tree = new TreePointsSpy();
            var mastery = CreateMastery(tree.Service);
            mastery.AddExperience(500);
            Action(mastery, 9).Execute(NarrativeContext.Empty);

            mastery.ResetSession();

            Assert.AreEqual(0, mastery.BonusPoints, "a new playthrough starts owing nothing to the last one's quests");
            Assert.AreEqual(0, mastery.TotalPoints);
            Assert.AreEqual(0, tree.TotalPoints);
        }

        [TestMethod]
        public void FactoryParsesTheRewardEntry()
        {
            var mastery = CreateMastery();

            var action = Factory(mastery).Create(JObject.Parse(TrialEntry), null!);

            Assert.IsNotNull(action);
            action.Execute(NarrativeContext.Empty);
            Assert.AreEqual(3, mastery.BonusPoints);
        }

        [TestMethod]
        public void FactoryRefusesAnAmountThatIsNotAWholeNumberOfPoints()
        {
            var factory = Factory(CreateMastery());

            Assert.IsNull(factory.Create(JObject.Parse("""{"type": "GrantTreePoints"}"""), null!),
                "amount is required — a reward of nothing is a typo");
            Assert.IsNull(factory.Create(JObject.Parse("""{"type": "GrantTreePoints", "amount": 0}"""), null!),
                "zero points is not a reward");
            Assert.IsNull(factory.Create(JObject.Parse("""{"type": "GrantTreePoints", "amount": -2}"""), null!),
                "a quest must not take the tree away");
            Assert.IsNull(factory.Create(JObject.Parse("""{"type": "GrantTreePoints", "amount": "three"}"""), null!),
                "an amount that is not a number is a broken entry, not a zero");
            Assert.IsNull(factory.Create(JObject.Parse("""{"type": "GrantTreePoints", "amount": 2.5}"""), null!),
                "half a point cannot be spent — the entry must say what it means");
        }

        private static GrantTreePointsActionFactory Factory(IMartialArtMastery mastery) => new(mastery);

        private static INarrativeAction Action(IMartialArtMastery mastery, int amount) =>
            new GrantTreePointsAction(mastery, amount);

        private static MartialArtMastery CreateMastery(IPassiveTreeService? tree = null) =>
            new(Mock.Of<IGameMessageBus>(), () => tree);

        /// <summary>Records what mastery states as the tree point total.</summary>
        private sealed class TreePointsSpy
        {
            public TreePointsSpy()
            {
                var mock = new Mock<IPassiveTreeService>();
                mock.Setup(tree => tree.SetTotalPoints(It.IsAny<int>()))
                    .Callback((int points) => TotalPoints = points);
                Service = mock.Object;
            }

            public IPassiveTreeService Service { get; }

            public int TotalPoints { get; private set; }
        }
    }
}
