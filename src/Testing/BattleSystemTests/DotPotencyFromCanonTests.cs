namespace LastBreathTest.BattleSystemTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Enums;
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using JarOfPoisonCast = Battle.Source.Abilities.JarOfPoison.JarOfPoison;
    using TwinAssistCast = Battle.Source.Abilities.TwinAssist.TwinAssistAttack;

    /// <summary>
    /// What one tick of a cast-laid damaging status carries. Two casts used to build their stack with the
    /// constructor's own fallback share, so the figure they ticked at was written in code and the
    /// canonical row balancing that same status was read by nobody — the file said one thing and the
    /// fight did another, and neither side knew. Both roads go through the registry now: the cast hands
    /// over the one figure it owns (how long the stack lasts) and every other number comes from
    /// <c>SharedData/Effects</c>.
    /// </summary>
    [TestClass]
    public class DotPotencyFromCanonTests
    {
        private const string PoisonId = "Effect_Damage_Over_Turn_Poison";
        private const string BurningId = "Effect_Damage_Over_Turn_Burning";

        /// <summary>The canonical shares, transcribed. This is the one place the numbers may be written
        /// out in code: everywhere else they are read from the file.</summary>
        private const float PoisonPotency = 0.35f;
        private const float BurningPotency = 0.45f;

        /// <summary>The canonical turns of the two rows — NOT what the casts below lay, which is the
        /// point: duration is the caster's figure and the potency is the canon's.</summary>
        private const int CanonicalPoisonTurns = 4;
        private const int CanonicalBurningTurns = 3;

        /// <summary>The canonical ceiling both rows carry, restated by the substitute so it moves the
        /// share alone.</summary>
        private const int CanonicalStacks = 999;

        /// <summary>What the two casts declare for themselves, each the opposite way round from its
        /// canonical row — so a duration taken from the wrong side is visible rather than a coincidence.</summary>
        private const int JarTurns = 3;
        private const int AssistTurns = 4;

        /// <summary>A share no shipped row carries, so a tick standing on it can only have come from the
        /// substitute canon and never from the file or from the constructor's own fallback.</summary>
        private const float Probe = 0.9f;

        private const float Blow = 100f;
        private const float Health = 500f;
        private const float Tolerance = 0.0001f;

        /// <summary>The shipped canon behind a composition, exactly as the game reads it: the registry is
        /// pulled from the composition at the moment a stack is laid, so a walk about it has to BE one.
        /// Composition is idempotent, so every case here shares the one container and the shipped rows are
        /// put back under it each time — a case that moved a figure must not leave it moved.</summary>
        [TestInitialize]
        public void ComposeTheCanon()
        {
            var canon = new EffectProvider();
            ShippedRowsInto(canon);
            GameServiceProvider.Initialize(services => services.AddSingleton<IEffectProvider>(canon));
            ShippedRowsInto(ComposedCanon());

            Assert.IsNotNull(GameServiceProvider.TryGet<IEffectProvider>()?.StackCeilingOf(PoisonId),
                "the composition does not answer for the shipped canon, so nothing below is measuring a canonical figure");
        }

        [TestCleanup]
        public void RestoreTheCanon() => ShippedRowsInto(ComposedCanon());

        [TestMethod]
        public async Task TheJarTicksAtTheShareTheCanonBalancesThePoisonAt()
        {
            var owner = Fighter();
            var target = Fighter();
            var jar = new JarOfPoisonCast(Data("Ability_Jar_Of_Poison"));
            jar.SetOwner(owner);

            await jar.Execute([target], FieldOf(owner, target));

            IDamageOverTurnEffect stack = StackOn(target, PoisonId);
            Assert.AreEqual(Blow * PoisonPotency, stack.DamagePerTick, Tolerance,
                "the jar's poison ticks at a share the canon does not balance it at — the figure is written in code again");
            Assert.AreNotEqual(CanonicalPoisonTurns, JarTurns, "the two turn counts were made equal, so the claim below proves nothing");
            Assert.AreEqual(JarTurns, stack.Duration,
                "the stack lasts the canonical turns instead of the jar's own — duration is the caster's figure");
        }

        [TestMethod]
        public async Task TheAssistBurnsForTheShareTheCanonBalancesTheBurningAt()
        {
            var owner = Fighter();
            var target = Fighter();
            var assist = new TwinAssistCast(Data("Ability_Twin_Assist_Attack"));
            assist.SetOwner(owner);

            await assist.Execute([target], FieldOf(owner, target));

            IDamageOverTurnEffect stack = StackOn(target, BurningId);
            Assert.AreEqual(Blow * BurningPotency, stack.DamagePerTick, Tolerance,
                "the assist's burn ticks at a share the canon does not balance it at — the figure is written in code again");
            Assert.AreNotEqual(CanonicalBurningTurns, AssistTurns, "the two turn counts were made equal, so the claim below proves nothing");
            Assert.AreEqual(AssistTurns, stack.Duration,
                "the stack lasts the canonical turns instead of the assist's own — duration is the caster's figure");
        }

        [TestMethod]
        public async Task MovingTheCanonicalShareMovesTheTickOfBothCasts()
        {
            // The whole point of the change, and the only thing that tells reading the canon apart from
            // happening to agree with it: the figure moves in the file and both casts follow it, with
            // nothing rebuilt and no code naming the number. The canon is substituted here rather than in
            // SharedData — balance is the owner's pass, not a side effect of a walk.
            ComposedCanon().Apply(DataCatalog.Effects, new GameDataFile("substitute-canon.json", SubstituteCanon()));

            var owner = Fighter();
            var poisoned = Fighter();
            var burned = Fighter();
            var jar = new JarOfPoisonCast(Data("Ability_Jar_Of_Poison"));
            var assist = new TwinAssistCast(Data("Ability_Twin_Assist_Attack"));
            jar.SetOwner(owner);
            assist.SetOwner(owner);

            await jar.Execute([poisoned], FieldOf(owner, poisoned));
            await assist.Execute([burned], FieldOf(owner, burned));

            Assert.AreEqual(Blow * Probe, StackOn(poisoned, PoisonId).DamagePerTick, Tolerance,
                "the poison tick stayed where it was after the canonical share moved — the jar reads a figure of its own");
            Assert.AreEqual(Blow * Probe, StackOn(burned, BurningId).DamagePerTick, Tolerance,
                "the burning tick stayed where it was after the canonical share moved — the assist reads a figure of its own");
        }

        [TestMethod]
        public void AStatusTheCanonCarriesNoRowForIsRefusedRatherThanQuietlyBuilt()
        {
            // The edge the registry owns: a typeless damage-over-time has no canonical row and nothing
            // builds it, so the road answers with nothing and the registry names the id in the Tracker.
            // A stack laid at some fallback share instead would be balance nobody wrote and nobody sees.
            Assert.IsNull(DamageOverTurnEffect.FromCanon(JarTurns, StatusEffects.None),
                "a status with no canonical row was built at a default share instead of being refused");
        }

        /// <summary>The composed registry as a reader of data files — the door a substitute canon goes in
        /// through, and the same one the shipped rows are put back through.</summary>
        private static IGameDataParticipant ComposedCanon()
        {
            var canon = GameServiceProvider.TryGet<IEffectProvider>() as IGameDataParticipant;
            Assert.IsNotNull(canon, "the composed effect registry reads no data files, so no canon can be put under it");
            return canon;
        }

        private static void ShippedRowsInto(IGameDataParticipant canon)
        {
            foreach (string file in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Effects), "*.json"))
                canon.Apply(DataCatalog.Effects, new GameDataFile(Path.GetFileName(file), File.ReadAllText(file)));
        }

        /// <summary>The two rows under test with their share moved and nothing else touched. Both carry
        /// every key their factory reads: the registry refuses a half-written row, which is what keeps a
        /// substitute honest.</summary>
        private static string SubstituteCanon() =>
            JsonConvert.SerializeObject(new { effects = new[] { Row(PoisonId, CanonicalPoisonTurns), Row(BurningId, CanonicalBurningTurns) } });

        private static object Row(string id, int duration) =>
            new { id, properties = new { duration, maxStacks = CanonicalStacks, percentFromDamage = Probe } };

        /// <summary>The stack the cast laid, as the thing that ticks.</summary>
        private static IDamageOverTurnEffect StackOn(IFightable victim, string effectId)
        {
            IEffect? laid = victim.Effects.GetBy(effect => effect.IsSame(effectId)).FirstOrDefault();
            Assert.IsNotNull(laid, $"nothing laid '{effectId}' at all, so the tick above measures nothing");

            var ticking = laid as IDamageOverTurnEffect;
            Assert.IsNotNull(ticking, $"'{effectId}' was laid as something that does not tick");
            return ticking;
        }

        /// <summary>A cast whose whole blow is the authored figure: no weapon or spell scales, so the pool
        /// a stack feeds on is exactly <see cref="Blow"/> and the share is the only unknown in the tick.</summary>
        private static AbilityBaseData Data(string id) => new() { Id = id, Damage = Blow };

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Health);
            fighter.SetMaximum(EntityParameter.Mana, Health);
            fighter.CurrentHealth = Health;
            fighter.CurrentMana = Health;

            return fighter;
        }

        private static IBattleField FieldOf(IFightable owner, params IFightable[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns(enemies);
            field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner]);
            field.Setup(battlefield => battlefield.GetAll()).Returns([owner, .. enemies]);
            field.Setup(battlefield => battlefield.GetRandomEntity(It.IsAny<IFightable>())).Returns(owner);

            return field.Object;
        }
    }
}
