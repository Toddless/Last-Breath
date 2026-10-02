namespace LastBreathTest.Augment
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Ability;
    using Battle.Source.Abilities;
    using Combat;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Localization;
    using Localization;
    using Moq;
    using DoubleStrike = Battle.Source.Abilities.DoubleStrike.DoubleStrike;

    /// <summary>
    /// Where the scale points a record sells actually land. A cast deals as many figures as its delivery
    /// has touches, and each of them reads coefficients of its own: the book's pair carries the main blow
    /// and every other figure — a second strike, a later stage, a fragment, a detonation — spells its pair
    /// privately. A record standing on the book's key alone therefore bought ONE of those figures, which on
    /// the double strike meant the first strike grew and the second stood still, and on the ice shards
    /// meant nothing grew at all past stage one, where the plan's coefficients are replaced outright.
    ///
    /// The walks below are written against the delivery rather than the key: what a record selling scales
    /// promises is that the cast hits harder, and an ability where only some of its figures moved has not
    /// been sold that.
    /// </summary>
    [TestClass]
    public class AugmentScaleFamilyTests
    {
        /// <summary>The record of the bargain this file was opened for: scale points on both coefficients
        /// against a share of the cast's own price.</summary>
        private const string ScalesForCost = "Augment_Increasing_Scales_Add_Cost";

        /// <summary>Enough of everything that no cast is refused and nothing dies mid-delivery — a target
        /// that falls between two runs would make them count different numbers of touches.</summary>
        private const float ProbeVitals = 1_000_000f;

        /// <summary>Damage of the caster the coefficients are read against. Both are non-zero and unequal,
        /// so a record moving only one of the two cannot pass for one moving both.</summary>
        private const float ProbePhysical = 100f;

        private const float ProbeSpell = 200f;

        /// <summary>Two coefficient bases a flatness walk is measured on. Far enough apart that a record
        /// stating itself as a share of what it moves cannot answer the same on both.</summary>
        private const float SmallScale = 0.5f;

        private const float LargeScale = 4f;

        /// <summary>Every gate open and every stage reached, so the walk measures the whole delivery.</summary>
        private sealed class SteadyRoll : IRandomNumberGenerator
        {
            public float RandFloat() => 0f;

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

        [TestMethod]
        public void TheScalesRecordMovesTheCoefficientsOfBothStrikesOfTheDoubleStrike()
        {
            // The owner's report, written as a claim: the first strike grew by the record's points and the
            // second was left exactly where the data put it, because the second strike reads a pair of its
            // own and the record had never heard of it.
            AbilityProvider registry = ShippedAbilityData.Abilities();
            IAbility ability = registry.CreateAbility("Ability_Double_Strike");

            float firstWeapon = ability[AbilityParameter.WeaponDamageScale];
            float firstSpell = ability[AbilityParameter.SpellDamageScale];
            float secondWeapon = ability[DoubleStrike.Parameters.SecondWeaponScale];
            float secondSpell = ability[DoubleStrike.Parameters.SecondSpellScale];

            Seated(registry, ScalesForCost).Apply(ability);

            Assert.IsTrue(ability[AbilityParameter.WeaponDamageScale] > firstWeapon,
                "the first strike's weapon coefficient did not move at all");
            Assert.IsTrue(ability[AbilityParameter.SpellDamageScale] > firstSpell,
                "the first strike's spell coefficient did not move at all");
            Assert.IsTrue(ability[DoubleStrike.Parameters.SecondWeaponScale] > secondWeapon,
                "the second strike's weapon coefficient stood still while the first one grew");
            Assert.IsTrue(ability[DoubleStrike.Parameters.SecondSpellScale] > secondSpell,
                "the second strike's spell coefficient stood still while the first one grew");
        }

        [TestMethod]
        public async Task TheScalesRecordGrowsTheDamageOfACastWhoseStageReplacesTheBookPair()
        {
            // The other half of the report — "on other abilities the scales look unchanged". The ice
            // shards past stage one OVERWRITE the plan's coefficients with a staged pair of their own, so
            // a record seated on the book's key was decorating a number the delivery no longer reads: the
            // augment was worn, paid for, and the target took the same damage to the point.
            AbilityProvider registry = ShippedAbilityData.Abilities();

            float bare = await DamageDealt(registry, "Ability_Ice_Shards", augmentId: null);
            float augmented = await DamageDealt(registry, "Ability_Ice_Shards", ScalesForCost);

            Assert.IsTrue(bare > 0f, "the walk measured no damage at all, so it proves nothing");
            Assert.IsTrue(augmented > bare,
                $"the record sold scale points and the cast dealt the same {bare} damage with them as without");
        }

        [TestMethod]
        public void TheTooltipOfTheDoubleStrikePrintsTheCoefficientsItsStrikesActuallyDeal()
        {
            // Why the report read as "the tooltip shows it working": the description prints from the same
            // decorated set the damage math reads, so the first strike's coefficient in the text moved
            // exactly as far as the first strike did — and the second strike's number, printed from that
            // set and moved by nothing, stood beside it unchanged and honest. Both halves of the text are
            // held to the numbers here, so the two can never say different things.
            var provider = new FakeLocalizationProvider();
            provider.Strings["Ability_Double_Strike_Description"] = "{WeaponDamageScale:%} then {SecondWeaponScale:%}";
            Localization.Override(new LocalizationService(
                provider, new ModifierFormatter(provider, new ParameterFormatProvider()), new ContextModifierFormatter(provider), []));

            AbilityProvider registry = ShippedAbilityData.Abilities();
            IAbility ability = registry.CreateAbility("Ability_Double_Strike");
            Seated(registry, ScalesForCost).Apply(ability);

            string text = ability.Description;

            StringAssert.Contains(text, Printed(ability[AbilityParameter.WeaponDamageScale]),
                "the tooltip prints a first-strike coefficient the cast does not deal");
            StringAssert.Contains(text, Printed(ability[DoubleStrike.Parameters.SecondWeaponScale]),
                "the tooltip prints a second-strike coefficient the cast does not deal");
        }

        [TestMethod]
        public void TheScalesRecordIsInertOnAnAbilityThatDealsNoScaledDamage()
        {
            // The ordinary silence of the catalog, kept honest: an ability with no coefficient anywhere in
            // it has an empty family, so the record fits it by tag, charges its share of the price and
            // moves no damage — and does so without laying a decorator on a key nobody registered.
            AbilityProvider registry = ShippedAbilityData.Abilities();
            IAbility ability = registry.CreateAbility("Ability_Ares_Blessing");
            float cost = ability[AbilityParameter.CostValue];

            Seated(registry, ScalesForCost).Apply(ability);

            Assert.AreEqual(0, ability.WeaponScales.Count, "an ability that scales nothing named a weapon coefficient");
            Assert.AreEqual(0, ability.SpellScales.Count, "an ability that scales nothing named a spell coefficient");
            Assert.IsTrue(ability[AbilityParameter.CostValue] > cost, "the record's own bill went unpaid");
        }

        [TestMethod]
        public void EveryCoefficientAnAbilitySpellsPrivatelyIsInItsFamily()
        {
            // What the fix cannot prove about itself. Membership is the ability's own word, so the next
            // ability to give a strike a pair of its own joins the family by remembering to say so — and
            // forgetting is silent, which is the whole bug this file was opened for. The names are the
            // claim: a constant of an ability called a weapon or spell scale IS one.
            //
            // The suffix alone will not do it. The book spells the same concept two ways — the double
            // strike's SecondWeaponScale beside the ice shards' SecondStageWeaponDamageScale — so a match
            // on the tail let four of the shards' six coefficients sit outside the invariant, which is the
            // failure this walk exists to catch. What every one of them ends in is "Scale", and what tells
            // the two families apart is the word inside. The one private scale the match passes over is the
            // aegis's PerIntelligenceScale, and rightly: it scales a barrier off an ATTRIBUTE and no record
            // selling weapon or spell points has any business on it.
            AbilityProvider registry = ShippedAbilityData.Abilities();
            List<string> unspoken = [];

            foreach (string abilityId in registry.KnownAbilityIds)
            {
                IAbility ability = registry.CreateAbility(abilityId);

                foreach (string parameter in PrivateKeysOf(ability))
                {
                    if (!parameter.EndsWith("Scale", StringComparison.Ordinal)) continue;

                    if (parameter.Contains("Weapon", StringComparison.Ordinal) && !ability.WeaponScales.Contains(parameter))
                        unspoken.Add($"{abilityId}.{parameter} is a weapon coefficient nobody declared");
                    if (parameter.Contains("Spell", StringComparison.Ordinal) && !ability.SpellScales.Contains(parameter))
                        unspoken.Add($"{abilityId}.{parameter} is a spell coefficient nobody declared");
                }
            }

            Assert.AreEqual(0, unspoken.Count,
                $"a strike carries a coefficient no record selling scales can reach:\n  {string.Join("\n  ", unspoken)}");
        }

        [TestMethod]
        public void NoRecordNamesTwoMembersOfOneFamily()
        {
            // What stopped being true by construction when a key started naming a family. A decorator is
            // named by its record and its parameter, so a record naming BOTH the book's scale key and a
            // private member of the same family would seat two moves under one name on that member — and
            // taking the augment out of its socket would lift exactly one of them, leaving half its work
            // on the ability forever. Unreachable while the table names shared keys only, and cheap to
            // keep unreachable.
            AbilityProvider registry = ShippedAbilityData.Abilities();
            List<string> doubled = [];

            foreach (string abilityId in registry.KnownAbilityIds)
            {
                IAbility ability = registry.CreateAbility(abilityId);

                foreach (string augmentId in registry.BuildableAugmentIds)
                {
                    List<string> landing = [.. registry.ParametersMovedBy(augmentId).SelectMany(ability.Family)];
                    foreach (string parameter in landing.GroupBy(key => key, StringComparer.Ordinal)
                                 .Where(group => group.Count() > 1).Select(group => group.Key))
                        doubled.Add($"'{augmentId}' lands on '{parameter}' of '{abilityId}' twice under one name");
                }
            }

            Assert.AreEqual(0, doubled.Count, string.Join("\n  ", doubled));
        }

        [TestMethod]
        public void EveryRecordSellingScalePointsSellsThemFlat()
        {
            // Owner's decision 29, made code. Rivalry between two records on one parameter is settled by
            // distance from the BASE, and every member of a family carries a base of its own — so a flat
            // record and a share-shaped one seated together would win on some figures of a delivery and
            // lose on others, and the augment the player is wearing would be half in force. While every
            // scale record is flat the winner is the same on every member and the build reads as one thing.
            // Measured rather than read off the table, so the factories are held to it as well as the rows.
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            List<string> shares = [];
            int measured = 0;

            foreach (AbilityAugmentData record in catalog.All)
                foreach (string scale in (string[])[AbilityParameter.WeaponDamageScale, AbilityParameter.SpellDamageScale])
                {
                    float small = Sells(registry, record, scale, SmallScale);
                    float large = Sells(registry, record, scale, LargeScale);
                    if (Math.Abs(small) < 0.0001f && Math.Abs(large) < 0.0001f) continue;

                    measured++;
                    if (Math.Abs(small - large) > 0.0001f)
                        shares.Add($"'{record.Id}' moves '{scale}' by {small} on a base of {SmallScale} "
                                   + $"and by {large} on a base of {LargeScale} — that is a share, not points");
                }

            Assert.IsTrue(measured > 0, "no shipped record moved a scale at all, so this walk proves nothing");
            Assert.AreEqual(0, shares.Count, $"a record sells scale points as a share:\n  {string.Join("\n  ", shares)}");
        }

        /// <summary>How far one record moves a scale on an ability whose coefficients start at the given
        /// figure. A flat record answers the same on any base; a share answers in proportion to it.</summary>
        private static float Sells(AbilityProvider registry, AbilityAugmentData record, string scale, float start)
        {
            var probe = new ScaleProbe(new AbilityBaseData
            {
                Id = "Ability_Scale_Probe",
                CostValue = 100,
                Cooldown = 4,
                Damage = 10f,
                WeaponDamageScale = start,
                SpellDamageScale = start
            });

            float before = probe[scale];
            // A record written for one ability class cannot be seated on a bare probe and sells no scales.
            if (registry.CreateUpgrade(record) is Augment<Ability> seated) seated.Apply(probe);

            return probe[scale] - before;
        }

        /// <summary>An ability that is its damage coefficients and nothing else — it never casts.</summary>
        private sealed class ScaleProbe(AbilityBaseData data) : DamagingAbility(data)
        {
            public override IAbility Copy() => new ScaleProbe(Data);

            protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) =>
                Task.CompletedTask;
        }

        /// <summary>A fraction as the description engine writes it — a share rendered as a percentage,
        /// coloured the way every number in a rich tooltip is.</summary>
        private static string Printed(float share) =>
            TextPalette.ColorizeNumber(TextTemplateEngine.FormatNumber(share * 100f) + "%");

        /// <summary>The parameter names an ability spells for itself: the constants of its nested
        /// <c>Parameters</c> class, which is where every private key of the book lives. Suffixes are read
        /// off the names because that is the only thing a walk can hold a NEW key to.</summary>
        private static IEnumerable<string> PrivateKeysOf(IAbility ability)
        {
            Type? parameters = ability.GetType().GetNestedType("Parameters", BindingFlags.Public | BindingFlags.Static);
            if (parameters == null) return [];

            return parameters
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string))
                .Select(field => field.GetValue(null))
                .OfType<string>();
        }

        /// <summary>The upgrade one shipped record builds, at the numbers the record itself carries.</summary>
        private static IAugment Seated(AbilityProvider registry, string augmentId)
        {
            AbilityAugmentData? record = ShippedAbilityData.Augments().Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");

            IAugment? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentId}'");

            return upgrade;
        }

        /// <summary>What one cast of the ability takes off a target, with the record seated or without it.</summary>
        private static async Task<float> DamageDealt(AbilityProvider registry, string abilityId, string? augmentId)
        {
            using var rolls = new CombatRandomScope(new SteadyRoll());
            ConditionOwner caster = Fighter();
            ConditionOwner enemy = Fighter();
            enemy.TakesDamageForReal = true;

            IAbility ability = registry.CreateAbility(abilityId);
            if (augmentId != null) Seated(registry, augmentId).Apply(ability);
            ability.SetOwner(caster);

            await ability.Execute([enemy], FieldOf(caster, enemy));

            return ProbeVitals - enemy.CurrentHealth;
        }

        private static ConditionOwner Fighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, ProbeVitals);
            fighter.SetMaximum(EntityParameter.Mana, ProbeVitals);
            fighter.SetMaximum(EntityParameter.Barrier, ProbeVitals);
            fighter.SetMaximum(EntityParameter.PhysicalDamage, ProbePhysical);
            fighter.SetMaximum(EntityParameter.SpellDamage, ProbeSpell);
            fighter.CurrentHealth = ProbeVitals;
            fighter.CurrentMana = ProbeVitals;

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
