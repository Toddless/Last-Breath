namespace PlaygroundTest
{
    using Playground.Script.Enums;
    using Playground.Script.Reputation;

    [TestClass]
    public class ReputationManagerTest
    {

        //[TestMethod]
        //public void TestAllThreeRaceNeutral()
        //{
        //    var repManager = new ReputationManager(0, 0, 0);
        //    Assert.IsTrue(repManager.ElfReputation?.CurrentRank == ReputationRank.Neutrality);
        //    Assert.IsTrue(repManager.HumansReputation?.CurrentRank == ReputationRank.Neutrality);
        //    Assert.IsTrue(repManager.DwarfsReputation?.CurrentRank == ReputationRank.Neutrality);
        //}

        //[TestMethod]
        //public void TestElfsPositiovDwarfsNegativ()
        //{
        //    var repManager = new ReputationManager(1001, -1001, 0);

        //    Assert.IsTrue(repManager.ElfReputation?.CurrentRank == ReputationRank.Friendliness);
        //    Assert.IsTrue(repManager.DwarfsReputation?.CurrentRank == ReputationRank.Detestation);
        //}

        //[TestMethod]
        //public void TestDwarfsPositivElfsNegativ()
        //{
        //    var repManager = new ReputationManager(-1001, 1001, 0);

        //    Assert.IsTrue(repManager.ElfReputation?.CurrentRank == ReputationRank.Detestation);
        //    Assert.IsTrue(repManager.DwarfsReputation?.CurrentRank == ReputationRank.Friendliness);
        //}

        //[TestMethod]
        //public void TestChangeNonCoflictFractions()
        //{
        //    var repManager = new ReputationManager(0, 0, 0);
        //    var rankList = new List<Fractions>() { Fractions.Elf, Fractions.Human };
        //    repManager.ChangeReputationForMultipleFractions(rankList, 10);

        //    Assert.IsTrue(repManager.ElfReputation?.CurrentReputationPoints == 10);
        //    Assert.IsTrue(repManager.HumansReputation?.CurrentReputationPoints == 10);
        //}

        //[TestMethod]
        //public void TestChangeCoflictFractions()
        //{
        //    var repManager = new ReputationManager(0, 0, 0);

        //    repManager.ChangeReputationConflictFractions(Fractions.Elf, Fractions.Dwarf, 650);

        //    Assert.IsTrue(repManager.ElfReputation?.CurrentReputationPoints == -650);
        //    Assert.IsTrue(repManager.DwarfsReputation?.CurrentReputationPoints == 650);
        //}

        //[TestMethod]
        //public void TestAllFractionsGetPositiveRepPoints()
        //{
        //    var repManager = new ReputationManager(0, 0, 0);
        //    repManager.ChangeReputationForMultipleFractions([Fractions.Human, Fractions.Dwarf, Fractions.Elf], 50);

        //    Assert.IsTrue(repManager.ElfReputation?.CurrentReputationPoints == 50);
        //    Assert.IsTrue(repManager.HumansReputation?.CurrentReputationPoints == 50);
        //    Assert.IsTrue(repManager.DwarfsReputation?.CurrentReputationPoints == 50);
        //}

        //[TestMethod]
        //public void TestAllFractionsGetNegativRepPoints()
        //{
        //    var repManager = new ReputationManager(0, 0, 0);
        //    repManager.ChangeReputationForMultipleFractions([Fractions.Human, Fractions.Dwarf, Fractions.Elf], -50);

        //    Assert.IsTrue(repManager.ElfReputation?.CurrentReputationPoints == -50);
        //    Assert.IsTrue(repManager.HumansReputation?.CurrentReputationPoints == -50);
        //    Assert.IsTrue(repManager.DwarfsReputation?.CurrentReputationPoints == -50);
        //}

        //[TestMethod]
        //public void TestNullFractionUnaffectedOthers()
        //{
        //    var repManager = new ReputationManager(0, 0, 0);

        //    repManager.ChangeReputationForMultipleFractions([Fractions.None], 50);
        //    Assert.IsTrue(repManager.HumansReputation?.CurrentReputationPoints == 0);
        //    Assert.IsTrue(repManager.ElfReputation?.CurrentReputationPoints == 0);
        //    Assert.IsTrue(repManager.DwarfsReputation?.CurrentReputationPoints == 0);
        //}
    }
}
