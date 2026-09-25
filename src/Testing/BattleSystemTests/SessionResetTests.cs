namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using Battle.Source;
    using Core.Enums;
    using Core.Events;
    using Core.MessageBus;
    using Core.Narrative.Facts;
    using Core.Reputation;
    using Core.Save;
    using Core.Session;
    using Moq;

    [TestClass]
    public class SessionResetTests
    {
        [TestMethod]
        public void ResetRunsEveryParticipantInOrderInsideLoadScope()
        {
            var loadScope = new LoadScope();
            var service = new SessionResetService(loadScope);
            var order = new List<string>();
            bool wasLoading = false;

            service.Register(new FakeResettable(() => { wasLoading = loadScope.IsLoading; order.Add("first"); }));
            service.Register(new FakeResettable(() => order.Add("second")));

            service.ResetSession();

            CollectionAssert.AreEqual(new[] { "first", "second" }, order);
            Assert.IsTrue(wasLoading, "the reset must run inside the load scope to mute gameplay notifications");
            Assert.IsFalse(loadScope.IsLoading);
        }

        [TestMethod]
        public void OneBrokenParticipantDoesNotStopTheRest()
        {
            var service = new SessionResetService(new LoadScope());
            bool lastReset = false;

            service.Register(new FakeResettable(() => throw new InvalidOperationException("broken")));
            service.Register(new FakeResettable(() => lastReset = true));

            service.ResetSession();

            Assert.IsTrue(lastReset);
        }

        [TestMethod]
        public void DuplicateRegistrationResetsOnce()
        {
            var service = new SessionResetService(new LoadScope());
            int resets = 0;
            var participant = new FakeResettable(() => resets++);

            service.Register(participant);
            service.Register(participant);
            service.ResetSession();

            Assert.AreEqual(1, resets);
        }

        [TestMethod]
        public void WorldFactsResetIsSilentAndComplete()
        {
            var facts = new WorldFactsService();
            facts.SetFact("Quest_Done");
            facts.Add("Kills", 5);
            bool eventFired = false;
            facts.FactChanged += (_, _) => eventFired = true;

            facts.ResetSession();

            Assert.AreEqual(0, facts.Snapshot.Count);
            Assert.IsFalse(eventFired, "a session reset must not storm FactChanged subscribers");
        }

        [TestMethod]
        public void FactionRelationsResetReturnsPlayerToDataDefaults()
        {
            var relations = new FactionRelationService(FactionTestData.Create());
            relations.AddReputation(Fractions.Elf, 1100, "TestDeed");
            relations.AddReputation(Fractions.Undead, 900, "TestDeed");

            relations.ResetSession();

            Assert.AreEqual(0, relations.GetReputation(Fractions.Elf));
            Assert.AreEqual(RelationLevel.Neutral, relations.GetPlayerRelation(Fractions.Elf));
            Assert.AreEqual(-1000, relations.GetReputation(Fractions.Undead));
            Assert.AreEqual(RelationLevel.Hostility, relations.GetPlayerRelation(Fractions.Undead));
        }

        [TestMethod]
        public void MartialArtMasteryResetZeroesLevelExperienceAndBonus()
        {
            var mastery = new MartialArtMastery(Mock.Of<IGameMessageBus>());
            mastery.AddExperience(10_000);
            mastery.AddBonusLevel();
            Assert.IsTrue(mastery.CurrentLevel > 1, "the setup must actually level up");

            mastery.ResetSession();

            Assert.AreEqual(0, mastery.CurrentLevel); // mastery counts from zero: a fresh game has no levels
            Assert.AreEqual(0, mastery.CurrentExperience);
            Assert.AreEqual(0, mastery.BonusLevel);
        }

        [TestMethod]
        public void PersonalReputationResetForgetsEveryone()
        {
            var personal = new PersonalReputationService(
                new FactionRelationService(FactionTestData.Create()), Mock.Of<IGameEventBus>());
            personal.SetPersonal("npc-1", 500);

            personal.ResetSession();

            Assert.AreEqual(0, personal.GetPersonal("npc-1"));
            Assert.AreEqual(0, personal.Snapshot.Count);
        }

        private sealed class FakeResettable(Action onReset) : ISessionResettable
        {
            public void ResetSession() => onReset();
        }
    }
}
