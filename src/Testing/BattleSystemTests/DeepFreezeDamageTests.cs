namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;
    using DeepFreezeCast = Battle.Source.Abilities.DeepFreeze.DeepFreeze;

    /// <summary>
    /// The Deep Freeze deals damage, which it did not until CL-3a — the stance document had it dealing
    /// "80 + (35% + 65%)" all along while the ability delivered a freeze and nothing else. A blow added
    /// to an ability that had none touches everything downstream: the crit roll, the cold mitigation,
    /// what the impact riders are told, and the order the payload lands in. None of that is visible from
    /// the record, so it is walked here.
    /// </summary>
    [TestClass]
    public class DeepFreezeDamageTests
    {
        private const string AbilityId = "Ability_Deep_Freeze";

        /// <summary>The figures the stance document names, and the ones the record carries.</summary>
        private const float Flat = 80f;
        private const float WeaponShare = 0.35f;
        private const float SpellShare = 0.65f;

        private const float Health = 100000f;
        private const float WeaponDamage = 200f;
        private const float SpellDamage = 400f;

        /// <summary>What one un-critted, un-resisted blow comes to on the fighter below.</summary>
        private static float Expected => Flat + (WeaponDamage * WeaponShare) + (SpellDamage * SpellShare);

        [TestMethod]
        public async Task TheBlowIsTheFlatDamagePlusBothScalesOfTheCaster()
        {
            using var rolls = new CombatRandomScope(new NoCrit());
            var owner = Fighter();
            var target = Fighter();
            var freeze = Shipped();
            freeze.SetOwner(owner);

            await freeze.Execute([target], FieldOf(owner, target));

            Assert.AreEqual(Expected, Health - target.CurrentHealth, 0.01f,
                "the Deep Freeze no longer deals the flat damage plus both scales the document names");
        }

        [TestMethod]
        public async Task TheBlowIsCold()
        {
            // Not decoration: the cold resistance below is what a target's gear buys against this cast,
            // and a blow typed as anything else walks straight past it. It is also what the Frostbite
            // the same cast lays amplifies — a mistyped blow makes the ability's own combo inert.
            using var rolls = new CombatRandomScope(new NoCrit());
            var owner = Fighter();
            var resistant = Fighter();
            resistant.SetMaximum(EntityParameter.ColdResistance, 0.5f);
            var freeze = Shipped();
            freeze.SetOwner(owner);

            await freeze.Execute([resistant], FieldOf(owner, resistant));

            float taken = Health - resistant.CurrentHealth;
            Assert.IsTrue(taken < Expected,
                $"cold resistance did not touch the blow ({taken} of {Expected}) — it is not being dealt as cold damage");
        }

        [TestMethod]
        public async Task ACriticalBlowLandsHarderThanAPlainOne()
        {
            using var rolls = new CombatRandomScope(new NoCrit());
            var plainTarget = Fighter();
            var plainOwner = Fighter();
            var plain = Shipped();
            plain.SetOwner(plainOwner);
            await plain.Execute([plainTarget], FieldOf(plainOwner, plainTarget));
            float plainDamage = Health - plainTarget.CurrentHealth;

            using var crits = new CombatRandomScope(new AlwaysCrit());
            var critTarget = Fighter();
            var critOwner = Fighter();
            var critical = Shipped();
            critical.SetOwner(critOwner);
            await critical.Execute([critTarget], FieldOf(critOwner, critTarget));

            Assert.IsTrue(Health - critTarget.CurrentHealth > plainDamage,
                "a critical Deep Freeze landed no harder than a plain one — the blow skips the crit roll");
        }

        [TestMethod]
        public async Task TheImpactCarriesTheDamageThatActuallyLandedAndSaysWhetherItCrit()
        {
            // Riders bought for this cast read the impact and nothing else. It used to carry a hard
            // zero, so every "on damage" rider seated on the Deep Freeze was buying nothing.
            using var rolls = new CombatRandomScope(new AlwaysCrit());
            var owner = Fighter();
            var target = Fighter();
            var freeze = Shipped();
            var seen = Riding(freeze);
            freeze.SetOwner(owner);

            await freeze.Execute([target], FieldOf(owner, target));

            AbilityImpact hit = seen.Find(impact => impact.Kind == ImpactKind.Hit && ReferenceEquals(impact.Target, target))!;
            Assert.IsNotNull(hit, "the cast's own target was never reported to the riders");
            Assert.AreEqual(Health - target.CurrentHealth, hit.Damage.Total, 0.01f,
                "the impact reported a different number than the target actually lost");
            Assert.IsTrue(hit.IsCritical, "a critical blow reached the riders as a plain one");
        }

        [TestMethod]
        public async Task TheBlowLandsBeforeTheFrostbiteSoItDoesNotAmplifyItself()
        {
            // The whole point of the ordering. Frostbite raises the COLD damage its bearer takes, and
            // this cast lays Frostbite and deals cold damage in one go — payload first and the ability
            // would amplify its own blow by a stack the target did not have when it was struck. The
            // claim is made in both directions: this blow is unamplified, and the NEXT one is not.
            using var rolls = new CombatRandomScope(new NoCrit());
            var owner = Fighter();
            var target = Fighter();
            var first = Shipped();
            first.SetOwner(owner);

            await first.Execute([target], FieldOf(owner, target));
            float firstBlow = Health - target.CurrentHealth;

            Assert.AreEqual(Expected, firstBlow, 0.01f,
                "the first blow was amplified — the Frostbite this very cast laid was standing when it struck");
            Assert.IsTrue(target.Effects.GetBy(effect => effect.IsSame("Effect_Frostbite")).Any(),
                "no Frostbite was laid at all, so the measurement above proves nothing");

            float healthBeforeSecond = target.CurrentHealth;
            var second = Shipped();
            second.SetOwner(owner);
            await second.Execute([target], FieldOf(owner, target));

            Assert.IsTrue(healthBeforeSecond - target.CurrentHealth > firstBlow,
                "the second blow was not amplified by the standing Frostbite, so the ordering above is untested");
        }

        [TestMethod]
        public async Task StageFourSpreadsTheFreezeOverTheFieldButNotTheBlow()
        {
            // Stage 4 widens the payload to every enemy. The blow keeps its own target list, so the
            // stage buys a battlefield of frozen enemies and not a battlefield of damaged ones — a
            // distinction that only exists because the plan carries the two lists apart.
            using var rolls = new CombatRandomScope(new StageFourThenNoCrit());
            var owner = Fighter();
            var aimed = Fighter();
            var bystander = Fighter();
            var freeze = Shipped();
            freeze.SetOwner(owner);

            await freeze.Execute([aimed], FieldOf(owner, aimed, bystander));

            Assert.AreEqual(Expected, Health - aimed.CurrentHealth, 0.01f, "the aimed target did not take the blow");
            Assert.AreEqual(Health, bystander.CurrentHealth,
                "a bystander the stage only meant to freeze took the full blow as well");
            Assert.IsTrue(bystander.Effects.GetBy(effect => effect.IsSame("Effect_Freeze")).Any(),
                "the stage did not freeze the bystander at all, so the claim above is vacuous");
        }

        [TestMethod]
        public void ThePayloadItLaysCarriesTheCanonicalNumbersOfThoseEffects()
        {
            // The ability builds its payload by hand rather than through the effect registry, because
            // every one of these figures is also an augment handle on its parameter set and a registry
            // call would take them off it. That leaves two readings of one number, so they are compared
            // here: edit the canon and this names the ability that did not follow.
            var canon = CanonOf("Effect_Frostbite", "Effect_Heal_Reduction");
            var freeze = Shipped();
            List<string> divergent = [];

            Compare(divergent, freeze, "Effect_Frostbite", "duration", canon, "FrostbiteDuration");
            Compare(divergent, freeze, "Effect_Frostbite", "maxStacks", canon, AbilityParameter.Stacks);
            Compare(divergent, freeze, "Effect_Frostbite", "coldDamageAmp", canon, "FrostbiteColdAmp");
            Compare(divergent, freeze, "Effect_Heal_Reduction", "duration", canon, "HealReductionDuration");
            Compare(divergent, freeze, "Effect_Heal_Reduction", "maxStacks", canon, "HealReductionStacks");
            Compare(divergent, freeze, "Effect_Heal_Reduction", "reduceBy", canon, "HealReductionValue");

            Assert.AreEqual(0, divergent.Count,
                $"the Deep Freeze lays effects on numbers of its own:\n  {string.Join("\n  ", divergent)}");
        }

        private static void Compare(
            List<string> divergent, DeepFreezeCast freeze, string effectId, string key,
            Dictionary<string, IReadOnlyDictionary<string, float>> canon, string parameter)
        {
            float canonical = canon[effectId][key];
            float onTheAbility = freeze[parameter];
            if (Math.Abs(canonical - onTheAbility) > 0.0001f)
                divergent.Add($"{effectId}.{key}: the canon says {canonical}, the ability's '{parameter}' says {onTheAbility}");
        }

        private static Dictionary<string, IReadOnlyDictionary<string, float>> CanonOf(params string[] ids)
        {
            EffectProvider provider = EffectProviders.FromShippedData();

            Dictionary<string, IReadOnlyDictionary<string, float>> canon = new(StringComparer.Ordinal);
            foreach (string id in ids)
            {
                IEffect? built = provider.CreateEffect(id, RecordProperties.Empty);
                Assert.IsNotNull(built, $"the canon does not build '{id}', so the comparison below proves nothing");
                canon[id] = Numbers(id);
            }

            return canon;
        }

        /// <summary>One canonical row as written.</summary>
        private static IReadOnlyDictionary<string, float> Numbers(string effectId)
        {
            foreach (string file in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Effects), "*.json"))
                foreach (Newtonsoft.Json.Linq.JObject entry in
                         (Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(file))["effects"] as Newtonsoft.Json.Linq.JArray ?? [])
                         .OfType<Newtonsoft.Json.Linq.JObject>())
                {
                    if ((string?)entry["id"] != effectId) continue;
                    return ((Newtonsoft.Json.Linq.JObject)entry["properties"]!).Properties()
                        .ToDictionary(property => property.Name, property => (float)property.Value!, StringComparer.Ordinal);
                }

            Assert.Fail($"'{effectId}' has no canonical row");
            return new Dictionary<string, float>();
        }

        private static DeepFreezeCast Shipped() => (DeepFreezeCast)ShippedAbilityData.Abilities().CreateAbility(AbilityId);

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
            // A crit that multiplied by nothing would make the crit walk pass for the wrong reason.
            fighter.SetMaximum(EntityParameter.CriticalDamage, 2f);
            // A chance the scripted draws can win and lose: a fighter without one crits on nothing at all.
            fighter.SetMaximum(EntityParameter.CriticalChance, 0.5f);
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

        /// <summary>Every roll at the top of its range: the stage lands on the base one and the crit
        /// roll fails, so what is measured is the plain blow.</summary>
        private sealed class NoCrit : IRandomNumberGenerator
        {
            public float RandFloat() => 1f;

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

        /// <summary>Every roll at the bottom: the crit roll succeeds. (The stage roll comes out at 4 as
        /// well; its payload lands after the blow and adds no damage, so the blow is still the blow.)</summary>
        private sealed class AlwaysCrit : IRandomNumberGenerator
        {
            public float RandFloat() => 0f;

            public float RandFloatRange(float min, float max) => min;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }

        /// <summary>The stage roll is the FIRST roll of a multicast, so the bottom of the range once
        /// opens stage 4 and the top of it ever after keeps the blow itself plain.</summary>
        private sealed class StageFourThenNoCrit : IRandomNumberGenerator
        {
            private bool _stageRolled;

            public float RandFloat()
            {
                if (_stageRolled) return 1f;
                _stageRolled = true;
                return 0f;
            }

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

        /// <summary>Records every impact a delivery reports.</summary>
        private sealed class CaptureRider : IImpactRider
        {
            public List<AbilityImpact> Impacts { get; } = [];

            public string Id => "Rider_Capture_Deep_Freeze";

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
