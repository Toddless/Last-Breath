namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.Riders;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;
    using Newtonsoft.Json.Linq;
    using ArmageddonCast = Battle.Source.Abilities.Armageddon.Armageddon;

    /// <summary>
    /// The figures a stage adds and the figures an augment is worth, asked where they come from. Each of
    /// them used to be a literal inside the delivery — a stage that lengthened a stun "by one", a chain
    /// that jumped "one more time", a series clamped between two constants, an augment that hit "twice as
    /// hard" — so the record could say nothing about them and a balance pass had nowhere to write.
    ///
    /// What is measured is not the figure alone but the ROAD: the shipped number is read at the far end of
    /// a real cast, and the same cast is run again over a catalog that names a different one. The doctored
    /// catalog lives in memory and the file on disk is never touched — moving the record has to move the
    /// cast, and that is the whole claim.
    /// </summary>
    [TestClass]
    public class StageAndAugmentNumbersTests
    {
        private const string IceBlockId = "Ability_Ice_Block";
        private const string ChainId = "Ability_Chain_Lightning";
        private const string BerserkId = "Ability_Berserk_Fury";
        private const string ArmageddonId = "Ability_Armageddon";
        private const string DeepFreezeId = "Ability_Deep_Freeze";
        private const string JarId = "Ability_Jar_Of_Poison";
        private const string SeriesId = "Ability_Series_Of_Attacks";

        private const string ConsumeStunRecord = "Augment_Ice_Block_Consume_Stun_Deal_Double_Damage";
        private const string PoisonSeriesRecord = "Augment_Poison_Attack_Series";
        private const string LeachRecord = "Augment_Leach_On_Crit";

        private const string PoisonId = "Effect_Damage_Over_Turn_Poison";
        private const string StunId = "Effect_Stun";
        private const string ShredId = "Effect_Cold_Resistance_Shred";
        private const string HealReductionId = "Effect_Heal_Reduction";
        private const string LeechId = "Effect_Crit_Leech";

        /// <summary>What the shipped records name, transcribed rather than read back out of the files the
        /// walks are about — a walk reading its own subject agrees with whatever that subject became.</summary>
        private const int IceBlockStun = 1;
        private const int IceBlockStageTwoBonus = 1;
        private const float ConsumeStunMultiplier = 2f;
        private const int ChainJumps = 2;
        private const int ChainStageTwoBonus = 1;
        private const float MaxContinueChance = 0.80f;
        private const float MissingHpStep = 5f;
        private const int ShredStacks = 1;
        private const int HealReductionStacks = 2;
        private const int JarPoisonTurns = 3;
        private const float PoisonPotency = 0.7f;
        private const int LeachTurns = 3;
        private const int LeachStacks = 1;

        /// <summary>Draws the stage roll is judged on. The roll walks the stances' rows top-down (5% for
        /// stage 4, 25% for stage 3, 50% for stage 2) and takes the first it beats, so one fixed draw
        /// picks the stage: above every row is the base stage, and between two rows is the one below.</summary>
        private const float StageTwoDraw = 0.3f;
        private const float StageThreeDraw = 0.1f;
        private const float NoStageDraw = 1f;

        private const float Health = 100000f;
        private const float WeaponDamage = 200f;
        private const float SpellDamage = 400f;
        private const float Tolerance = 0.01f;

        [TestMethod]
        public async Task TheIceBlockLaysTheStunItsRecordNamesAndTheStageAddsWhatTheRecordNames()
        {
            // Both halves in one walk, because the bonus can only be read as the difference between them:
            // a stage that added nothing and a base stun of two would print the same number.
            int baseStage = await BlockStun(Shipped(), NoStageDraw);
            int stageTwo = await BlockStun(Shipped(), StageTwoDraw);

            Assert.AreEqual(IceBlockStun, baseStage, "the plain Ice Block no longer stuns for the turns its record names");
            Assert.AreEqual(IceBlockStun + IceBlockStageTwoBonus, stageTwo,
                "the Ice Block's stage two no longer lengthens the stun by what its record names");
        }

        [TestMethod]
        public async Task ARecordNamingAnotherStageTwoBonusMovesTheStunTheIceBlockLays()
        {
            const int Doctored = 4;

            int moved = await BlockStun(AbilitiesWith(IceBlockId, "stageTwoStunBonus", Doctored), StageTwoDraw);

            Assert.AreEqual(IceBlockStun + Doctored, moved,
                "the record names another stage-two bonus and the stun the cast lays did not follow it");
        }

        [TestMethod]
        public async Task TheStunEatingRecordHitsForTheMultipleItNames()
        {
            // The augment is the only thing that eats a standing stun, so the bare cast is the plain blow
            // and the worn one is that same blow times what the record is worth.
            float plain = await StunEatingBlow(wearing: null);
            float eaten = await StunEatingBlow(wearing: ShippedRecord(ConsumeStunRecord));

            Assert.IsTrue(plain > 0, "the bare block dealt nothing at all, so the multiple below proves nothing");
            Assert.AreEqual(plain * ConsumeStunMultiplier, eaten, Tolerance,
                "the stun-eating record no longer hits for the multiple its record names");
        }

        [TestMethod]
        public async Task ARecordNamingAnotherMultipleMovesWhatTheStunEatingBlockHitsFor()
        {
            const float Doctored = 3f;
            AbilityAugmentData record = ShippedRecord(ConsumeStunRecord);

            float plain = await StunEatingBlow(wearing: null);
            float eaten = await StunEatingBlow(wearing: record with
            {
                UpgradeProperties = new Dictionary<string, float>(StringComparer.Ordinal) { ["damageMultiplier"] = Doctored }
            });

            Assert.AreEqual(plain * Doctored, eaten, Tolerance,
                "the record names another multiple and the blow that ate the stun did not follow it");
        }

        [TestMethod]
        public async Task TheChainStageTwoAddsTheJumpItsRecordNames()
        {
            int baseStage = await ChainStrikes(Shipped(), NoStageDraw);
            int stageTwo = await ChainStrikes(Shipped(), StageTwoDraw);

            Assert.AreEqual(1 + ChainJumps, baseStage, "the plain chain no longer strikes once and jumps what its record names");
            Assert.AreEqual(1 + ChainJumps + ChainStageTwoBonus, stageTwo,
                "the chain's stage two no longer adds the jump its record names");
        }

        [TestMethod]
        public async Task ARecordNamingAnotherStageTwoBonusMovesTheChainsJumps()
        {
            const int Doctored = 3;

            int moved = await ChainStrikes(AbilitiesWith(ChainId, "stageTwoJumpBonus", Doctored), StageTwoDraw);

            Assert.AreEqual(1 + ChainJumps + Doctored, moved,
                "the record names another stage-two bonus and the chain's jumps did not follow it");
        }

        [TestMethod]
        public async Task TheBerserkSeriesIsCappedByTheChanceItsRecordNames()
        {
            // A berserk at full health would carry on for as long as his health lasts if nothing capped
            // the chance. The ceiling is bracketed by two draws sitting either side of it — that is what
            // names the shipped figure rather than merely showing the key is read — and a record naming a
            // higher one carries the series past the draw the shipped ceiling stopped.
            const float Over = 0.85f;
            const float Under = 0.75f;
            const float Doctored = 0.95f;

            int stopped = await Swings(Shipped(), Health, Over);
            int carried = await Swings(Shipped(), Health, Under);
            int raised = await Swings(AbilitiesWith(BerserkId, "maxContinueChance", Doctored), Health, Over);

            Assert.IsTrue(Over > MaxContinueChance && Under < MaxContinueChance,
                "the two draws no longer sit either side of the shipped ceiling, so the claims below are vacuous");
            Assert.AreEqual(1, stopped, $"a draw of {Over} beat the ceiling of {MaxContinueChance} — the cap sits lower than the record names");
            Assert.AreEqual(2, carried, $"a draw of {Under} lost to the ceiling of {MaxContinueChance} — the cap sits higher than the record names");
            Assert.AreEqual(2, raised, "the record names a higher ceiling and the series did not carry on past the same draw");
        }

        [TestMethod]
        public async Task TheBerserkSeriesFloorIsTheChanceItsRecordNames()
        {
            // The other end. At half health the share itself decides, so the shipped floor changes
            // nothing; a record naming a floor ABOVE that share holds the series up, which is the only
            // thing a floor ever does.
            const float Draw = 0.6f;
            const float Doctored = 0.7f;

            int shipped = await Swings(Shipped(), Health / 2f, Draw);
            int floored = await Swings(AbilitiesWith(BerserkId, "minContinueChance", Doctored), Health / 2f, Draw);

            Assert.AreEqual(1, shipped, "the half-health share already beat the draw, so the floor below proves nothing");
            Assert.AreEqual(2, floored, "the record names a floor above the share and the series still ended on it");
        }

        [TestMethod]
        public async Task TheArmageddonCountsMissingHealthInTheStepItsRecordNames()
        {
            // The rate is nothing in the shipped record, so the step is only reachable through a decorator
            // on the rate — which is what an augment selling "damage per missing health" would be.
            const float Missing = 1000f;
            const float Rate = 2f;
            const float DoctoredStep = 10f;

            float shipped = await MissingHealthBlow(Shipped(), Missing, Rate);
            float halved = await MissingHealthBlow(AbilitiesWith(ArmageddonId, "missingHpStep", DoctoredStep), Missing, Rate);
            float plain = await MissingHealthBlow(Shipped(), Missing, rate: 0f);

            Assert.AreEqual(plain + (Missing / MissingHpStep * Rate), shipped, Tolerance,
                "the Armageddon no longer counts missing health in the step its record names");
            Assert.AreEqual(plain + (Missing / DoctoredStep * Rate), halved, Tolerance,
                "the record names another step and the blow did not follow it");
        }

        [TestMethod]
        public async Task TheDeepFreezeLaysItsPayloadAtTheStackCountsItsRecordNames()
        {
            List<IEffect> shipped = await FreezePayload(Shipped());

            Assert.AreEqual(ShredStacks, StackCeiling(shipped, ShredId),
                "the Deep Freeze no longer shreds for the stacks its record names");
            Assert.AreEqual(HealReductionStacks, StackCeiling(shipped, HealReductionId),
                "the Deep Freeze no longer cuts healing for the stacks its record names");
        }

        [TestMethod]
        public async Task ARecordNamingOtherStackCountsMovesWhatTheDeepFreezeLays()
        {
            const int DoctoredShred = 3;
            const int DoctoredHealReduction = 1;

            AbilityProvider doctored = AbilitiesWith(DeepFreezeId, new Dictionary<string, float>(StringComparer.Ordinal)
            {
                ["shredStacks"] = DoctoredShred,
                ["healReductionStacks"] = DoctoredHealReduction
            });
            List<IEffect> laid = await FreezePayload(doctored);

            Assert.AreEqual(DoctoredShred, StackCeiling(laid, ShredId),
                "the record names other shred stacks and the payload did not follow it");
            Assert.AreEqual(DoctoredHealReduction, StackCeiling(laid, HealReductionId),
                "the record names other heal-reduction stacks and the payload did not follow it");
        }

        [TestMethod]
        public async Task TheJarPoisonsForTheTurnsItsRecordNames()
        {
            const int Doctored = 5;

            int shipped = await JarPoisonTurnsOf(Shipped());
            int moved = await JarPoisonTurnsOf(AbilitiesWith(JarId, "poisonDuration", Doctored));

            Assert.AreEqual(JarPoisonTurns, shipped, "the jar no longer poisons for the turns its record names");
            Assert.AreEqual(Doctored, moved, "the record names other turns and the jar's poison did not follow it");
        }

        [TestMethod]
        public void ThePoisonSeriesRecordLendsThePotencyItNames()
        {
            // The potency is not held by the rider: the record LENDS it to the ability as a parameter, and
            // the rider reads it back off the ability at the moment it lays a stack. So what the key is
            // worth on the seated ability is what every stack of that series ticks at.
            const float Doctored = 0.25f;
            AbilityAugmentData record = ShippedRecord(PoisonSeriesRecord);

            float shipped = LentPotency(record);
            float moved = LentPotency(record with
            {
                UpgradeProperties = new Dictionary<string, float>(record.UpgradeProperties, StringComparer.Ordinal)
                {
                    ["poisonPotency"] = Doctored
                }
            });

            Assert.AreEqual(PoisonPotency, shipped, Tolerance, "the poison series record no longer lends the potency it names");
            Assert.AreEqual(Doctored, moved, Tolerance, "the record names another potency and the lent key did not follow it");
        }

        [TestMethod]
        public async Task TheLeachRecordLaysTheBuffForTheTurnsAndStacksItNames()
        {
            const int DoctoredTurns = 5;
            AbilityAugmentData record = ShippedRecord(LeachRecord);

            IEffect shipped = await LeechLaidBy(record);
            IEffect moved = await LeechLaidBy(record with
            {
                UpgradeProperties = new Dictionary<string, float>(record.UpgradeProperties, StringComparer.Ordinal)
                {
                    ["duration"] = DoctoredTurns
                }
            });

            Assert.AreEqual(LeachTurns, shipped.Duration, "the leach record no longer lays its buff for the turns it names");
            Assert.AreEqual(LeachStacks, shipped.MaxStacks, "the leach record no longer lays its buff at the stacks it names");
            Assert.AreEqual(DoctoredTurns, moved.Duration, "the record names other turns and the buff it lays did not follow it");
        }

        /// <summary>Turns of the stun one Ice Block cast leaves on its target.</summary>
        private static async Task<int> BlockStun(AbilityProvider registry, float draw)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(draw));
            var owner = Fighter();
            var target = Fighter();
            var block = (Ability)registry.CreateAbility(IceBlockId);
            block.SetOwner(owner);

            await block.Execute([target], FieldOf(owner, target));

            return EffectOn(target, StunId).Duration;
        }

        /// <summary>What one Ice Block takes off a target that is already stunned. The base stage only, so
        /// the extra blocks of stage four never join the figure.</summary>
        private static async Task<float> StunEatingBlow(AbilityAugmentData? wearing)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(NoStageDraw));
            (AbilityProvider registry, _) = ShippedAbilityData.Load();
            var owner = Fighter();
            var target = Fighter();
            var block = (Ability)registry.CreateAbility(IceBlockId);
            Wear(registry, block, wearing);
            block.SetOwner(owner);

            await new StunEffect(2).Apply(new EffectApplyingContext
            {
                Caster = owner,
                Target = target,
                Source = "Test_Standing_Stun"
            });

            float before = target.CurrentHealth;
            await block.Execute([target], FieldOf(owner, target));
            return before - target.CurrentHealth;
        }

        /// <summary>How many times one Chain Lightning cast touched the field — the opening strike and
        /// every jump after it.</summary>
        private static async Task<int> ChainStrikes(AbilityProvider registry, float draw)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(draw));
            var owner = Fighter();
            var target = Fighter();
            var bystander = Fighter();
            IAbility chain = registry.CreateAbility(ChainId);
            List<AbilityImpact> seen = Riding(chain);
            chain.SetOwner(owner);

            await chain.Execute([target], FieldOf(owner, target, bystander));

            return seen.Count;
        }

        /// <summary>Swings one Berserk Fury cast landed on its target, counted off the riders every attack
        /// of a series reaches.</summary>
        private static async Task<int> Swings(AbilityProvider registry, float health, float draw)
        {
            // One draw at the given figure and every draw after it at certainty: the series carries on at
            // most once past the first decision, so what the count says is which way that decision went.
            using var rolls = new CombatRandomScope(new ScriptedDraws(draw));
            var owner = Fighter();
            var target = Fighter();
            owner.CurrentHealth = health;
            IAbility fury = registry.CreateAbility(BerserkId);
            List<AbilityImpact> seen = Riding(fury);
            fury.SetOwner(owner);

            await fury.Execute([target], FieldOf(owner, target));

            return seen.Count;
        }

        /// <summary>What one Armageddon takes off a target missing the given health, with the rate the
        /// step is counted against seated as a decorator.</summary>
        private static async Task<float> MissingHealthBlow(AbilityProvider registry, float missing, float rate)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(NoStageDraw));
            var owner = Fighter();
            var target = Fighter();
            target.CurrentHealth = Health - missing;
            var cast = (ArmageddonCast)registry.CreateAbility(ArmageddonId);
            if (rate > 0)
                cast.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                    ArmageddonCast.Parameters.MissingHpRate, Priority.Weak, OperationType.Add, rate,
                    "Ability_Parameter_Decorator_Test_Missing_Hp_Rate", "Test"));
            cast.SetOwner(owner);

            float before = target.CurrentHealth;
            await cast.Execute([target], FieldOf(owner, target));
            return before - target.CurrentHealth;
        }

        /// <summary>Everything one stage-three Deep Freeze left on its target — the shred of stage two and
        /// the healing cut of stage three among it.</summary>
        private static async Task<List<IEffect>> FreezePayload(AbilityProvider registry)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(StageThreeDraw));
            var owner = Fighter();
            var target = Fighter();
            IAbility freeze = registry.CreateAbility(DeepFreezeId);
            freeze.SetOwner(owner);

            await freeze.Execute([target], FieldOf(owner, target));

            return [.. target.Effects.Effects];
        }

        /// <summary>Turns of the poison one jar leaves on its target.</summary>
        private static async Task<int> JarPoisonTurnsOf(AbilityProvider registry)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(NoStageDraw));
            var owner = Fighter();
            var target = Fighter();
            IAbility jar = registry.CreateAbility(JarId);
            jar.SetOwner(owner);

            await jar.Execute([target], FieldOf(owner, target));

            return EffectOn(target, PoisonId).Duration;
        }

        /// <summary>What the record is worth on the key it lends the ability it is seated on.</summary>
        private static float LentPotency(AbilityAugmentData record)
        {
            (AbilityProvider registry, _) = ShippedAbilityData.Load();
            var series = (Ability)registry.CreateAbility(SeriesId);
            Wear(registry, series, record);

            Assert.IsTrue(series.Declares(PoisonOnHitRider.Parameters.PoisonPotency),
                "the seated record lent no potency at all, so the figure below is nothing being read");
            return series[PoisonOnHitRider.Parameters.PoisonPotency];
        }

        /// <summary>The leech buff one cast wearing the record leaves on its caster.</summary>
        private static async Task<IEffect> LeechLaidBy(AbilityAugmentData record)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(NoStageDraw));
            (AbilityProvider registry, _) = ShippedAbilityData.Load();
            var owner = Fighter();
            var target = Fighter();
            var jar = (Ability)registry.CreateAbility(JarId);
            Wear(registry, jar, record);
            jar.SetOwner(owner);

            await jar.Execute([target], FieldOf(owner, target));

            return EffectOn(owner, LeechId);
        }

        /// <summary>Seats one record on the ability, the way the board seats what a socket holds.</summary>
        private static void Wear(AbilityProvider registry, Ability ability, AbilityAugmentData? record)
        {
            if (record == null) return;

            IAugment? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{record.Id}'");
            upgrade.Apply(ability);
        }

        /// <summary>The shipped abilities, off the real files and the real loader.</summary>
        private static AbilityProvider Shipped() => ShippedAbilityData.Abilities();

        private static AbilityAugmentData ShippedRecord(string augmentId)
        {
            AbilityAugmentData? record = ShippedAbilityData.Augments().Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            return record;
        }

        private static AbilityProvider AbilitiesWith(string abilityId, string key, float value) =>
            AbilitiesWith(abilityId, new Dictionary<string, float>(StringComparer.Ordinal) { [key] = value });

        /// <summary>The shipped catalog with figures of one ability rewritten, read back through the loader
        /// the game runs. The copy lives in memory — nothing on disk moves.</summary>
        private static AbilityProvider AbilitiesWith(string abilityId, IReadOnlyDictionary<string, float> figures)
        {
            JObject root = ShippedAbilityData.AbilityCatalog();
            JObject entry = Entry(root, "abilities", abilityId);
            if (entry["abilityProperties"] is not JObject properties)
            {
                properties = [];
                entry["abilityProperties"] = properties;
            }

            foreach ((string key, float value) in figures) properties[key] = value;
            return ShippedAbilityData.AbilitiesOver(root.ToString());
        }

        private static JObject Entry(JObject root, string section, string id)
        {
            JObject? entry = (root[section] as JArray ?? []).OfType<JObject>()
                .FirstOrDefault(candidate => (string?)candidate["id"] == id);

            Assert.IsNotNull(entry, $"the shipped data declares no '{id}'");
            return entry;
        }

        private static IEffect EffectOn(IFightable bearer, string effectId)
        {
            IEffect? effect = bearer.Effects.GetBy(candidate => candidate.IsSame(effectId)).FirstOrDefault();
            Assert.IsNotNull(effect, $"nothing laid '{effectId}' at all, so the figure below is not being measured");
            return effect;
        }

        private static int StackCeiling(List<IEffect> laid, string effectId)
        {
            IEffect? effect = laid.Find(candidate => candidate.IsSame(effectId));
            Assert.IsNotNull(effect, $"the payload holds no '{effectId}' at all, so the count below is not being measured");
            return effect.MaxStacks;
        }

        private static List<AbilityImpact> Riding(IAbility ability)
        {
            var capture = new CaptureRider();
            ability.AddImpactRider(capture.Id, capture);
            return capture.Impacts;
        }

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, Health);
            fighter.SetMaximum(EntityParameter.Mana, Health);
            fighter.SetMaximum(EntityParameter.PhysicalDamage, WeaponDamage);
            fighter.SetMaximum(EntityParameter.SpellDamage, SpellDamage);
            // No critical chance anywhere: a crit would multiply the blows the walks above compare, and
            // what they are about is the stage and the record rather than the dice.
            fighter.SetMaximum(EntityParameter.CriticalDamage, 2f);
            fighter.TakesDamageForReal = true;
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

        /// <summary>One fixed draw, whatever is asked.</summary>
        private sealed class FixedDraw(float draw) : Draws
        {
            public override float RandFloat() => draw;
        }

        /// <summary>The first draw as written and every one after it at certainty — so a decision taken
        /// twice is told from a decision taken once.</summary>
        private sealed class ScriptedDraws(float first) : Draws
        {
            private bool _drawn;

            public override float RandFloat()
            {
                if (_drawn) return 1f;
                _drawn = true;
                return first;
            }
        }

        private abstract class Draws : IRandomNumberGenerator
        {
            public abstract float RandFloat();

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }

        /// <summary>An impact rider that does nothing but remember what it was handed.</summary>
        private sealed class CaptureRider : IImpactRider
        {
            public List<AbilityImpact> Impacts { get; } = [];

            public string Id => "Rider_Capture_Stage_Numbers";

            public string InstanceId { get; } = Guid.NewGuid().ToString();

            public bool IsSame(string otherId) => Id.Equals(otherId, StringComparison.Ordinal);

            public Task Apply(AbilityImpact impact)
            {
                Impacts.Add(impact);
                return Task.CompletedTask;
            }
        }
    }
}
