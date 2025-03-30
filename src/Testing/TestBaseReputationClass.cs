namespace PlaygroundTest
{
    using Moq;
    using Playground.Script.Reputation;

    [TestClass]
    public sealed class TestBaseReputationClass
    {
        [TestInitialize]
        public void TestInit()
        {

        }

        [TestMethod]
        public void TestElfsReputationConstruktor()
        {
            var baseRep = new Mock<BaseReputation>(50).Object;
            Assert.IsTrue(baseRep.CurrentReputationPoints == 50);
        }

        [TestMethod]
        public void TestRankChange()
        {
            var baseRep = new Mock<BaseReputation>(50).Object;
            baseRep.ChangeReputationPoints(990);

            Assert.IsTrue(baseRep.CurrentRank == Playground.Script.Enums.ReputationRank.Friendliness);
        }

        [TestMethod]
        public void TestChangeRankToNegative()
        {
            var baseRep = new Mock<BaseReputation>(1001).Object;

            baseRep.ChangeReputationPoints(-2410);

            Assert.IsTrue(baseRep.CurrentRank == Playground.Script.Enums.ReputationRank.Detestation);
        }

        [TestMethod]
        public void TestMaxPositiveRank()
        {
            var baseRep = new Mock<BaseReputation>(MockBehavior.Default, 0).Object;
            baseRep.ChangeReputationPoints(3002);

            Assert.IsTrue(baseRep.CurrentRank == Playground.Script.Enums.ReputationRank.Allyship);
        }

        [TestMethod]
        public void TestMaxNegativeRank()
        {
            var baseRep = new Mock<BaseReputation>(MockBehavior.Default, 0).Object;
            baseRep.ChangeReputationPoints(-3002);

            Assert.IsTrue(baseRep.CurrentRank == Playground.Script.Enums.ReputationRank.Hate);
        }
    }
}
