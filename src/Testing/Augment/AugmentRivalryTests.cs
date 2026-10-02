namespace LastBreathTest.Augment
{
    using System;
    using System.Threading.Tasks;
    using Ability;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Services;
    using Moq;
    using static Ability.AbilityBookStand;
    using static AugmentBench;
    using static AugmentCopies;

    /// <summary>
    /// Two augments that do the same thing do not add up: one of them works, and it is the better one.
    /// Before this, an ability wearing one turn off its wait and two turns off the same wait was wearing
    /// both — the player bought the same improvement twice and was paid for it twice, and which of two
    /// rivals survived where they did collide was settled by whichever socket happened to be read first.
    ///
    /// What makes two of them the same thing is what they DO — the parameter they stand on and the way
    /// they move it (<see cref="AbilityEffectIdentity"/>) — and never how much, because how much is
    /// exactly what tells the winner from the loser. Two records, two copies of one record, a cut of one
    /// turn and a cut of two: all of them are one effect if they reach for one parameter the same way,
    /// and the losers stay in their sockets doing nothing until the winner is pulled.
    ///
    /// The bill an augment sends is the other half of the rule and the one that must NOT collapse: a
    /// record charging fifty more mana for a longer stun and a record charging a share of the price for
    /// a shorter wait are two deals, and waiving either would hand the player a discount for wearing
    /// more surcharges.
    /// </summary>
    [TestClass]
    public class AugmentRivalryTests
    {
        private const string AbilityId = "Ability_Head_Butt";

        /// <summary>Two shipped records that cut the SAME wait, both in whole turns and by different
        /// amounts — the collision the rule is about, in the data as it ships rather than in a pair
        /// invented for the case.</summary>
        private const string CooldownAugment = "Augment_Reduce_Cooldown";
        private const string SurchargeAugment = "Augment_Reduce_Cooldown_Add_Cost";

        private const string CostAugment = "Augment_Reduce_Cost";

        /// <summary>A record whose whole point is a bill on the price of the cast: a longer stun bought
        /// with fifty more mana.</summary>
        private const string StunSurcharge = "Augment_Extend_Stun_Add_Cost";

        /// <summary>The two records that swap the resource a cast is paid in. Neither is more of the
        /// other, so they are the case for a rivalry with no strength to measure.</summary>
        private const string HealthCostAugment = "Augment_Cost_Type_Health";
        private const string BarrierCostAugment = "Augment_Cost_Barrier";

        private const string CostShareProperty = "costShare";

        private const float CooldownTurns = 1f;
        private const float SurchargeTurns = 2f;
        private const float SurchargeCostShare = 0.15f;

        /// <summary>The rolls the two copies of one record come out at. Both are real draws around the
        /// declared three tenths, far enough apart that the cut they buy differs by whole points.</summary>
        private const float LuckyRoll = 0.35f;
        private const float PoorRoll = 0.20f;

        private const string FirstSocket = "socket_tier_one";
        private const string SecondSocket = "socket_tier_two";

        [TestMethod]
        public void TwoRecordsCuttingTheSameWaitLeaveOnlyTheDeeperCut()
        {
            // Nine turns, one off from one record and two off from another. Two cuts of one wait are one
            // improvement bought twice: the deeper of them is what the ability waits.
            var ability = AbilityWith(cooldown: 9);

            ability.InstallUpgrades(InThisOrder(ReduceCooldown(), Surcharge()));

            Assert.AreEqual(7f, ability.Cooldown, "the two cuts were added up instead of the deeper one taking the wait");
        }

        [TestMethod]
        public void TheDeeperCutWinsWhicheverSocketWasFilledFirst()
        {
            // The order two augments are seated in is the order of the sockets they sit in, and a build
            // that depends on it is a build the player cannot read off his own board.
            var seatedShallowFirst = AbilityWith(cooldown: 9);
            var seatedDeepFirst = AbilityWith(cooldown: 9);

            seatedShallowFirst.InstallUpgrades(InThisOrder(ReduceCooldown(), Surcharge()));
            seatedDeepFirst.InstallUpgrades(InThisOrder(Surcharge(), ReduceCooldown()));

            Assert.AreEqual(seatedShallowFirst.Cooldown, seatedDeepFirst.Cooldown,
                "the same two augments came to two different waits depending on which slot was filled first");
            Assert.AreEqual(7f, seatedDeepFirst.Cooldown, "the weaker of the two cuts took the wait when it was seated first");
        }

        [TestMethod]
        public void TwoCopiesOfOneRecordLeaveTheOneThatRolledHigher()
        {
            // The copies are what the player actually holds: one record, drawn twice, two different
            // numbers. Neither the record nor the socket says which of them works — the roll does.
            Assert.AreEqual(97, PriceWornBy(PoorRoll, LuckyRoll), "the copy that rolled lower took the price");
            Assert.AreEqual(97, PriceWornBy(LuckyRoll, PoorRoll), "the same two copies came to two prices in the two orders");
        }

        [TestMethod]
        public void AugmentsReachingForDifferentParametersStillAddUp()
        {
            // The rule is about doing the same thing and nothing else: a cheaper cast and a shorter wait
            // are two improvements, and an ability wears both.
            var ability = AbilityWith(cost: 100, cooldown: 8);

            ability.InstallUpgrades(InThisOrder(ReduceCost(), ReduceCooldown()));

            Assert.AreEqual(70, ability.CostValue, "the cost augment stopped working next to one that never touches the price");
            Assert.AreEqual(7f, ability.Cooldown, "the cooldown augment stopped working next to one that never touches the wait");
        }

        [TestMethod]
        public void TwoAugmentsPullingOneNumberOppositeWaysAreNotOneEffect()
        {
            // Reaching for the same number is not doing the same thing: the book ships a record that
            // makes a berserker burn harder and another that makes him burn less, and one of the two is
            // not a weaker version of the other. Told apart by which way they leave the number, they are
            // two effects and both are worn — read as one, the pair would be silently halved.
            var ability = AbilityWith();

            ability.InstallUpgrades(InThisOrder(MoreAttacks(), FewerAttacks()));

            Assert.AreEqual(3, ability.Attacks, "the two records were read as one effect and one of them was dropped");
        }

        [TestMethod]
        public void EveryRecordStillPaysItsOwnBill()
        {
            // The half that must not collapse. Both records below raise the price of the cast, and the
            // two raises look identical from the parameter's side — but one is what a shorter wait
            // costs and the other is what a longer stun costs, and the player owes both.
            var ability = AbilityWith(cost: 150, cooldown: 9);

            ability.InstallUpgrades(InThisOrder(Surcharge(), StunSurchargeUpgrade()));

            Assert.AreEqual(223, ability.CostValue, "one of the two records had its bill waived by the other");
        }

        [TestMethod]
        public void TheRecordWhoseCutLostStillPaysForIt()
        {
            // Stated on purpose rather than found later: an augment that loses a parameter to a better
            // one is inert on THAT parameter, not switched off. Its own bill is part of the deal its
            // record offers and goes on being charged, so wearing a rival of a surcharge record is a
            // worse build and not a free one.
            var alone = AbilityWith(cost: 150, cooldown: 9);
            var outdone = AbilityWith(cost: 150, cooldown: 9);

            alone.InstallUpgrades(InThisOrder(Surcharge()));
            outdone.InstallUpgrades(InThisOrder(Surcharge(), DeeperCut()));

            Assert.AreEqual(alone.CostValue, outdone.CostValue, "the surcharge record stopped charging once its cut was outdone");
            Assert.AreEqual(4f, outdone.Cooldown, "the deeper cut did not take the wait from the surcharge record");
        }

        [TestMethod]
        public void TwoSharesOfOnePriceComeToTheSameNumberInEitherOrder()
        {
            // A cut and a bill on one price are two different things, so both are read — one after the
            // other, and each of them rounds to whole points. Which is read first therefore shows in the
            // number, and it may not be decided by which socket the player filled first. Fifty-five mana
            // is a base small enough for the two roundings to disagree.
            var cutFirst = AbilityWith(cost: 55, cooldown: 9);
            var billFirst = AbilityWith(cost: 55, cooldown: 9);

            cutFirst.InstallUpgrades(InThisOrder(ReduceCost(), Surcharge()));
            billFirst.InstallUpgrades(InThisOrder(Surcharge(), ReduceCost()));

            Assert.AreEqual(cutFirst.CostValue, billFirst.CostValue,
                "the cut and the bill were read in the order the sockets were filled, and the build came to two prices");
        }

        [TestMethod]
        public void TheResourceSwapIsSettledTheSameWayInEitherOrder()
        {
            // Two records that replace the resource a cast is paid in. Nothing is more of health than it
            // is of barrier, so there is no strength to measure — what must not depend on anything is
            // that it comes out the same whichever slot was filled first.
            var healthFirst = AbilityWith();
            var barrierFirst = AbilityWith();

            healthFirst.InstallUpgrades(InThisOrder(SwapTo(HealthCostAugment, Costs.Health, tier: 2), SwapTo(BarrierCostAugment, Costs.Barrier, tier: 3)));
            barrierFirst.InstallUpgrades(InThisOrder(SwapTo(BarrierCostAugment, Costs.Barrier, tier: 3), SwapTo(HealthCostAugment, Costs.Health, tier: 2)));

            Assert.AreEqual(healthFirst.CostType, barrierFirst.CostType,
                "the ability is paid for out of two different pools depending on which swap was seated first");
        }

        [TestMethod]
        public void TheDeeperTierTakesASwapThatCannotBeWeighed()
        {
            // What settles a swap now that it is settled by something the player can see. A replacement
            // has no distance from the base to measure, and the id it happens to sort under says nothing
            // to anyone; the tier says what the record cost him, and the deeper tier takes the swap.
            // The ids below sort AGAINST the answer on purpose — under the old rule the tier-one record
            // would take the cast, and a tier-three purchase would sit there doing nothing.
            var deepSecond = AbilityWith();
            var deepFirst = AbilityWith();

            deepSecond.InstallUpgrades(InThisOrder(SwapTo("Augment_A_Shallow", Costs.Health, tier: 1), SwapTo("Augment_Z_Deep", Costs.Barrier, tier: 3)));
            deepFirst.InstallUpgrades(InThisOrder(SwapTo("Augment_Z_Deep", Costs.Barrier, tier: 3), SwapTo("Augment_A_Shallow", Costs.Health, tier: 1)));

            Assert.AreEqual(Costs.Barrier, deepSecond.CostType, "the shallower record's swap took a cast a tier-three record was paid to swap");
            Assert.AreEqual(deepSecond.CostType, deepFirst.CostType, "the same pair came to two answers in the two orders");
        }

        [TestMethod]
        public void TheAbilitySaysWhichSocketIsAsleep()
        {
            // The data behind the mark on a cell. Two records cutting one wait: the deeper cut works and
            // the shallower one sits there, and the ability is the only thing that knows which is which —
            // both are seated, both are saved, both are drawn, and nothing about the socket says a word.
            var ability = AbilityWith(cost: 150, cooldown: 9);

            ability.InstallUpgrades(InThisOrder(Surcharge(), DeeperCut()));

            Assert.AreEqual(AugmentActivity.Dormant, ability.ActivityOf("socket_0"),
                "the record whose only cut was outdone reads as working");
            Assert.AreEqual(AugmentActivity.Working, ability.ActivityOf("socket_1"), "the record taking the wait reads as asleep");
            Assert.AreEqual(AugmentActivity.Working, ability.ActivityOf("socket_9"), "an address holding nothing reported a sleeper");
        }

        [TestMethod]
        public void ARecordAsleepOnOneNumberIsOnlyPartlyAsleep()
        {
            // The middle answer, and why the enum has three. A record that moves two numbers and loses
            // one of them is not dormant: the other half is still being paid out, and a mark saying
            // otherwise would tell the player to pull something that is working for him.
            var ability = AbilityWith(cost: 150, cooldown: 9);

            ability.InstallUpgrades(InThisOrder(CutAndCheapen(), DeeperCut()));

            Assert.AreEqual(AugmentActivity.Partly, ability.ActivityOf("socket_0"),
                "a record that lost one of its two moves reads as all or nothing");
        }

        [TestMethod]
        public void TheBillOfASleepingRecordIsNoPartOfWhetherItIsAwake()
        {
            // The other half of "the loser still pays". A surcharge record goes on charging when its
            // gift has lost, and counting that bill as a move that still runs would report it half-awake
            // — hiding the bad deal the mark exists to show. The price is still charged; see
            // TheRecordWhoseCutLostStillPaysForIt for that half.
            var ability = AbilityWith(cost: 150, cooldown: 9);

            ability.InstallUpgrades(InThisOrder(Surcharge(), DeeperCut()));

            // A hundred and fifty and the surcharge record's share of it, rounded: the bill is unchanged
            // by the cut having lost.
            Assert.AreEqual(173, ability.CostValue, "the sleeping record stopped charging for the cut it no longer makes");
            Assert.AreEqual(AugmentActivity.Dormant, ability.ActivityOf("socket_0"),
                "the bill it goes on charging was counted as one of its moves still working");
        }

        [TestMethod]
        public void ARecordWhoseRiderStillFiresIsNotAsleep()
        {
            // A rider has no rival: nothing on the ability can put one out of work, so it is running for
            // as long as it is seated (owner's word, 2026-08-19). A record that rides AND moves a number
            // therefore never sleeps outright — losing the number leaves it half-awake, and a mark saying
            // "dormant" would tell the player to pull something that is still doing its main job.
            var ability = AbilityWith(cooldown: 9);

            ability.InstallUpgrades(InThisOrder(CutAndRide(), DeeperCut()));

            Assert.AreEqual(4f, ability.Cooldown, "the deeper cut did not take the wait, so the loser below is not a loser");
            Assert.AreEqual(AugmentActivity.Partly, ability.ActivityOf("socket_0"),
                "the record's cut lost and its rider was buried with it");
        }

        [TestMethod]
        public void ARecordThatIsNothingButARiderAlwaysWorks()
        {
            // The other end of the same rule. Such a record moves no number at all, so there is nothing
            // it could ever lose — and reporting it by its numbers alone would have it answer for a
            // ledger it never wrote in.
            var ability = AbilityWith(cooldown: 9);

            ability.InstallUpgrades(InThisOrder(JustARider(), DeeperCut()));

            Assert.AreEqual(AugmentActivity.Working, ability.ActivityOf("socket_0"),
                "a record that only rides was judged on numbers it never moved");
        }

        [TestMethod]
        public void TwoCopiesOfOneRecordAnswerAsOne()
        {
            // Stated so it is a law and not a surprise. A move is filed under the RECORD that laid it —
            // it has to be, because a price belongs to the deal one record offers and two copies of one
            // record are one deal. So the two sockets share one answer, and it is the answer for the
            // pair: one of the two cuts is running.
            var ability = AbilityWith(cost: 150);
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            ability.InstallUpgrades(InThisOrder(Built(registry, catalog, PoorRoll), Built(registry, catalog, LuckyRoll)));

            Assert.AreEqual(AugmentActivity.Partly, ability.ActivityOf("socket_0"), "the pair of copies reported something other than half-working");
            Assert.AreEqual(ability.ActivityOf("socket_0"), ability.ActivityOf("socket_1"),
                "two copies of one record came to two different answers, which the record they share cannot support");
        }

        [TestMethod]
        public void TheLoserStaysInItsSocketAndTakesOverWhenTheWinnerIsPulled()
        {
            // The whole arrangement, through the board and the binder the game runs. The weaker copy is
            // seated, judged, saved and displayed exactly like the stronger one — it simply does nothing
            // while the better one is worn. Pulling the winner is all it takes to bring it back.
            var build = new Build();
            int bare = build.Ability.CostValue;

            build.Seat(FirstSocket, CostCopy(PoorRoll));
            int alone = build.Ability.CostValue;
            build.Seat(SecondSocket, CostCopy(LuckyRoll));

            Assert.AreEqual(97, build.Ability.CostValue, "the deeper cut did not take the price off the shallower one");
            Assert.AreEqual(2, build.Board.Occupants.Count, "the augment that lost the price was thrown out of its socket");

            build.Pull(SecondSocket);

            Assert.AreNotEqual(bare, alone, "the first copy did nothing on its own, so its revival below proves nothing");
            Assert.AreEqual(alone, build.Ability.CostValue, "the copy that lost never started working again once the winner was pulled");
        }

        /// <summary>What the ability charges wearing two copies of the cost record, seated in the order
        /// named. The upgrades are built by the registry out of each copy's own numbers, which is the
        /// road a seated augment actually travels.</summary>
        private static int PriceWornBy(float first, float second)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var ability = AbilityWith(cost: 150);

            ability.InstallUpgrades(InThisOrder(Built(registry, catalog, first), Built(registry, catalog, second)));

            return ability.CostValue;
        }

        /// <summary>One copy of the cost record as the registry builds it — the record with this copy's
        /// roll written over the declared number.</summary>
        private static IAugment Built(AbilityProvider registry, AbilityAugmentCatalog catalog, float roll)
        {
            AbilityAugmentData? record = catalog.Find(CostAugment);
            Assert.IsNotNull(record, $"the shipped data declares no '{CostAugment}'");

            IAugment? upgrade = registry.CreateUpgrade(CostCopy(roll).Applied(record));
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{CostAugment}'");

            return upgrade;
        }

        private static AugmentInstance CostCopy(float roll) => Copy(CostAugment, (CostShareProperty, roll));

        private static IAugment ReduceCooldown() => new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns);

        private static IAugment ReduceCost() => new AugmentReduceCost(CostAugment, [], 1, 0.30f);

        private static IAugment Surcharge() =>
            new AugmentReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeTurns, SurchargeCostShare);

        /// <summary>A cut deeper than the surcharge record's, standing on the same wait: the rival that
        /// puts the surcharge record's own cut out of work.</summary>
        private static IAugment DeeperCut() =>
            new AugmentParameterSet("Augment_Deeper_Cut", [], 3, [(AbilityParameter.Cooldown, OperationType.Subtract, 5f)]);

        /// <summary>Two records reaching for one number from opposite sides, shaped like the pair the
        /// book ships on a berserker's own burn (Augment_Fury_More_Burn and Augment_Fury_Less_Burn). Stood in for
        /// on a key of Head Butt's, because which number it is has nothing to do with the question.</summary>
        private static IAugment MoreAttacks() =>
            new AugmentParameterSet("Augment_More_Attacks", [], 2, [(AbilityParameter.Attacks, OperationType.Add, 2f)]);

        private static IAugment FewerAttacks() =>
            new AugmentParameterSet("Augment_Fewer_Attacks", [], 2, [(AbilityParameter.Attacks, OperationType.Subtract, 1f)]);

        /// <summary>Head Butt's own tier-two augment: a longer stun bought with fifty more mana.</summary>
        private static IAugment StunSurchargeUpgrade() =>
            new AugmentParameterSet(StunSurcharge, [], 2, [(AbilityParameter.CostValue, OperationType.Add, 50f)]);

        /// <summary>A record that rides every impact AND cuts the wait — the shape the shipped book puts
        /// on an applier that also carries a number of its own. Its cut is outdone below.</summary>
        private static IAugment CutAndRide() => new RidingCut("Augment_Riding_Cut", tier: 2, turns: 1f);

        /// <summary>A record that is nothing but a rider: no number of its own anywhere.</summary>
        private static IAugment JustARider() =>
            new AugmentImpactRider("Augment_Just_A_Rider", [], 2, () => new ProbeRider());

        /// <summary>A record that moves two numbers, one of which is outdone below: the case for an
        /// augment that is neither working nor asleep.</summary>
        private static IAugment CutAndCheapen() =>
            new AugmentParameterSet("Augment_Cut_And_Cheapen", [], 2,
                [(AbilityParameter.Cooldown, OperationType.Subtract, 1f), (AbilityParameter.CostValue, OperationType.Subtract, 20f)]);

        private static IAugment SwapTo(string augmentId, Costs resource, int tier = 2) =>
            new AugmentCostTypeOverride(augmentId, [], tier, resource);

        /// <summary>An impact rider that does nothing: what these walks need of one is that it EXISTS on
        /// the ability, because existing is the whole of what makes a rider a working move.</summary>
        private sealed class ProbeRider : IImpactRider
        {
            public string Id => "Rider_Probe";

            public string InstanceId { get; } = Guid.NewGuid().ToString();

            public bool IsSame(string otherId) => Id.Equals(otherId, StringComparison.Ordinal);

            public Task Apply(AbilityImpact impact) => Task.CompletedTask;
        }

        /// <summary>An augment that both rides and moves a number — the two halves whose verdicts have to
        /// be added up rather than one of them answering for the record.</summary>
        private sealed class RidingCut(string id, int tier, float turns)
            : AugmentImpactRider(id, [], tier, () => new ProbeRider())
        {
            public override void ApplyUpgrade(Ability ability)
            {
                base.ApplyUpgrade(ability);
                ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                    AbilityParameter.Cooldown, Priority.Weak, OperationType.Subtract, turns, DecoratorId, Id, rank: Tier));
            }

            public override void RemoveUpgrade(Ability ability)
            {
                base.RemoveUpgrade(ability);
                ability.RemoveParameterDecorator(DecoratorId, AbilityParameter.Cooldown);
            }

            public override IAugment Copy() => new RidingCut(Id, Tier, turns);

            private string DecoratorId => $"Ability_Parameter_Decorator_{Id}_{AbilityParameter.Cooldown}";
        }

        /// <summary>
        /// One character wearing the real thing: the shipped records, a board with two slots on one
        /// ability and the binder the game dresses a book with. A stand-in for any of the three would
        /// leave the claim standing on the stand-in — what is walked here is that an augment which lost
        /// its parameter is still an occupant of its socket, which only the board can answer for.
        /// </summary>
        private sealed class Build
        {
            private readonly IPlayerAccessor _players;
            private readonly IAbilityAugmentBinder _binder;

            internal Build()
            {
                (AbilityProvider abilities, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
                var book = new AbilityBookComponent(Fighter());
                book.Learn(abilities.GetAbilityStance(AbilityId), abilities.CreateAbility(AbilityId));

                _players = AccessorFor(book);
                Board = new AbilitySocketBoard(catalog);
                Board.Sync(
                [
                    new AbilitySocketPlacement(FirstSocket, AbilityId, 1),
                    new AbilitySocketPlacement(SecondSocket, AbilityId, 2)
                ]);
                _binder = new AbilityAugmentBinder(_players, Board, abilities);
            }

            internal AbilitySocketBoard Board { get; }

            internal IAbility Ability
            {
                get
                {
                    IAbility? learned = _players.Player?.AbilityBook.AllAbilities.FirstOrDefault(ability => ability.Id == AbilityId);
                    Assert.IsNotNull(learned, $"the character never learned '{AbilityId}'");
                    return learned;
                }
            }

            internal void Seat(string socketId, AugmentInstance copy)
            {
                Assert.IsTrue(Board.Install(Board.At(socketId), copy), $"the copy never reached '{socketId}'");
                _binder.Bind();
            }

            internal void Pull(string socketId)
            {
                Assert.IsNotNull(Board.Extract(Board.At(socketId)), $"'{socketId}' held nothing to pull");
                _binder.Bind();
            }

            /// <summary>An owner real enough to be handed an ability: learning one hands the ability its
            /// owner, and the ability subscribes to that owner's combat bus on the spot.</summary>
            private static IFightable Fighter()
            {
                var owner = new Mock<IFightable>();
                owner.SetupGet(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                return owner.Object;
            }
        }
    }
}
