namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Services;
    using Moq;
    using static AbilityBookStand;
    using static AugmentBench;
    using static AugmentCopies;

    /// <summary>
    /// Two augments that do the same thing do not add up: one of them works, and it is the better one.
    /// Before this, an ability wearing a quarter off its wait and two fifths off the same wait was
    /// wearing both — the player bought the same improvement twice and was paid for it twice, and which
    /// of two rivals survived where they did collide was settled by whichever socket happened to be read
    /// first.
    ///
    /// What makes two of them the same thing is what they DO — the parameter they stand on and the way
    /// they move it (<see cref="AbilityEffectIdentity"/>) — and never how much, because how much is
    /// exactly what tells the winner from the loser. Two records, two copies of one record, a flat cut
    /// and a cut stated as a share: all of them are one effect if they reach for one parameter the same
    /// way, and the losers stay in their sockets doing nothing until the winner is pulled.
    ///
    /// The bill an augment sends is the other half of the rule and the one that must NOT collapse: a
    /// record charging fifty more mana for a longer stun and a record charging four tenths of the price
    /// for a shorter wait are two deals, and waiving either would hand the player a discount for wearing
    /// more surcharges.
    /// </summary>
    [TestClass]
    public class AugmentRivalryTests
    {
        private const string AbilityId = "Ability_Head_Butt";

        /// <summary>Two shipped records that cut the SAME wait — the collision the rule is about, in the
        /// data as it ships rather than in a pair invented for the case.</summary>
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

        private const float CooldownShare = 0.25f;
        private const float SurchargeShare = 0.40f;

        /// <summary>The rolls the two copies of one record come out at. Both are real draws around the
        /// declared three tenths, far enough apart that the cut they buy differs by whole points.</summary>
        private const float LuckyRoll = 0.35f;
        private const float PoorRoll = 0.20f;

        private const string FirstSocket = "socket_tier_one";
        private const string SecondSocket = "socket_tier_two";

        [TestMethod]
        public void TwoRecordsCuttingTheSameWaitLeaveOnlyTheDeeperCut()
        {
            // Nine turns, a quarter off from one record and two fifths off from another. Two cuts of one
            // wait are one improvement bought twice: the deeper of them is what the ability waits.
            var ability = AbilityWith(cooldown: 9);

            ability.InstallUpgrades(InThisOrder(ReduceCooldown(), Surcharge()));

            Assert.AreEqual(5f, ability.Cooldown, "the two cuts were added up instead of the deeper one taking the wait");
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
            Assert.AreEqual(5f, seatedDeepFirst.Cooldown, "the weaker of the two cuts took the wait when it was seated first");
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
            Assert.AreEqual(6f, ability.Cooldown, "the cooldown augment stopped working next to one that never touches the wait");
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

            Assert.AreEqual(260, ability.CostValue, "one of the two records had its bill waived by the other");
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
            // is of barrier, so which of them wins is arbitrary — what is not arbitrary is that it is
            // the same one whichever slot was filled first.
            var healthFirst = AbilityWith();
            var barrierFirst = AbilityWith();

            healthFirst.InstallUpgrades(InThisOrder(SwapTo(HealthCostAugment, Costs.Health), SwapTo(BarrierCostAugment, Costs.Barrier)));
            barrierFirst.InstallUpgrades(InThisOrder(SwapTo(BarrierCostAugment, Costs.Barrier), SwapTo(HealthCostAugment, Costs.Health)));

            Assert.AreEqual(healthFirst.CostType, barrierFirst.CostType,
                "the ability is paid for out of two different pools depending on which swap was seated first");
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
        private static IAbilityUpgrade Built(AbilityProvider registry, AbilityAugmentCatalog catalog, float roll)
        {
            AbilityUpgradeData? record = catalog.Find(CostAugment);
            Assert.IsNotNull(record, $"the shipped data declares no '{CostAugment}'");

            IAbilityUpgrade? upgrade = registry.CreateUpgrade(CostCopy(roll).Applied(record));
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{CostAugment}'");

            return upgrade;
        }

        private static AugmentInstance CostCopy(float roll) => Copy(CostAugment, (CostShareProperty, roll));

        private static IAbilityUpgrade ReduceCooldown() => new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare);

        private static IAbilityUpgrade ReduceCost() => new AbilityUpgradeReduceCost(CostAugment, [], 1, 0.30f);

        private static IAbilityUpgrade Surcharge() =>
            new AbilityUpgradeReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeShare, SurchargeShare);

        /// <summary>A cut deeper than the surcharge record's, standing on the same wait: the rival that
        /// puts the surcharge record's own cut out of work.</summary>
        private static IAbilityUpgrade DeeperCut() =>
            new AbilityUpgradeParameterSet("Augment_Deeper_Cut", [], 3, [(AbilityParameter.Cooldown, OperationType.Subtract, 5f)]);

        /// <summary>Two records reaching for one number from opposite sides, shaped like the pair the
        /// book ships on a berserker's own burn (Augment_More_Burn and Augment_Less_Burn). Stood in for
        /// on a key of Head Butt's, because which number it is has nothing to do with the question.</summary>
        private static IAbilityUpgrade MoreAttacks() =>
            new AbilityUpgradeParameterSet("Augment_More_Attacks", [], 2, [(AbilityParameter.Attacks, OperationType.Add, 2f)]);

        private static IAbilityUpgrade FewerAttacks() =>
            new AbilityUpgradeParameterSet("Augment_Fewer_Attacks", [], 2, [(AbilityParameter.Attacks, OperationType.Subtract, 1f)]);

        /// <summary>Head Butt's own tier-two augment: a longer stun bought with fifty more mana.</summary>
        private static IAbilityUpgrade StunSurchargeUpgrade() =>
            new AbilityUpgradeParameterSet(StunSurcharge, [], 2, [(AbilityParameter.CostValue, OperationType.Add, 50f)]);

        private static IAbilityUpgrade SwapTo(string augmentId, Costs resource) =>
            new AbilityUpgradeCostTypeOverride(augmentId, [], 2, resource);

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
