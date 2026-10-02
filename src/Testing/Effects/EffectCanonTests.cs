namespace LastBreathTest.Effects
{
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.StaticArmor;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Enums;
    using LastBreathTest.Ability;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The canon of effect numbers (SharedData/Effects) against the registry that builds them and against
    /// the design list it was written from. Both pairings fail in silence otherwise: a row nothing builds
    /// is balance nobody plays, and an effect with no row falls back on whatever number a record happens
    /// to carry — the very thing CL-3 exists to end. The design table below is a SECOND copy of the list
    /// on purpose; reading it out of the same file would prove nothing.
    /// </summary>
    [TestClass]
    public class EffectCanonTests
    {
        /// <summary>The owner's list of temporary effects, transcribed: id, turns, stack ceiling and every
        /// figure the entry names. Rows outside the list are named in <see cref="s_offTheList"/>.</summary>
        private static readonly Dictionary<string, (int Duration, int MaxStacks, (string Key, float Value)[] Figures)> s_designList = new(StringComparer.Ordinal)
        {
            // The four Furies repeat healthPercent because the document states the burn once ("каждая
            // атака сжирает 5% здоровья") and the variants inherit it. The schema has no way to say
            // "the same as that one" and inventing one for four rows would cost more than it saves.
            ["Effect_Fury"] = (3, 1, [("healthPercent", 0.05f)]),
            ["Effect_Burning_Fury"] = (3, 1, [("healthPercent", 0.05f), ("burnDamage", 0.75f), ("burningDuration", 3f), ("burningMaxStacks", 999f)]),
            // The list says "увеличивает урон атак на 35%", so 0.35 is what it says: the canon records the
            // share GAINED and the effect adds the one. It used to carry the finished factor 1.35, which
            // read the same at effectiveness one and refused to move at any other.
            ["Effect_Primal_Fury"] = (3, 1, [("healthPercent", 0.05f), ("damageMultiplier", 0.35f)]),
            ["Effect_Healing_Fury"] = (3, 1, [("healthPercent", 0.05f), ("healAmount", 0.15f)]),
            // One stack, like every other seal: a seal changes a rule rather than piling up.
            ["Effect_Seal_Of_Slowness"] = (3, 1, [("amount", 1f)]),
            ["Effect_Seal_Of_Silence"] = (3, 1, []),
            ["Effect_Seal_Of_Oblivion"] = (3, 1, []),
            ["Effect_Seal_Of_Blood"] = (3, 1, []),
            ["Effect_Seal_Of_Spirit"] = (3, 1, []),
            ["Effect_Damage_Over_Turn_Burning"] = (3, 999, [("percentFromDamage", 0.45f)]),
            ["Effect_Damage_Over_Turn_Poison"] = (4, 999, [("percentFromDamage", 0.35f)]),
            ["Effect_Damage_Over_Turn_Bleed"] = (3, 999, [("percentFromDamage", 0.8f)]),
            // The list gives the Charge its turns and its ceiling and then describes the detonation
            // ("250 + (85% + 75%) молнией"), which is not the effect's figure to carry: the mark counts and
            // pops, the payload belongs to whoever laid it. Those three numbers are walked separately, on
            // the ability that hands them in — see TheChargeDetonatesForWhatTheListSays.
            ["Effect_Charge"] = (3, 3, []),
            ["Effect_Evade_First_Death"] = (3, 1, [("percentHealthToRecover", 0.35f)]),
            ["Effect_Fragility"] = (3, 3, [("critDamageAmp", 0.35f)]),
            ["Effect_Armor_Reduction"] = (3, 5, [("reduceBy", 0.15f)]),
            ["Effect_Clumsiness"] = (3, 5, [("value", 0.15f)]),
            ["Effect_Light_Step"] = (3, 5, [("value", 0.15f)]),
            ["Effect_Weakness"] = (3, 5, [("value", 0.15f)]),
            ["Effect_Fatigue"] = (3, 5, [("value", 0.15f)]),
            ["Effect_Armor_Buff"] = (3, 5, [("value", 0.15f)]),
            ["Effect_Feebleness"] = (3, 5, [("value", 0.15f)]),
            ["Effect_Clouded_Mind"] = (3, 3, [("value", 0.25f)]),
            ["Effect_Sorcery_Gift"] = (3, 3, [("value", 0.25f)]),
            ["Effect_Spell_Surge"] = (3, 5, [("value", 0.25f)]),
            ["Effect_Mind_Drain"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Mana_Flow"] = (3, 3, [("percentRegeneration", 0.15f)]),
            ["Effect_Rot"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Weak_Regeneration"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Percent_Health_Regeneration"] = (3, 1, [("percentRegeneration", 0.25f)]),
            ["Effect_Giants_Blessing"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Withering_Curse"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Curse"] = (1, 1, [("costIncrease", 150f)]),
            ["Effect_Regeneration"] = (3, 3, [("amount", 0.05f)]),
            ["Effect_Mana_Regeneration"] = (3, 3, [("percentRegeneration", 0.05f)]),
            ["Effect_Vulnerability"] = (3, 5, [("value", 0.15f)]),
            ["Effect_Decay"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Critical_Chance_Buff"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Accuracy_Buff"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Critical_Damage_Buff"] = (3, 3, [("value", 0.25f)]),
            ["Effect_Heal_Reduction"] = (3, 2, [("reduceBy", 0.45f)]),
            ["Effect_Blind"] = (3, 3, [("value", 0.25f)]),
            ["Effect_Enhance_Defense"] = (3, 3, [("value", 0.15f)]),
            ["Effect_Frostbite"] = (3, 8, [("coldDamageAmp", 0.15f)]),
            ["Effect_Lucky_Crit_Chance"] = (3, 1, []),
            ["Effect_Damage_Buff"] = (3, 3, [("value", 0.25f)]),
            ["Effect_Incoming_Damage_Reduction"] = (3, 2, [("reduce", 0.15f)]),
        };

        /// <summary>Canonical rows the design list does not name: equipment-grant effects, whose numbers
        /// came over from the code that used to carry them. Listed so the walk over the list stays exact
        /// instead of quietly forgiving anything unaccounted for.</summary>
        private static readonly string[] s_offTheList =
        [
            "Effect_Execution",
            "Effect_Life_Giving_Shade",
            "Effect_Next_Ability_Cooldown",
            // The mythic reading of the Critical Calculation. The design list names the AUGMENT and not an
            // effect, because what the augment grants is not a figure anybody balances — it is a rule about
            // other effects' turns. Its two canonical numbers are how long the rule holds and that it does
            // not stack; the extension it hands out is one turn by the augment's own line.
            "Effect_Mythic_Calculation",
            // The Critical Calculation's own buff, joined at А-1b. The design list names the ABILITY and
            // the chance it grants, not an effect of its own; the row carries that chance, the turns it
            // holds and a ceiling of five — the ability's three plus the two the stacks record can add,
            // so registering the effect held nothing back that a shipped build already had.
            "Effect_Crit_Calculation_Buff",
            // The two crit protections. The list of temporary effects does not name them: their figures
            // are written on the AUGMENTS that lay them, and are walked against that list separately —
            // see TheCritProtectionsSayWhatTheAugmentListSays.
            "Effect_Crit_Mitigation",
            "Effect_Ice_Crit_Mitigation"
        ];

        [TestMethod]
        public void EveryCanonicalRowIsBuiltAndEveryBuiltEffectHasACanonicalRow()
        {
            var canon = Canon();
            var registry = EffectProviders.WithoutCanon().KnownIds.ToHashSet(StringComparer.Ordinal);

            string[] unbuilt = [.. canon.Keys.Except(registry, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            string[] uncovered = [.. registry.Except(canon.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.AreEqual(0, unbuilt.Length, $"canonical rows nothing builds: [{string.Join(", ", unbuilt)}]");
            Assert.AreEqual(0, uncovered.Length,
                $"effects the registry builds with no canonical row — they fall back on whatever a record carries: [{string.Join(", ", uncovered)}]");
        }

        [TestMethod]
        public void EveryCanonicalRowCarriesExactlyTheKeysItsFactoryReads()
        {
            var provider = EffectProviders.WithoutCanon();
            List<string> broken = [];

            foreach ((string id, IReadOnlyDictionary<string, float> figures) in Canon())
            {
                IReadOnlyCollection<string>? declared = provider.KeysOf(id);
                if (declared == null) continue; // the pairing walk above owns this failure

                string[] unread = [.. figures.Keys.Except(declared, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                string[] missing = [.. declared.Except(figures.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                if (unread.Length > 0 || missing.Length > 0)
                    broken.Add($"{id}: nothing reads [{string.Join(", ", unread)}], missing [{string.Join(", ", missing)}]");
            }

            Assert.AreEqual(0, broken.Count, $"canonical rows the registry cannot take whole:\n  {string.Join("\n  ", broken)}");
        }

        [TestMethod]
        public void TheCanonSaysWhatTheDesignListSays()
        {
            var canon = Canon();
            List<string> divergent = [];

            foreach ((string id, var expected) in s_designList)
            {
                if (!canon.TryGetValue(id, out IReadOnlyDictionary<string, float>? figures))
                {
                    divergent.Add($"{id}: the list names it, the canon does not");
                    continue;
                }

                Compare(divergent, id, figures, "duration", expected.Duration);
                Compare(divergent, id, figures, "maxStacks", expected.MaxStacks);
                foreach ((string key, float value) in expected.Figures) Compare(divergent, id, figures, key, value);
            }

            string[] unaccounted = [.. canon.Keys.Except(s_designList.Keys, StringComparer.Ordinal).Except(s_offTheList, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.AreEqual(0, divergent.Count, $"canon against the design list:\n  {string.Join("\n  ", divergent)}");
            Assert.AreEqual(0, unaccounted.Length,
                $"canonical rows neither on the design list nor named as off it: [{string.Join(", ", unaccounted)}]");
        }

        [TestMethod]
        public void TheCritProtectionsSayWhatTheAugmentListSays()
        {
            // Both figures come from the augment list rather than the effect list: "снижает получаемый
            // критический урон на 80%" for the Porcupine's, "снижен на 50-80%" for the Ice Aegis'. The
            // second is a ladder by rarity the canon has no way to hold — a record laying an effect may
            // not restate its numbers — so the canon carries the floor of it and the ladder waits.
            var canon = Canon();

            Assert.AreEqual(0.8f, canon["Effect_Crit_Mitigation"]["value"], 0.0001f,
                "the Porcupine's crit protection left the 80% the augment list gives it");
            Assert.AreEqual(0.5f, canon["Effect_Ice_Crit_Mitigation"]["value"], 0.0001f,
                "the Ice Aegis' crit protection left the floor of the 50-80% the augment list gives it");

            // One stack each: the parameter is capped at 1 and a second stack of either would spend the
            // whole cap, flattening a crit into an ordinary hit for free.
            Assert.AreEqual(1f, canon["Effect_Crit_Mitigation"]["maxStacks"], 0.0001f);
            Assert.AreEqual(1f, canon["Effect_Ice_Crit_Mitigation"]["maxStacks"], 0.0001f);
        }

        [TestMethod]
        public void TheCritProtectionsAreDrawnAcrossTheRaritiesTheAugmentListGivesThem()
        {
            // The other half of what the list says about them, and the half no figure can carry: the
            // Porcupine's is a legendary at a flat 80%, while the Ice Aegis' spans "Эпический -
            // Легендарный" because its 50-80% is a ladder by rarity. Until the ladder can be expressed
            // the canon holds its floor, so the BAND is the only place that reading survives — and a
            // band quietly narrowed back to one rarity would erase it without moving a number.
            (_, AbilityAugmentCatalog catalog, _) = ShippedAbilityData.Composed();

            AbilityAugmentData porcupine = Record(catalog, "Augment_Porcupine_Crit_Mitigation");
            AbilityAugmentData iceAegis = Record(catalog, "Augment_Ice_Aegis_Crit_Mitigation_Under_Shield");

            Assert.AreEqual((Rarity.Legendary, Rarity.Legendary), porcupine.RarityBand,
                "the Porcupine's crit protection is one rarity at a flat figure and the list gives it no band");
            Assert.AreEqual((Rarity.Epic, Rarity.Legendary), iceAegis.RarityBand,
                "the Ice Aegis' crit protection lost the epic-to-legendary band its 50-80% is a ladder across");
        }

        /// <summary>One shipped record by id, or a failure naming it rather than a null dereference.</summary>
        private static AbilityAugmentData Record(AbilityAugmentCatalog catalog, string augmentId)
        {
            AbilityAugmentData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped catalog holds no record '{augmentId}'");
            return record;
        }

        [TestMethod]
        public void TheCritProtectionsAreNotTheGeneralDefenceBuffTheRecordsUsedToLay()
        {
            // What #199 was: both crit-protection augments laid Effect_Enhance_Defense, a shared buff the
            // effect pool and another record draw as well. Its 15% read out on cards promising far more,
            // and any rebalance of it moved three unrelated things at once. Named here so pointing them
            // back at it — or quietly rebalancing the shared buff to 80% — cannot pass unnoticed.
            var canon = Canon();

            Assert.AreEqual(0.15f, canon["Effect_Enhance_Defense"]["value"], 0.0001f,
                "the shared defence buff moved; check it was not rebalanced to serve the crit protections again");
            Assert.AreNotEqual("Effect_Enhance_Defense", LaidBy("Augment_Porcupine_Crit_Mitigation"));
            Assert.AreEqual("Effect_Crit_Mitigation", LaidBy("Augment_Porcupine_Crit_Mitigation"),
                "the Porcupine's crit augment lays something other than its own crit protection");
            Assert.AreEqual("Effect_Ice_Crit_Mitigation", LaidBy("Augment_Ice_Aegis_Crit_Mitigation_Under_Shield"),
                "the Ice Aegis' crit augment lays something other than its own crit protection");
        }

        [TestMethod]
        public async Task ACritProtectionBuiltFromTheCanonAloneActuallyCutsACriticalHit()
        {
            // The whole channel, end to end: the canon builds the effect, the effect lands on a fighter,
            // and a critical hit against him is resolved by the real calculation. Everything between the
            // file and the damage has to hold — the right parameter, an additive decorator, the caps —
            // and none of it is visible from the canon walks, which only read numbers off a file.
            var provider = Loaded();
            var owner = new ConditionOwner();

            IEffect? protection = provider.CreateEffect("Effect_Crit_Mitigation", RecordProperties.Empty);
            Assert.IsNotNull(protection, "the canon alone does not build the Porcupine's crit protection");
            await protection.Apply(new EffectApplyingContext { Caster = owner, Target = owner, Source = "test" });

            Assert.AreEqual(0.8f, owner.Parameters.GetValueForParameter(EntityParameter.CriticalDamageMitigation), 0.0001f,
                "the crit protection did not reach the parameter the crit calculation reads");

            var attacker = new ConditionOwner();
            var hit = new AttackContext(attacker, owner, 100f, null!, Mock.Of<IAttackContextScheduler>())
            {
                IsCritical = true,
                RawCriticalDamage = 3f
            };
            Calculations.CalculateInitialAttackDamage(hit);

            // Only the crit's SURPLUS is cut: a triple-damage crit against 80% protection keeps the hit
            // under it and a fifth of the two it added — 1 + (3 - 1) × (1 - 0.8) = 1.4 of the base hit.
            Assert.AreEqual(140f, hit.DamageComponents[DamageType.Physical], 0.0001f,
                "the crit protection is on the fighter and the critical hit was not cut by it");
            Assert.IsTrue(hit.DamageComponents[DamageType.Physical] > 100f,
                "a mitigated crit landed for less than the ordinary hit under it — the whole multiplier was scaled, not the surplus");
        }

        /// <summary>The effect a shipped augment record names, straight out of the catalog file.</summary>
        private static string LaidBy(string augmentId) =>
            EffectRecordsInData().Single(record => record.Id == augmentId).EffectId;

        [TestMethod]
        public void TheBurningFuryBurnIsNotTheBurningEffectAndIsNotToBeMergedWithIt()
        {
            // Two figures that look like duplicates of one another and are not: the Burning effect's
            // tick is a share of the damage DEALT, while the Burning Fury's is a share of the health the
            // bearer BURNED OFF HIMSELF. They are different quantities measured against different
            // things, and the day somebody tidies the file by pointing one at the other, this says so.
            var canon = Canon();

            Assert.AreNotEqual(
                canon["Effect_Damage_Over_Turn_Burning"]["percentFromDamage"],
                canon["Effect_Burning_Fury"]["burnDamage"],
                "the Burning Fury's share of burned health now equals the Burning tick's share of dealt "
                + "damage — check they were not merged: they measure different things");
        }

        [TestMethod]
        public void TheChargeDetonatesForWhatTheListSays()
        {
            // The Charge is the one entry of the list whose figures are not its own: the effect carries
            // turns and a ceiling, the detonation is handed in by the Static Armour that laid it. So the
            // list's "250 + (85% + 75%) молнией при накоплении максимума стаков" is asked of the ability.
            (AbilityProvider registry, _) = ShippedAbilityData.Load();
            var armor = (Ability)registry.CreateAbility("Ability_Static_Armor");

            Assert.AreEqual(250f, armor[StaticArmor.Parameters.DetonationDamage], 0.0001f, "the detonation's flat damage left the list");
            Assert.AreEqual(0.85f, armor[StaticArmor.Parameters.DetonationWeaponScale], 0.0001f, "the detonation's weapon scale left the list");
            Assert.AreEqual(0.75f, armor[StaticArmor.Parameters.DetonationSpellScale], 0.0001f, "the detonation's spell scale left the list");
            Assert.AreEqual(3, (int)armor[StaticArmor.Parameters.ChargeDuration], "the charge is laid for a different number of turns than the list gives it");
        }

        [TestMethod]
        public void EveryRowTheFileCarriesIsActuallyTakenByTheProvider()
        {
            // The walks above read the file straight, which is the point of them — but it also means a row
            // the provider REFUSED as it read (a key nothing reads, a strength that parses into nothing,
            // "Strogn") still counts as canon to them, while the effect it balances quietly falls back on
            // whatever a record happens to carry. This is the same file seen from the other side: what was
            // actually taken, named row by row.
            var provider = Loaded();
            List<string> dropped = [];

            foreach ((string id, IReadOnlyDictionary<string, float> figures) in Canon())
            {
                if (provider.CreateEffect(id, RecordProperties.Empty) == null)
                {
                    dropped.Add($"{id}: the file carries a row, the provider builds nothing from it");
                    continue;
                }

                if (figures.TryGetValue("maxStacks", out float stacks) && provider.StackCeilingOf(id) != (int)stacks)
                    dropped.Add($"{id}: the provider answers a stack ceiling the file does not carry");
            }

            Assert.AreEqual(0, dropped.Count, $"canonical rows lost between the file and the provider:\n  {string.Join("\n  ", dropped)}");
        }

        [TestMethod]
        public void AnEffectBuildsFromTheCanonAloneWithoutARecordCarryingNumbers()
        {
            // What CL-3b arrives at: records carry no effect numbers at all and every figure comes from here.
            var provider = Loaded();
            List<string> unbuildable = [];

            foreach (string id in provider.KnownIds)
                if (provider.CreateEffect(id, RecordProperties.Empty) == null)
                    unbuildable.Add(id);

            Assert.AreEqual(0, unbuildable.Count,
                $"effects the canon alone cannot build: [{string.Join(", ", unbuildable)}]");
        }

        [TestMethod]
        public void NoAugmentRecordRestatesANumberTheCanonAlreadyBalances()
        {
            // The end state of CL-3. A record naming an effect describes WHICH effect and on what
            // touch; how much it is worth is settled in one file. One record carrying its own figure
            // is all it takes for the two to start drifting, and the drift is invisible until somebody
            // reads both — which is how the shipped catalog came to hold seven of them.
            var provider = Loaded();
            List<string> restating = [];

            foreach ((string id, string effectId, List<string> keys) in EffectRecordsInData())
            {
                IReadOnlyCollection<string>? declared = provider.KeysOf(effectId);
                if (declared == null)
                {
                    restating.Add($"{id}: names '{effectId}', which nothing builds");
                    continue;
                }

                string[] restated = [.. keys.Intersect(declared, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                if (restated.Length > 0) restating.Add($"{id} ({effectId}): [{string.Join(", ", restated)}]");
            }

            Assert.IsTrue(EffectRecordsInData().Any(), "no effect-laying records were read, so this walk proves nothing");
            Assert.AreEqual(0, restating.Count,
                $"records restating figures the canon balances:\n  {string.Join("\n  ", restating)}");
        }

        [TestMethod]
        public void AFigureOfferedAlongsideTheCanonStillWinsBecauseTheItemGrantsNeedIt()
        {
            // The merge itself is NOT gone: the item-effect catalog has its own weights and its own
            // reasons to hand a figure over, and it is judged by its own walks. What CL-3b closed is
            // the AUGMENT record's door to it — see the walk above and the gate in AbilityProvider.
            var provider = Loaded();
            const string Id = "Effect_Blind";

            IEffect? canonical = provider.CreateEffect(Id, RecordProperties.Empty);
            IEffect? offered = provider.CreateEffect(Id, new RecordProperties(Id, new Dictionary<string, float>(StringComparer.Ordinal) { ["duration"] = 7 }));

            Assert.AreEqual(3, canonical?.Duration, "the canon did not supply the duration");
            Assert.AreEqual(7, offered?.Duration, "a figure handed over explicitly no longer wins over the canon");
            Assert.AreEqual(3, offered?.MaxStacks, "the keys nobody spoke about did not come from the canon");
        }

        /// <summary>Every shipped augment record that names an effect, with the property keys it carries.</summary>
        private static IEnumerable<(string Id, string EffectId, List<string> Keys)> EffectRecordsInData()
        {
            string path = SharedData.Catalog(DataCatalog.Abilities);
            foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                foreach (JObject entry in (JObject.Parse(File.ReadAllText(file))["augments"] as JArray ?? []).OfType<JObject>())
                {
                    string effectId = (string?)entry["effectId"] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(effectId)) continue;

                    List<string> keys = entry["upgradeProperties"] is JObject properties
                        ? [.. properties.Properties().Select(property => property.Name)]
                        : [];
                    yield return ((string?)entry["id"] ?? string.Empty, effectId, keys);
                }
        }

        [TestMethod]
        public void AKeyNoFactoryReadsIsStillRefusedEvenWithACanonBehindIt()
        {
            var provider = Loaded();
            const string Id = "Effect_Blind";

            Assert.IsNull(
                provider.CreateEffect(Id, new RecordProperties(Id, new Dictionary<string, float>(StringComparer.Ordinal) { ["valeu"] = 1f })),
                "the canon covered a typo instead of letting it be reported");
        }

        private static void Compare(List<string> divergent, string id, IReadOnlyDictionary<string, float> figures, string key, float expected)
        {
            if (!figures.TryGetValue(key, out float actual))
            {
                divergent.Add($"{id}.{key}: the list says {expected}, the canon says nothing");
                return;
            }

            if (Math.Abs(actual - expected) > 0.0001f) divergent.Add($"{id}.{key}: the list says {expected}, the canon says {actual}");
        }

        private static EffectProvider Loaded() => EffectProviders.FromShippedData();

        /// <summary>The shipped canon as written, read straight off the file rather than through the
        /// provider — the provider is one of the two sides being compared.</summary>
        private static Dictionary<string, IReadOnlyDictionary<string, float>> Canon()
        {
            Dictionary<string, IReadOnlyDictionary<string, float>> canon = new(StringComparer.Ordinal);
            string path = SharedData.Catalog(DataCatalog.Effects);
            Assert.IsTrue(Directory.Exists(path), $"the canonical effect catalog is missing at {path}");

            foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                foreach (JObject entry in (JObject.Parse(File.ReadAllText(file))["effects"] as JArray ?? []).OfType<JObject>())
                {
                    string id = (string?)entry["id"] ?? string.Empty;
                    canon[id] = entry["properties"] is JObject properties
                        ? properties.Properties().ToDictionary(property => property.Name, property => (float)property.Value!, StringComparer.Ordinal)
                        : new Dictionary<string, float>(StringComparer.Ordinal);
                }

            Assert.IsTrue(canon.Count > 0, "the canonical effect catalog was not read at all, so these walks prove nothing");
            return canon;
        }
    }
}
