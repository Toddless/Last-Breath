namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Save.Participants;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using static AbilityBookStand;

    /// <summary>
    /// A cooldown is spent in a battle and outlives it: nothing ticks it down in the world, so a
    /// remainder is a debt the character carries between fights. Loading used to hand a fresh book with
    /// every ability ready, which made quitting to the menu the cheapest way to refresh a rotation.
    ///
    /// THE REMAINDER IS THE FILE'S. It comes back exactly as written, with no ceiling measured against
    /// what the ability charges today: the figure was issued under the arrangement the save was made
    /// with, and taking an augment off since is not a refund. The one correction is the floor — a
    /// negative would never tick back to zero and would lock the ability shut for the rest of the game.
    ///
    /// ONLY WHAT IS OWED IS WRITTEN. A ready ability is the ordinary case; its zero belongs in no file.
    /// </summary>
    [TestClass]
    public class AbilityCooldownSaveTests
    {
        private const string Spent = "Ability_Spent";
        private const string AlsoSpent = "Ability_Also_Spent";
        private const string Ready = "Ability_Ready";

        [TestMethod]
        public void WhatEachAbilityStillOwesSurvivesASaveAndLoad()
        {
            // The whole of the fix, walked as the game walks it: a book that has spent two abilities is
            // written to text, and a brand new book — the one the reloaded scene builds — is dressed from
            // that text. A ready ability is not in the file at all, and comes back ready.
            IAbilityBookComponent spentBook = BookHolding((Spent, 3), (AlsoSpent, 1), (Ready, 0));

            string file = Participant(spentBook).Capture().ToString(Formatting.None);

            Assert.AreEqual($$"""{"{{Spent}}":3,"{{AlsoSpent}}":1}""", JToken.Parse(file)["cooldowns"]?.ToString(Formatting.None),
                "the file names an ability that owes nothing, or lost one that owes something");

            IAbilityBookComponent freshBook = BookHolding((Spent, 0), (AlsoSpent, 0), (Ready, 0));
            AbilityBookSaveParticipant participant = Participant(freshBook);
            participant.Restore(JToken.Parse(file), participant.Version);

            Assert.AreEqual(3, Owed(freshBook, Spent), "the ability came back ready and the battle was refunded by quitting");
            Assert.AreEqual(1, Owed(freshBook, AlsoSpent), "the ability came back ready and the battle was refunded by quitting");
            Assert.AreEqual(0, Owed(freshBook, Ready), "an ability that owed nothing was handed a debt");
        }

        [TestMethod]
        public void ARemainderLargerThanTheAbilityChargesTodayComesBackWhole()
        {
            // No ceiling. The figure was issued when the ability's cooldown was longer — an augment
            // shortening it has been taken off since — and trimming it to today's charge would pay the
            // player for the change. The saved remainder is what he owes.
            IAbilityBookComponent book = BookHolding((Spent, 0));
            AbilityBookSaveParticipant participant = Participant(book);

            participant.Restore(File((Spent, 99)), participant.Version);

            Assert.AreEqual(99, Owed(book, Spent), "the remainder was trimmed to what the ability charges now");
        }

        [TestMethod]
        public void ANegativeRemainderComesBackReadyRatherThanLockedShut()
        {
            // The one figure corrected on the way in. A cast is allowed at zero exactly, and nothing
            // outside a battle ticks anything down, so a negative in the file is an ability that could
            // never be cast again for the rest of the playthrough.
            IAbilityBookComponent book = BookHolding((Spent, 0));
            AbilityBookSaveParticipant participant = Participant(book);

            participant.Restore(File((Spent, -4)), participant.Version);

            Assert.AreEqual(0, Owed(book, Spent), "a negative remainder was seated and the ability is shut forever");
        }

        [TestMethod]
        public void AnAbilityTheBookNoLongerHoldsIsSkippedAndTheRestStillLands()
        {
            // The allocation is the only authority over which abilities the character owns, so a file
            // remembering one it no longer grants names nothing — exactly as a slot naming one does.
            // What must not happen is the entry taking the section down with it.
            IAbilityBookComponent book = BookHolding((Spent, 0));
            AbilityBookSaveParticipant participant = Participant(book);

            participant.Restore(File(("Ability_The_Tree_Never_Grants", 5), (Spent, 2)), participant.Version);

            Assert.AreEqual(2, Owed(book, Spent), "one unreadable entry cost the section every other one");
        }

        [TestMethod]
        public void AFileWrittenBeforeTheCooldownsIsReadWithoutThem()
        {
            // Version 8 named no remainders, which is a character who owes nothing: the same state a
            // load produced before this section carried them. Everything else in the body still lands.
            var legacy = JToken.Parse($$"""
                {
                  "currentStance": "Strength",
                  "stances": { "Dexterity": { "slots": ["{{Spent}}", null] } },
                  "sockets": [],
                  "ornaments": []
                }
                """);

            IAbilityBookComponent book = BookHolding((Spent, 0));
            Participant(book).Restore(legacy, savedVersion: 8);

            Assert.AreEqual(0, Owed(book, Spent), "a file that names no remainder handed one out");
            Assert.AreEqual(Stance.Strength, book.CurrentStance, "the rest of the body was lost with the missing property");
            Assert.AreEqual(Spent, book.GetSlotLayout(Stance.Dexterity)[0]?.Id, "the layout was lost with the missing property");
        }

        [TestMethod]
        public void TheRemaindersAreLaidDownAfterTheAugmentsAreOn()
        {
            // Dress first, then hand over the debt. The binder rebuilds what every ability wears from
            // scratch, so a remainder placed before it runs would be measured against an ability that is
            // not the one the player gets back.
            IAbilityBookComponent book = BookHolding((Spent, 0));
            int owedWhenBound = -1;
            var binder = new Mock<IAbilityAugmentBinder>();
            binder.Setup(bind => bind.Bind()).Callback(() => owedWhenBound = Owed(book, Spent));

            var participant = new AbilityBookSaveParticipant(AccessorFor(book), augments: binder.Object);
            participant.Restore(File((Spent, 6)), participant.Version);

            Assert.AreEqual(0, owedWhenBound, "the remainder was already down when the abilities were being dressed");
            Assert.AreEqual(6, Owed(book, Spent), "the remainder never landed at all");
        }

        /// <summary>A section body carrying nothing but those remainders, as text — the shape a save file
        /// actually holds, rather than an object handed straight back to its reader.</summary>
        private static JToken File(params (string Ability, int Owed)[] cooldowns)
        {
            var cooldownEntries = new JObject();
            foreach ((string ability, int owed) in cooldowns) cooldownEntries[ability] = owed;

            return JToken.Parse(new JObject
            {
                ["currentStance"] = Stance.Dexterity.ToString(),
                ["stances"] = new JObject(),
                ["cooldowns"] = cooldownEntries
            }.ToString(Formatting.None));
        }

        private static AbilityBookSaveParticipant Participant(IAbilityBookComponent book) => new(AccessorFor(book));

        private static int Owed(IAbilityBookComponent book, string abilityId) =>
            book.AllAbilities.Single(ability => ability.Id == abilityId).CooldownLeft;

        /// <summary>A book of abilities, each already owing what it is given here.</summary>
        private static IAbilityBookComponent BookHolding(params (string Ability, int Owed)[] abilities)
        {
            AbilityBookComponent book = NewBook();
            foreach ((string abilityId, int owed) in abilities)
                book.Learn(Stance.Dexterity, NewAbility(abilityId, owed));

            return book;
        }

        private static IAbility NewAbility(string abilityId, int owed)
        {
            string instanceId = Guid.NewGuid().ToString();
            var ability = new Mock<IAbility>();
            ability.SetupGet(mock => mock.Id).Returns(abilityId);
            ability.SetupGet(mock => mock.InstanceId).Returns(instanceId);
            ability.SetupProperty(mock => mock.CooldownLeft, owed);
            ability.Setup(mock => mock.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
            return ability.Object;
        }
    }
}
