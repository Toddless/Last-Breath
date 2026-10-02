namespace LastBreathTest.Ability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Combat;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Moq;

    /// <summary>
    /// The legendary record on the poison coating: a blow lays a poison stack for every living enemy
    /// instead of one. Measured through the shipped registry and a landed blow, because the count is
    /// taken as the blow lands and nothing about it is visible where the record is built.
    /// <para>The field answers each side differently on purpose: WHOSE enemies are counted is half the
    /// mechanism, and a stand where everyone is handed one list would pass a count taken from the
    /// victim's side — which is what the dead implementation this replaced actually did.</para>
    /// </summary>
    [TestClass]
    public class PoisonCoatingStackPerEnemyTests
    {
        private const string Coating = "Ability_Poison_Coating";
        private const string Record = "Augment_Poison_Coating_Apply_Poison_For_Each_Enemy";
        private const string PoisonId = "Effect_Damage_Over_Turn_Poison";

        /// <summary>What a landed swing carries. Named because a stack with nothing to tick with is
        /// never laid at all, and every count below would then be zero for the wrong reason.</summary>
        private static readonly DamageSnapshot Blow = DamageSnapshot.Of(DamageType.Physical, 100f);

        [TestMethod]
        public async Task ABlowLaysAStackForEveryLivingEnemyOnTheField()
        {
            using var rolls = new CombatRandomScope(new SteadyRoll());

            Assert.AreEqual(1, await StacksFromOneBlow(seated: false, [true, true, true]),
                "the plain coating stopped laying its one stack per blow");

            // Three enemies against a lone caster: the two sides are of different sizes, so a count
            // taken from the victim's side instead of the caster's comes out at one here.
            Assert.AreEqual(3, await StacksFromOneBlow(seated: true, [true, true, true]),
                "the record laid something other than a stack per enemy — three stood against the caster");

            Assert.AreEqual(2, await StacksFromOneBlow(seated: true, [true, true]),
                "the count does not follow the field: two enemies bought a number other than two stacks");

            // The coating counts the LIVING. The shipped arena never offers a corpse (it filters the
            // fallen and the fled itself), so what this holds is the coating's own filter — for any
            // other field, and for the finishing blow below, where the freshly killed is still listed.
            Assert.AreEqual(2, await StacksFromOneBlow(seated: true, [true, true, false]),
                "a fallen enemy still paid a stack");
        }

        [TestMethod]
        public async Task TheSideCountedIsTheCastersOwnAndNotTheWholeField()
        {
            using var rolls = new CombatRandomScope(new SteadyRoll());

            // Two against two: the caster stands with an ally, so a count taken off everything present
            // comes out at four while the answer is two.
            Assert.AreEqual(2, await StacksFromOneBlow(seated: true, [true, true], allies: 1),
                "the whole field was counted instead of the caster's enemies");
        }

        [TestMethod]
        public async Task AFinishingBlowCountsTheFieldItIsLeftWith()
        {
            using var rolls = new CombatRandomScope(new SteadyRoll());

            // A blow is announced after the damage is taken, so an enemy the blow killed is already
            // outside the count when the coating asks. Three enemies, the struck one among the fallen:
            // the corpse takes two stacks. Harmless — poison on a corpse never ticks — but it is the
            // arithmetic the record ships with, so it is written down rather than left to be discovered.
            Assert.AreEqual(2, await StacksFromOneBlow(seated: true, [false, true, true]),
                "a finishing blow counted the enemy it had just killed");

            // The last one standing leaves nothing to count, and the coating keeps its floor of one: a
            // blow that landed always poisons what it landed on.
            Assert.AreEqual(1, await StacksFromOneBlow(seated: true, [false]),
                "the blow that emptied the field laid no stack at all");
        }

        [TestMethod]
        public async Task TakingTheRecordOffPutsTheCoatingBackToOneStack()
        {
            using var rolls = new CombatRandomScope(new SteadyRoll());
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            ConditionOwner owner = Fighter();
            ConditionOwner victim = Fighter();

            Ability coating = Seated(registry, catalog, Coating, Record);
            coating.SetOwner(owner);
            await coating.Execute([owner], FieldOf(owner, [victim, Fighter()]));

            Assert.AreEqual(1, owner.ListenersOf<AfterAttackEvent>(),
                "nothing listened for a blow at all, so the rebuilds below prove nothing");

            // The binder rebuilds the whole arrangement on every seating, extraction, load and session
            // reset — fresh copies of the same record over a coating already cast.
            for (int rebuild = 0; rebuild < 5; rebuild++) Reseat(coating, registry, catalog);

            Assert.AreEqual(1, owner.ListenersOf<AfterAttackEvent>(), "five rebuilds left listeners on the caster");

            LandBlowOn(owner, victim);
            Assert.AreEqual(2, Stacks(victim, PoisonId), "the rebuilds cost the standing coating its count of the field");

            // Off the ability entirely. A coating already standing keeps what it was cast with, so what
            // the removal decides is the NEXT cast — asked on a caster who has none.
            coating.InstallUpgrades(new Dictionary<string, IAugment>());
            ConditionOwner second = Fighter();
            ConditionOwner plain = Fighter();
            coating.RemoveOwner();
            coating.SetOwner(second);
            await coating.Execute([second], FieldOf(second, [plain, Fighter()]));
            LandBlowOn(second, plain);

            Assert.AreEqual(1, Stacks(plain, PoisonId), "the record came off the ability and its cast still counted the field");
        }

        /// <summary>One landed blow after the coating was cast on a caster facing <paramref name="enemies"/>
        /// — one flag per enemy, telling the living from the fallen — with <paramref name="allies"/>
        /// standing on his own side. The blow lands on the first enemy, fallen or not.</summary>
        private static async Task<int> StacksFromOneBlow(bool seated, bool[] enemies, int allies = 0)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            ConditionOwner owner = Fighter();
            List<ConditionOwner> side = [.. enemies.Select(alive => Fighter(alive))];
            List<ConditionOwner> friends = [.. Enumerable.Range(0, allies).Select(_ => Fighter())];

            Ability coating = seated
                ? Seated(registry, catalog, Coating, Record)
                : (Ability)registry.CreateAbility(Coating);
            coating.SetOwner(owner);
            await coating.Execute([owner], FieldOf(owner, [.. side], [.. friends]));

            LandBlowOn(owner, side[0]);
            return Stacks(side[0], PoisonId);
        }

        /// <summary>The whole arrangement built again exactly as the binder builds it: everything worn
        /// comes off and a fresh copy of the record goes back on.</summary>
        private static void Reseat(Ability coating, AbilityProvider registry, AbilityAugmentCatalog catalog)
        {
            IAugment? upgrade = registry.CreateUpgrade(Shipped(catalog, Record));
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{Record}'");
            coating.InstallUpgrades(new Dictionary<string, IAugment> { ["socket_0"] = upgrade });
        }

        /// <summary>The ability as the game builds it, wearing the named shipped records.</summary>
        private static Ability Seated(AbilityProvider registry, AbilityAugmentCatalog catalog, string abilityId, params string[] records)
        {
            var ability = (Ability)registry.CreateAbility(abilityId);
            Dictionary<string, IAugment> seated = [];

            for (int slot = 0; slot < records.Length; slot++)
            {
                IAugment? upgrade = registry.CreateUpgrade(Shipped(catalog, records[slot]));
                Assert.IsNotNull(upgrade, $"the registry builds nothing for '{records[slot]}'");
                seated[$"socket_{slot}"] = upgrade;
            }

            ability.InstallUpgrades(seated);
            return ability;
        }

        private static AbilityAugmentData Shipped(AbilityAugmentCatalog catalog, string id)
        {
            AbilityAugmentData? record = catalog.Find(id);
            Assert.IsNotNull(record, $"the shipped data declares no '{id}'");
            return record;
        }

        /// <summary>A landed swing as the fighters announce it — on the attacker's own bus, which is
        /// where the coating listens.</summary>
        private static void LandBlowOn(IFightable attacker, IFightable victim)
        {
            var landed = new Mock<IAttackContext>();
            landed.Setup(context => context.Result).Returns(AttackResults.Succeed);
            landed.Setup(context => context.Target).Returns(victim);
            landed.Setup(context => context.FinalDamage).Returns(Blow);

            attacker.CombatEvents.Publish(new AfterAttackEvent(landed.Object));
        }

        private static int Stacks(IFightable bearer, string effectId) =>
            bearer.Effects.Effects.Count(effect => effect.Id == effectId);

        private static ConditionOwner Fighter(bool alive = true)
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, 10000f);
            fighter.SetMaximum(EntityParameter.Mana, 10000f);
            fighter.CurrentHealth = alive ? 10000f : 0f;
            fighter.CurrentMana = 10000f;

            return fighter;
        }

        /// <summary>A field that answers by SIDE: the caster is told about his enemies, anyone else is
        /// told about the caster's side. Two answers rather than one, so which side a count was taken
        /// from is a thing this stand can tell apart.</summary>
        private static IBattleField FieldOf(IFightable owner, IFightable[] enemies, params IFightable[] allies)
        {
            IFightable[] ownSide = [owner, .. allies];
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(owner)).Returns(enemies);
            field.Setup(battlefield => battlefield.GetEnemies(It.Is<IFightable>(asker => !asker.IsSame(owner.InstanceId))))
                .Returns(ownSide);
            field.Setup(battlefield => battlefield.GetAllies(owner)).Returns(allies);
            field.Setup(battlefield => battlefield.GetAll()).Returns([.. ownSide, .. enemies]);

            return field.Object;
        }

        /// <summary>Every gate open and every count at the top: what is measured is the field, not the dice.</summary>
        private sealed class SteadyRoll : IRandomNumberGenerator
        {
            public float RandFloat() => 0f;

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => max;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }
    }
}
