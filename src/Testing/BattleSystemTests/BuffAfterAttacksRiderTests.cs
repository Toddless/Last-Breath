namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.Riders;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Moq;

    /// <summary>
    /// The tally behind "after N successful attacks, lay the buff" — per-impact arithmetic, the same way
    /// <see cref="AugmentBehaviourTests"/> measures a record's mechanism rather than its construction.
    /// The count runs across casts on purpose: the thresholds the catalog ships (6 and 9) are above the
    /// longest series in the game, so a cast-scoped tally would make both records dead on arrival.
    /// Its one boundary is the battle, because ability instances outlive one.
    /// </summary>
    [TestClass]
    public class BuffAfterAttacksRiderTests
    {
        /// <summary>The shipped record whose threshold is the lower of the two, so a walk reaches it
        /// without drowning the reader in impacts.</summary>
        private const string Record = "Augment_Apply_Buff_Critical_Chance";
        private const string BuffId = "Effect_Critical_Chance_Buff";
        private const string Carrier = "Ability_Series_Of_Attacks";

        /// <summary>What the shipped record asks for. Read off the catalog rather than written here:
        /// the threshold is balance, and a test pinning it would fail on every rebalance.</summary>
        private static int Threshold => (int)Shipped().UpgradeProperties["amountAttacks"];

        [TestMethod]
        public async Task TheBuffArrivesOnTheNthSuccessfulAttackAndOnEveryOneAfterIt()
        {
            (Ability series, ConditionOwner owner, IBattleField field) = Seated();

            for (int attack = 1; attack < Threshold; attack++)
            {
                await series.ApplyImpactRiders(Attack(series, owner, field));
                Assert.AreEqual(0, Stacks(owner), $"the buff arrived on attack {attack}, below the threshold of {Threshold}");
            }

            await series.ApplyImpactRiders(Attack(series, owner, field));
            Assert.AreEqual(1, Stacks(owner), $"the {Threshold}th successful attack did not lay the buff");

            await series.ApplyImpactRiders(Attack(series, owner, field));
            Assert.AreEqual(2, Stacks(owner), "the attack after the threshold laid nothing — the record stops at the Nth instead of running on");
        }

        [TestMethod]
        public async Task TouchesOfAnotherKindAreNotCounted()
        {
            // Asked with the tally one short of the threshold: a counted stranger would tip it over and
            // show as a stack, and nothing else in the walk could produce one.
            (Ability series, ConditionOwner owner, IBattleField field) = Seated();
            await Land(series, owner, field, Threshold - 1);

            foreach (ImpactKind kind in new[]
                     {
                         ImpactKind.Hit, ImpactKind.Projectile, ImpactKind.Splash, ImpactKind.ChainJump,
                         // A swing thrown back at whoever swung last. It is the owner attacking and it
                         // lands, so nothing but the genus keeps it out of a tally bought for a series —
                         // and a series of attacks is what the player aimed, not what he was provoked into.
                         ImpactKind.Reaction
                     })
                await series.ApplyImpactRiders(Impact(series, owner, field, kind));

            Assert.AreEqual(0, Stacks(owner), "a touch that is not an attack was counted towards the threshold");

            await series.ApplyImpactRiders(Attack(series, owner, field));
            Assert.AreEqual(1, Stacks(owner), "the attack that should have completed the tally laid nothing");
        }

        [TestMethod]
        public async Task AnAttackThatDidNotLandIsNotCounted()
        {
            // Same shape, and the reason the rider asks at all: a dodged swing is an impact the delivery
            // still announces, and counting it would sell the buff for attacks that never connected.
            (Ability series, ConditionOwner owner, IBattleField field) = Seated();
            await Land(series, owner, field, Threshold - 1);

            for (int miss = 0; miss < 3; miss++)
                await series.ApplyImpactRiders(
                    new AbilityImpact(owner, owner, field, Succeeded: false) { Source = series, Kind = ImpactKind.Attack });

            Assert.AreEqual(0, Stacks(owner), "an attack that did not land was counted towards the threshold");

            await series.ApplyImpactRiders(Attack(series, owner, field));
            Assert.AreEqual(1, Stacks(owner), "the attack that should have completed the tally laid nothing");
        }

        [TestMethod]
        public async Task TheTallyCarriesFromOneCastToTheNext()
        {
            // The whole point of the change. The longest series in the game is five attacks and the
            // shipped thresholds are six and nine, so a tally that reset per cast could never be paid.
            // Each attack is bracketed by the announcements a real cast opens and closes with, under a
            // fresh cast id — so a reset reintroduced on either of them fails here.
            (Ability series, ConditionOwner owner, IBattleField field) = Seated();

            for (int cast = 0; cast < Threshold; cast++)
            {
                Assert.AreEqual(0, Stacks(owner), $"the buff arrived during cast {cast}, before the tally was paid");

                string castId = Guid.NewGuid().ToString();
                owner.CombatEvents.Publish(new AbilityActivatedEvent(series, owner, VitalsSnapshot.From(owner), castId));
                await series.ApplyImpactRiders(Attack(series, owner, field));
                owner.CombatEvents.Publish(new AbilityExecutedEvent(series, owner, castId));
            }

            Assert.AreEqual(1, Stacks(owner), "one attack per cast never added up — the tally is still being reset between casts");
        }

        [TestMethod]
        public async Task TheTallyStartsOverWhenTheBattleEnds()
        {
            // The one boundary it has. Ability instances live in the book between battles, so a tally
            // carried across would hand the next fight a buff the player did not earn in it.
            (Ability series, ConditionOwner owner, IBattleField field) = Seated();
            await Land(series, owner, field, Threshold - 1);

            owner.CombatEvents.Publish(new BattleEndEvent(BattleResults.PlayerWon));
            owner.Effects.RemoveAllEffects();

            await Land(series, owner, field, Threshold - 1);
            Assert.AreEqual(0, Stacks(owner),
                $"{Threshold - 1} attacks either side of the battle end added up — the tally survived the boundary");

            await series.ApplyImpactRiders(Attack(series, owner, field));
            Assert.AreEqual(1, Stacks(owner), "the fresh tally never reached its own threshold after the battle ended");
        }

        [TestMethod]
        public async Task TheBuffIsLaidWithTheEffectivenessOfTheCastThatCountedIt()
        {
            // The rider reads the SOURCE ability's multiplier instead of leaving the effect at one, so an
            // augment raising the cast's effectiveness is felt by what this record lays. Built directly:
            // the walk needs a cast whose effectiveness is not one, which the shipped series never is.
            var owner = Fighter();
            var effect = new CriticalChanceBuffEffect(duration: 5, maxStacks: 3, 0.15f);
            var rider = new BuffAfterAttacksImpactRider("Rider_Probe", attacksNeeded: 1, effect);
            var source = new Mock<IAbility>();
            source.SetupGet(ability => ability.Effectiveness).Returns(2.5f);

            await rider.Apply(new AbilityImpact(owner, owner, FieldOf(owner), Succeeded: true)
            {
                Source = source.Object,
                Kind = ImpactKind.Attack
            });

            IEffect? laid = owner.Effects.Effects.FirstOrDefault(applied => applied.Id == BuffId);
            Assert.IsNotNull(laid, "the rider laid nothing at all");
            Assert.AreEqual(2.5f, laid.Effectiveness, 0.0001f,
                "the buff went out at the default multiplier — the cast's own effectiveness was dropped on the way");
        }

        [TestMethod]
        public async Task RebuildingTheBuildLeavesNoOrphanedListener()
        {
            // Every rebuild of the binder builds a brand-new rider even for a socket nothing happened to,
            // and this rider listens on its caster for the end of the battle. Without a way to tell the
            // old one to let go, each rebuild left one more dead listener on the fighter, all of them
            // holding riders nothing else pointed at. The contract's detach is what closes it, and the
            // count of live subscriptions is what proves it closed.
            ConditionOwner owner = Fighter();
            (Ability series, IBattleField field) = SeatedOn(owner);
            await series.ApplyImpactRiders(Attack(series, owner, field));

            int afterFirst = owner.ListenersOf<BattleEndEvent>();
            Assert.AreEqual(1, afterFirst, "the rider never subscribed at all, so the rebuilds below prove nothing");

            for (int rebuild = 0; rebuild < 5; rebuild++)
            {
                Reseat(series);
                await series.ApplyImpactRiders(Attack(series, owner, field));
            }

            Assert.AreEqual(afterFirst, owner.ListenersOf<BattleEndEvent>(),
                "five rebuilds left five listeners on the caster — the riders they replaced never let go");

            series.InstallUpgrades(new Dictionary<string, IAugment>());

            Assert.AreEqual(0, owner.ListenersOf<BattleEndEvent>(), "the augment came off the ability and its listener stayed on the fighter");
        }

        /// <summary>The same total rebuild the binder does: everything worn comes off, everything seated
        /// goes back on, and the upgrade is built fresh exactly as <c>AbilityAugmentBinder</c> builds it.</summary>
        private static void Reseat(Ability series)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            IAugment? upgrade = registry.CreateUpgrade(Shipped(catalog));
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{Record}'");
            series.InstallUpgrades(new Dictionary<string, IAugment> { ["socket_0"] = upgrade });
        }

        private static (Ability Series, IBattleField Field) SeatedOn(ConditionOwner owner)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var ability = (Ability)registry.CreateAbility(Carrier);
            IAugment? upgrade = registry.CreateUpgrade(Shipped(catalog));
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{Record}'");

            ability.InstallUpgrades(new Dictionary<string, IAugment> { ["socket_0"] = upgrade });
            ability.SetOwner(owner);

            return (ability, FieldOf(owner));
        }

        private static async Task Land(Ability series, ConditionOwner owner, IBattleField field, int attacks)
        {
            for (int attack = 0; attack < attacks; attack++)
                await series.ApplyImpactRiders(Attack(series, owner, field));
        }

        private static AbilityImpact Attack(IAbility source, IFightable owner, IBattleField field) =>
            Impact(source, owner, field, ImpactKind.Attack);

        private static AbilityImpact Impact(IAbility source, IFightable owner, IBattleField field, ImpactKind kind) =>
            new(owner, owner, field) { Source = source, Kind = kind };

        private static (Ability Series, ConditionOwner Owner, IBattleField Field) Seated()
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var ability = (Ability)registry.CreateAbility(Carrier);
            IAugment? upgrade = registry.CreateUpgrade(Shipped(catalog));
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{Record}'");

            ability.InstallUpgrades(new Dictionary<string, IAugment> { ["socket_0"] = upgrade });
            var owner = Fighter();
            ability.SetOwner(owner);

            return (ability, owner, FieldOf(owner));
        }

        private static AbilityAugmentData Shipped(AbilityAugmentCatalog? catalog = null)
        {
            AbilityAugmentData? record = (catalog ?? ShippedAbilityData.Augments()).Find(Record);
            Assert.IsNotNull(record, $"the shipped data declares no '{Record}'");
            return record;
        }

        private static int Stacks(IFightable bearer) =>
            bearer.Effects.Effects.Count(effect => effect.Id == BuffId);

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, 10000f);
            fighter.SetMaximum(EntityParameter.Mana, 10000f);
            fighter.CurrentHealth = 10000f;
            fighter.CurrentMana = 10000f;

            return fighter;
        }

        private static IBattleField FieldOf(IFightable owner)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner]);

            return field.Object;
        }
    }
}
