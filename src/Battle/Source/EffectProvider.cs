namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.EffectsData;
    using Core.Data.GameData;
    using Core.Enums;
    using Effects;
    using Newtonsoft.Json;

    /// <summary>
    /// Builds an effect from an id and the numbers behind it — the single registry item grants and
    /// catalog records share. Every entry declares the keys it reads; the numbers themselves are
    /// balanced in ONE place, <c>SharedData/Effects</c>, which this provider loads as its canon.
    /// A record may still carry its own numbers and they win over the canon for now — the transition
    /// is described in <c>Docs/PLAN-Augments.md §4f</c>, and CL-3b takes the numbers off the records.
    /// </summary>
    public class EffectProvider : IEffectProvider, IGameDataParticipant
    {
        /// <summary>The keys an effect is built from and the builder that reads them. All keys are required:
        /// an effect built without one of its numbers is a mis-tuned effect nobody asked for.</summary>
        private sealed record EffectFactory(string[] Keys, Func<RecordProperties, IEffect> Build);

        private static readonly Dictionary<string, EffectFactory> s_factories = new(StringComparer.Ordinal)
        {
            // ---- Fury family. The variants differ by what the burned health buys.
            ["Effect_Fury"] = new(["duration", "maxStacks", "healthPercent"], p =>
                new FuryEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("healthPercent"))),
            ["Effect_Burning_Fury"] = new(["duration", "maxStacks", "healthPercent", "burnDamage", "burningDuration", "burningMaxStacks"], p =>
                new BurningFuryEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("healthPercent"))
                {
                    BurnDamage = p.Get("burnDamage"),
                    BurningDuration = p.GetInt("burningDuration"),
                    BurningMaxStacks = p.GetInt("burningMaxStacks")
                }),
            ["Effect_Primal_Fury"] = new(["duration", "maxStacks", "healthPercent", "damageMultiplier"], p =>
                new PrimalFuryEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("healthPercent"))
                {
                    DamageMultiplier = p.Get("damageMultiplier")
                }),
            ["Effect_Healing_Fury"] = new(["duration", "maxStacks", "healthPercent", "healAmount"], p =>
                new HealingFuryEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("healthPercent"))
                {
                    HealAmount = p.Get("healAmount")
                }),

            // ---- Seals: what an activation is forbidden, delayed or charged.
            ["Effect_Seal_Of_Slowness"] = new(["duration", "maxStacks", "amount"], p =>
                new SlownessSeal(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("amount"))),
            ["Effect_Seal_Of_Silence"] = new(["duration", "maxStacks"], p =>
                new SilenceSeal(p.GetInt("duration"), p.GetInt("maxStacks"))),
            ["Effect_Seal_Of_Oblivion"] = new(["duration", "maxStacks"], p =>
                new OblivionSeal(p.GetInt("duration"), p.GetInt("maxStacks"))),
            ["Effect_Seal_Of_Blood"] = new(["duration", "maxStacks"], p =>
                new BloodSeal(p.GetInt("duration"), p.GetInt("maxStacks"))),
            ["Effect_Seal_Of_Spirit"] = new(["duration", "maxStacks"], p =>
                new SpiritSeal(p.GetInt("duration"), p.GetInt("maxStacks"))),

            // ---- Damage over turns. One identity per damage kind — the status is code, the numbers data.
            ["Effect_Damage_Over_Turn_Burning"] = new(["duration", "maxStacks", "percentFromDamage"], p =>
                new DamageOverTurnEffect(p.GetInt("duration"), StatusEffects.Burning, p.GetInt("maxStacks"), p.Get("percentFromDamage"))),
            ["Effect_Damage_Over_Turn_Poison"] = new(["duration", "maxStacks", "percentFromDamage"], p =>
                new DamageOverTurnEffect(p.GetInt("duration"), StatusEffects.Poison, p.GetInt("maxStacks"), p.Get("percentFromDamage"))),
            ["Effect_Damage_Over_Turn_Bleed"] = new(["duration", "maxStacks", "percentFromDamage"], p =>
                new DamageOverTurnEffect(p.GetInt("duration"), StatusEffects.Bleed, p.GetInt("maxStacks"), p.Get("percentFromDamage"))),

            // ---- Recovery and survival.
            ["Effect_Evade_First_Death"] = new(["duration", "maxStacks", "percentHealthToRecover"], p =>
                new EvadeFirstDeath(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("percentHealthToRecover"))),
            // Percent by construction: the canon figure is a share of max health, matching the design list.
            ["Effect_Regeneration"] = new(["amount", "duration", "maxStacks"], p =>
                new RegenerationEffect(p.Get("amount"), p.GetInt("duration"), p.GetInt("maxStacks"), isPercent: true)),
            ["Effect_Percent_Health_Regeneration"] = new(["percentRegeneration", "duration", "maxStacks"], p =>
                new HealthRegenerationEffect(p.Get("percentRegeneration"), p.GetInt("duration"), p.GetInt("maxStacks"))),
            ["Effect_Weak_Regeneration"] = new(["duration", "maxStacks", "value"], p =>
                new WeakRegenerationEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Mana_Regeneration"] = new(["percentRegeneration", "duration", "maxStacks"], p =>
                new ManaRegenerationEffect(p.Get("percentRegeneration"), p.GetInt("duration"), p.GetInt("maxStacks"))),
            // Mana regeneration under a name of its own: same effect, its own icon, text and numbers.
            ["Effect_Mana_Flow"] = new(["percentRegeneration", "duration", "maxStacks"], p =>
                new ManaRegenerationEffect(p.Get("percentRegeneration"), p.GetInt("duration"), p.GetInt("maxStacks"), id: "Effect_Mana_Flow")),
            ["Effect_Life_Giving_Shade"] = new(["lifeToRecover", "duration", "activations"], p =>
                new LifeGivingShadeEffect(p.Get("lifeToRecover"), p.GetInt("duration"), p.GetInt("activations"))),

            // ---- Debuffs.
            ["Effect_Clumsiness"] = new(["duration", "maxStacks", "value"], p =>
                new Clumsiness(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Blind"] = new(["duration", "maxStacks", "value"], p =>
                new BlindEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Weakness"] = new(["duration", "maxStacks", "value"], p =>
                new Weakness(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Fatigue"] = new(["duration", "maxStacks", "value"], p =>
                new FatigueEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Feebleness"] = new(["duration", "maxStacks", "value"], p =>
                new FeeblenessEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Armor_Reduction"] = new(["duration", "maxStacks", "reduceBy"], p =>
                new ArmorReductionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("reduceBy"))),
            ["Effect_Heal_Reduction"] = new(["duration", "maxStacks", "reduceBy"], p =>
                new HealReductionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("reduceBy"))),
            ["Effect_Mind_Drain"] = new(["duration", "maxStacks", "value"], p =>
                new MindDrainEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Rot"] = new(["duration", "maxStacks", "value"], p =>
                new RotEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Decay"] = new(["duration", "maxStacks", "value"], p =>
                new DecayEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Vulnerability"] = new(["duration", "maxStacks", "value"], p =>
                new VulnerabilityEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Withering_Curse"] = new(["duration", "maxStacks", "value"], p =>
                new WitheringCurseEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Clouded_Mind"] = new(["duration", "maxStacks", "value"], p =>
                new CloudedMindEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Curse"] = new(["duration", "maxStacks", "costIncrease"], p =>
                new CurseEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("costIncrease"))),
            ["Effect_Fragility"] = new(["duration", "maxStacks", "critDamageAmp"], p =>
                new FragilityEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("critDamageAmp"))),
            ["Effect_Frostbite"] = new(["duration", "maxStacks", "coldDamageAmp"], p =>
                new FrostbiteEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("coldDamageAmp"))),
            ["Effect_Execution"] = new(["duration", "maxStacks", "percentage"], p =>
                new ExecutionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("percentage"))),
            ["Effect_Next_Ability_Cooldown"] = new(["duration", "amount"], p =>
                new NextAbilityCooldownEffect(p.GetInt("duration"), p.Get("amount"))),

            // ---- Buffs.
            ["Effect_Armor_Buff"] = new(["duration", "maxStacks", "value"], p =>
                new ArmorBuffEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Damage_Buff"] = new(["duration", "maxStacks", "value"], p =>
                new DamageBuffEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Spell_Surge"] = new(["duration", "maxStacks", "value"], p =>
                new SpellSurgeEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Giants_Blessing"] = new(["duration", "maxStacks", "value"], p =>
                new GiantsBlessingEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Sorcery_Gift"] = new(["duration", "maxStacks", "value"], p =>
                new SorceryGiftEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Enhance_Defense"] = new(["duration", "maxStacks", "value"], p =>
                new EnhanceDefenseEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Incoming_Damage_Reduction"] = new(["duration", "maxStacks", "reduce"], p =>
                new IncomingDamageReductionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("reduce"))),
            ["Effect_Light_Step"] = new(["duration", "maxStacks", "value"], p =>
                new LightStep(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Accuracy_Buff"] = new(["duration", "maxStacks", "value"], p =>
                new AccuracyBuff(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Critical_Chance_Buff"] = new(["duration", "maxStacks", "value"], p =>
                new CriticalChanceBuffEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Critical_Damage_Buff"] = new(["duration", "maxStacks", "value"], p =>
                new CriticalDamageBuffEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Lucky_Crit_Chance"] = new(["duration", "maxStacks"], p =>
                new LuckyCritChanceEffect(p.GetInt("duration"), p.GetInt("maxStacks"))),
        };

        /// <summary>Canonical numbers per effect id, as loaded from SharedData/Effects.</summary>
        private readonly Dictionary<string, IReadOnlyDictionary<string, float>> _canon = new(StringComparer.Ordinal);

        /// <summary>The pairing of registry and canon is reported once, on first use: loading walks file
        /// by file, and a catalog of several files would be judged half-read at the end of the first.</summary>
        private bool _pairingReported;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Effects];

        public IReadOnlyCollection<string> KnownIds
        {
            get
            {
                ReportPairingOnce();
                return s_factories.Keys;
            }
        }

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<EffectCatalogData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize effect data");

            foreach (EffectDefinitionData definition in data.Effects)
            {
                if (!s_factories.TryGetValue(definition.Id, out EffectFactory? factory))
                {
                    // An error and not a missed lookup: balance written for an effect nothing builds is
                    // balance that will never be played, and the file is the thing being read right now.
                    Tracker.TrackError(
                        $"Canonical row '{definition.Id}' names an effect nothing builds. "
                        + $"Known: {string.Join(", ", s_factories.Keys.Order(StringComparer.Ordinal))}");
                    continue;
                }

                string[] unread = [.. definition.Properties.Keys.Except(factory.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                string[] missing = [.. factory.Keys.Except(definition.Properties.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                if (unread.Length > 0 || missing.Length > 0)
                {
                    Tracker.TrackError(
                        $"Canonical row '{definition.Id}' not taken. Properties nothing reads: [{string.Join(", ", unread)}]; "
                        + $"missing: [{string.Join(", ", missing)}]; the factory reads: [{string.Join(", ", factory.Keys)}]");
                    continue;
                }

                _canon[definition.Id] = new Dictionary<string, float>(definition.Properties, StringComparer.Ordinal);
            }

            // A second file may complete the pairing, so the verdict is postponed until first use.
            _pairingReported = false;
        }

        public int? StackCeilingOf(string effectId) =>
            _canon.TryGetValue(effectId, out IReadOnlyDictionary<string, float>? canon)
            && canon.TryGetValue("maxStacks", out float ceiling)
                ? (int)ceiling
                : null;

        public IReadOnlyCollection<string>? KeysOf(string effectId) =>
            s_factories.TryGetValue(effectId, out EffectFactory? factory) ? [.. factory.Keys] : null;

        public IEffect? CreateEffect(string id, RecordProperties properties)
        {
            ReportPairingOnce();

            if (!s_factories.TryGetValue(id, out EffectFactory? factory))
            {
                Tracker.TrackNotFound($"Effect factory for '{id}'. Known: {string.Join(", ", s_factories.Keys.Order(StringComparer.Ordinal))}", this);
                return null;
            }

            // A key nothing reads stays an outright refusal — that is the typo trap the registry exists
            // for, and the canon does not excuse it.
            string[] unknown = [.. properties.Names.Except(factory.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            var numbers = Compose(id, properties, factory);
            string[] missing = [.. factory.Keys.Except(numbers.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            if (unknown.Length > 0 || missing.Length > 0)
            {
                Tracker.TrackError(
                    $"Effect '{id}' not built. Unknown properties: [{string.Join(", ", unknown)}]; "
                    + $"missing from record and canon: [{string.Join(", ", missing)}]; it reads: [{string.Join(", ", factory.Keys)}]");
                return null;
            }

            return factory.Build(new RecordProperties(id, numbers));
        }

        /// <summary>
        /// The numbers the effect is actually built from: the canon is the default, a number the record
        /// carries overrides it. The override is the transition — records stop carrying effect numbers in
        /// CL-3b, after which every figure here comes from the canon alone.
        /// </summary>
        private Dictionary<string, float> Compose(string id, RecordProperties properties, EffectFactory factory)
        {
            var numbers = _canon.TryGetValue(id, out IReadOnlyDictionary<string, float>? canon)
                ? new Dictionary<string, float>(canon, StringComparer.Ordinal)
                : new Dictionary<string, float>(StringComparer.Ordinal);

            foreach (string key in factory.Keys)
                if (properties.Names.Contains(key, StringComparer.Ordinal))
                    numbers[key] = properties.Get(key);

            return numbers;
        }

        /// <summary>Names both halves of a broken pairing rather than letting an effect fall back on
        /// whatever a record happens to carry. Rows naming an id nothing builds are reported as the file
        /// is read; this is the other direction, plus the case of no canon at all.</summary>
        private void ReportPairingOnce()
        {
            if (_pairingReported) return;
            _pairingReported = true;

            if (_canon.Count == 0)
            {
                Tracker.TrackError(
                    $"Canonical effect data ({DataCatalog.Effects}) was never loaded: every effect falls back "
                    + "on the numbers its record happens to carry.");
                return;
            }

            string[] uncovered = [.. s_factories.Keys.Except(_canon.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            if (uncovered.Length > 0)
                Tracker.TrackError($"Effects the registry builds with no canonical row: [{string.Join(", ", uncovered)}]");
        }
    }
}
