namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Effects;

    /// <summary>
    /// Builds an effect from an id and the numbers a record carries — the single registry item grants
    /// and catalog records share. Every entry declares the keys it reads; balance lives in data, this
    /// registry only wires ids to code. Growth rule and the diagnostics — <c>Docs/PLAN-Augments.md §4b</c>.
    /// </summary>
    public class EffectProvider : IEffectProvider
    {
        /// <summary>The keys an effect is built from and the builder that reads them. All keys are required:
        /// an effect built without one of its numbers is a mis-tuned effect nobody asked for.</summary>
        private sealed record EffectFactory(string[] Keys, Func<RecordProperties, IEffect> Build);

        private static readonly Dictionary<string, EffectFactory> s_factories = new(StringComparer.Ordinal)
        {
            // Item grants. Battle-scoped: the grant re-applies them each battle, so "the whole battle"
            // is a high duration in the payload.
            ["Effect_Evade_First_Death"] = new(["duration", "percentHealthToRecover"], p =>
                new EvadeFirstDeath(p.GetInt("duration"), maxStacks: 1, p.Get("percentHealthToRecover"))),
            ["Effect_Regeneration"] = new(["amount", "duration", "maxStacks"], p =>
                new RegenerationEffect(p.Get("amount"), p.GetInt("duration"), p.GetInt("maxStacks"))),
            ["Effect_Lucky_Crit_Chance"] = new(["duration", "maxStacks"], p =>
                new LuckyCritChanceEffect(p.GetInt("duration"), p.GetInt("maxStacks"))),
            ["Effect_Execution"] = new(["duration", "maxStacks", "percentage"], p =>
                new ExecutionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("percentage"))),
            ["Effect_Curse"] = new(["duration", "maxStacks", "costIncrease"], p =>
                new CurseEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("costIncrease"))),
            ["Effect_Life_Giving_Shade"] = new(["lifeToRecover", "duration", "activations"], p =>
                new LifeGivingShadeEffect(p.Get("lifeToRecover"), p.GetInt("duration"), p.GetInt("activations"))),

            // Debuffs the shipped catalog records already lay.
            ["Effect_Clumsiness"] = new(["duration", "maxStacks", "value"], p =>
                new Clumsiness(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Blind"] = new(["duration", "maxStacks", "value"], p =>
                new BlindEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Weakness"] = new(["duration", "maxStacks", "value"], p =>
                new Weakness(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Armor_Reduction"] = new(["duration", "maxStacks", "reduceBy"], p =>
                new ArmorReductionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("reduceBy"))),
            ["Effect_Heal_Reduction"] = new(["duration", "maxStacks", "reduceBy"], p =>
                new HealReductionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("reduceBy"))),
            ["Effect_Next_Ability_Cooldown"] = new(["duration", "amount"], p =>
                new NextAbilityCooldownEffect(p.GetInt("duration"), p.Get("amount"))),

            // Buffs the shipped catalog records already lay.
            ["Effect_Armor_Buff"] = new(["duration", "maxStacks", "value"], p =>
                new ArmorBuffEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Damage_Buff"] = new(["duration", "maxStacks", "value"], p =>
                new DamageBuffEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Enhance_Defense"] = new(["duration", "maxStacks", "value"], p =>
                new EnhanceDefenseEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("value"))),
            ["Effect_Incoming_Damage_Reduction"] = new(["duration", "maxStacks", "reduce"], p =>
                new IncomingDamageReductionEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("reduce"))),
            ["Effect_Mana_Regeneration"] = new(["percentRegeneration", "duration", "maxStacks"], p =>
                new ManaRegenerationEffect(p.Get("percentRegeneration"), p.GetInt("duration"), p.GetInt("maxStacks"))),
            ["Effect_Percent_Health_Regeneration"] = new(["percentRegeneration", "duration", "maxStacks"], p =>
                new HealthRegenerationEffect(p.Get("percentRegeneration"), p.GetInt("duration"), p.GetInt("maxStacks"))),

            // Mana regeneration under a name of its own: same effect, its own icon and text.
            ["Effect_Mana_Flow"] = new(["percentRegeneration", "duration", "maxStacks"], p =>
                new ManaRegenerationEffect(p.Get("percentRegeneration"), p.GetInt("duration"), p.GetInt("maxStacks"), id: "Effect_Mana_Flow")),
            ["Effect_Fragility"] = new(["duration", "maxStacks", "critDamageAmp"], p =>
                new FragilityEffect(p.GetInt("duration"), p.GetInt("maxStacks"), p.Get("critDamageAmp"))),
            ["Effect_Seal_Of_Oblivion"] = new(["duration", "maxStacks"], p =>
                new OblivionSeal(p.GetInt("duration"), p.GetInt("maxStacks"))),
        };

        public IReadOnlyCollection<string> KnownIds => s_factories.Keys;

        public IReadOnlyCollection<string>? KeysOf(string effectId) =>
            s_factories.TryGetValue(effectId, out EffectFactory? factory) ? [.. factory.Keys] : null;

        public IEffect? CreateEffect(string id, RecordProperties properties)
        {
            if (!s_factories.TryGetValue(id, out EffectFactory? factory))
            {
                Tracker.TrackNotFound($"Effect factory for '{id}'. Known: {string.Join(", ", s_factories.Keys.Order(StringComparer.Ordinal))}", this);
                return null;
            }

            string[] unknown = [.. properties.Names.Except(factory.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            string[] missing = [.. factory.Keys.Except(properties.Names, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            if (unknown.Length > 0 || missing.Length > 0)
            {
                Tracker.TrackError(
                    $"Effect '{id}' not built. Unknown properties: [{string.Join(", ", unknown)}]; "
                    + $"missing: [{string.Join(", ", missing)}]; it reads: [{string.Join(", ", factory.Keys)}]");
                return null;
            }

            return factory.Build(properties);
        }
    }
}
