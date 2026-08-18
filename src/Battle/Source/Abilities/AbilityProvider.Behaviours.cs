namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Modifiers.Context;
    using Riders;

    public partial class AbilityProvider
    {
        /// <summary>What a behaviour needs from the record besides its numbers, and how it is built.
        /// Declared like the effect registry's keys: a record missing a field or naming an unknown one
        /// is refused with names rather than built half-way.</summary>
        private sealed record Behaviour(BehaviourField Fields, Func<AbilityAugmentData, Func<IEffectProvider?>, IAbilityAugment?> Build);

        [Flags]
        private enum BehaviourField
        {
            None = 0,
            EffectId = 1,
            ImpactKind = 2,
            AttackModifier = 4
        }

        /// <summary>
        /// The behaviours a record may declare instead of a factory of its own. Three roads today:
        /// lay an effect on every touch of a kind, lay one on the caster at activation, tune the
        /// ability's attacks. Grows by record like the effect registry — see <c>Docs/PLAN-Augments.md §4d</c>.
        /// </summary>
        private static readonly Dictionary<string, Behaviour> s_behaviours = new(StringComparer.Ordinal)
        {
            ["ApplyEffectOnImpact"] = new(BehaviourField.EffectId | BehaviourField.ImpactKind, (data, effects) =>
                new AbilityAugmentImpactRider(data.Id, data.Tags, data.Tier, new DataEffectImpactRider(
                    data.Id, data.LaidEffectId, host => EffectNumbers(data, host, effects), KindOf(data), data.PoolFromWholeHit, effects))),

            ["BuffOnCast"] = new(BehaviourField.EffectId, (data, effects) =>
                new AbilityAugmentCastEffect(data.Id, data.Tags, data.Tier,
                    host => effects()?.CreateEffect(data.LaidEffectId, EffectNumbers(data, host, effects)))),

            ["DebuffOnCast"] = new(BehaviourField.EffectId, (data, effects) =>
                new AbilityAugmentCastDebuff(data.Id, data.Tags, data.Tier,
                    host => effects()?.CreateEffect(data.LaidEffectId, EffectNumbers(data, host, effects)))),

            ["AttackModifier"] = new(BehaviourField.AttackModifier, (data, _) =>
                new AbilityAugmentAttackModifier(data.Id, data.Tags, data.Tier, s_attackModifiers[data.AttackModifier]())),
        };

        /// <summary>Attack modifiers a record may name. One today; it grows by record.</summary>
        private static readonly Dictionary<string, Func<IAttackModifier>> s_attackModifiers = new(StringComparer.Ordinal)
        {
            ["Unevadable"] = () => new UnevadableAttackContextModifier(),
        };

        /// <summary>Behaviour names a record may declare — for the walks that hold the data to them.</summary>
        public static IReadOnlyCollection<string> KnownBehaviours => s_behaviours.Keys;

        /// <summary>The upgrade a record's behaviour builds, or null with a report. Null for a record
        /// that declares no behaviour at all: that one is answered by a factory of its own.</summary>
        private IAbilityAugment? CreateBehaviour(AbilityAugmentData data)
        {
            if (string.IsNullOrWhiteSpace(data.Behaviour)) return null;

            if (!s_behaviours.TryGetValue(data.Behaviour, out Behaviour? behaviour))
                return Refused(data, $"names behaviour '{data.Behaviour}'. Known: {string.Join(", ", s_behaviours.Keys.Order(StringComparer.Ordinal))}");

            string? complaint = Missing(data, behaviour.Fields)
                                ?? Extra(data, behaviour.Fields)
                                ?? Unbuildable(data, behaviour.Fields);
            return complaint != null ? Refused(data, complaint) : behaviour.Build(data, _effects);
        }

        /// <summary>What the record names and the registries cannot make. Asked HERE, at load, because
        /// the alternative is a record that parses, is offered and is seated, and throws on the cast.</summary>
        private string? Unbuildable(AbilityAugmentData data, BehaviourField fields)
        {
            foreach ((string property, string parameter) in data.PropertyRefs)
            {
                if (!s_sharedParameters.Contains(parameter))
                    return $"points property '{property}' at '{parameter}', which is no shared parameter. Known: {string.Join(", ", s_sharedParameters.Order(StringComparer.Ordinal))}";
            }

            if (fields.HasFlag(BehaviourField.AttackModifier) && !s_attackModifiers.ContainsKey(data.AttackModifier))
                return $"names attack modifier '{data.AttackModifier}'. Known: {string.Join(", ", s_attackModifiers.Keys.Order(StringComparer.Ordinal))}";

            if (!fields.HasFlag(BehaviourField.EffectId)) return null;

            IEffectProvider? effects = _effects();
            if (effects == null) return $"lays effect '{data.LaidEffectId}' and no effect registry is composed to build it";

            // Every effect a copy of the record MIGHT lay, and not only the one an un-minted record
            // stands for: a pool member the registry cannot build is a copy that lays nothing, and which
            // copy that is would be decided by a draw rather than by anything anybody could read.
            foreach (string effectId in data.PoolEffects.Count > 0 ? data.PoolEffects : [data.EffectId])
                if (Unbuildable(data, effectId, effects) is { } complaint) return complaint;

            return null;
        }

        /// <summary>What stops one named effect from being laid by this record — unknown to the registry,
        /// or with its canonical figures restated on the record.</summary>
        private static string? Unbuildable(AbilityAugmentData data, string effectId, IEffectProvider effects)
        {
            IReadOnlyCollection<string>? keys = effects.KeysOf(effectId);
            if (keys == null)
                return $"names effect '{effectId}' the registry cannot build. Known: {string.Join(", ", effects.KnownIds.Order(StringComparer.Ordinal))}";

            // The numbers of an effect are balanced in one file and a record may not restate them: one
            // record carrying its own figure is all it takes for the balance to start drifting apart
            // again, and the drift is invisible until somebody reads both. What a record may still carry
            // is its OWN levers — anything the effect does not read.
            string[] restated = [.. data.UpgradeProperties.Keys.Intersect(keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            return restated.Length > 0
                ? $"carries figures of effect '{effectId}' that are balanced in the canon: [{string.Join(", ", restated)}]"
                : null;
        }

        /// <summary>Fields the behaviour needs and the record did not write.</summary>
        private static string? Missing(AbilityAugmentData data, BehaviourField fields)
        {
            if (fields.HasFlag(BehaviourField.EffectId) && string.IsNullOrWhiteSpace(data.LaidEffectId)) return "declares neither effectId nor effectPool";
            if (fields.HasFlag(BehaviourField.AttackModifier) && string.IsNullOrWhiteSpace(data.AttackModifier)) return "declares no attackModifier";
            return null;
        }

        /// <summary>Fields the record wrote and the behaviour never reads — a line nobody would ever
        /// notice doing nothing — and the one pair that contradicts rather than idles.</summary>
        private static string? Extra(AbilityAugmentData data, BehaviourField fields)
        {
            // Both is not "more information": one says the effect is fixed and the other says it is
            // drawn, and whichever the code happened to read would leave the other line silently untrue.
            if (!string.IsNullOrWhiteSpace(data.EffectId) && data.EffectPool.Count > 0)
                return "names an effectId AND an effectPool, so what it lays is stated twice";
            if (!fields.HasFlag(BehaviourField.EffectId) && !string.IsNullOrWhiteSpace(data.LaidEffectId)) return "writes an effectId its behaviour never reads";
            if (!fields.HasFlag(BehaviourField.ImpactKind) && !string.IsNullOrWhiteSpace(data.ImpactKind)) return "writes an impactKind its behaviour never reads";
            if (!fields.HasFlag(BehaviourField.AttackModifier) && !string.IsNullOrWhiteSpace(data.AttackModifier)) return "writes an attackModifier its behaviour never reads";
            // The pool lever travels on the applying context, which only the impact road builds.
            if (!fields.HasFlag(BehaviourField.ImpactKind) && data.PoolFromWholeHit) return "writes poolFromWholeHit its behaviour never reads";
            return null;
        }

        /// <summary>Which touches the behaviour works on, or null for every one of them.</summary>
        private static ImpactKind? KindOf(AbilityAugmentData data) =>
            string.IsNullOrWhiteSpace(data.ImpactKind) ? null : EnumParser.ParseEnum<ImpactKind>(data.ImpactKind);

        /// <summary>
        /// What the effect registry is handed: the record's figures for the keys that effect READS, and
        /// nothing else. A record may carry levers of its own beside the effect it names — the registry
        /// judges an unknown key as a typo and refuses the whole effect, so passing them on would turn
        /// "a record with a lever" into "a record that lays nothing" without a word being said.
        /// <para><see cref="AbilityAugmentData.PropertyRefs"/> are read off the host here — decorated and
        /// at the moment of use, so an augment moving that key is felt. A key the ability never declared
        /// is left OUT rather than written as nothing: the canon fills it, and a zero written here would
        /// override the canon with a number nobody chose.</para>
        /// </summary>
        private static RecordProperties EffectNumbers(AbilityAugmentData data, IAbility? host, Func<IEffectProvider?> effects)
        {
            Dictionary<string, float> values = new(data.UpgradeProperties, StringComparer.Ordinal);

            if (host != null)
                foreach ((string property, string parameter) in data.PropertyRefs)
                    if (host.Declares(parameter)) values[property] = host.ValueOr(parameter, 0f);

            IReadOnlyCollection<string>? keys = effects()?.KeysOf(data.LaidEffectId);
            if (keys != null)
                foreach (string stranger in values.Keys.Except(keys, StringComparer.Ordinal).ToList())
                    values.Remove(stranger);

            return new RecordProperties(data.Id, values);
        }

        /// <summary>Shared parameter names a record may point at — the constants of
        /// <see cref="AbilityParameter"/>, read once by reflection so the list cannot drift.</summary>
        private static readonly HashSet<string> s_sharedParameters =
        [
            .. typeof(AbilityParameter)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
        ];

        private static IAbilityAugment? Refused(AbilityAugmentData data, string complaint)
        {
            Tracker.TrackError($"Augment '{data.Id}' {complaint}");
            return null;
        }
    }
}
