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
        private sealed record Behaviour(BehaviourField Fields, Func<AbilityUpgradeData, Func<IEffectProvider?>, IAbilityUpgrade?> Build);

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
                new AbilityUpgradeImpactRider(data.Id, data.Tags, data.Tier, new DataEffectImpactRider(
                    data.Id, data.EffectId, host => Numbers(data, host), KindOf(data), effects))),

            ["BuffOnCast"] = new(BehaviourField.EffectId, (data, effects) =>
                new AbilityUpgradeCastEffect(data.Id, data.Tags, data.Tier,
                    host => effects()?.CreateEffect(data.EffectId, Numbers(data, host)))),

            ["DebuffOnCast"] = new(BehaviourField.EffectId, (data, effects) =>
                new AbilityUpgradeCastDebuff(data.Id, data.Tags, data.Tier,
                    host => effects()?.CreateEffect(data.EffectId, Numbers(data, host)))),

            ["AttackModifier"] = new(BehaviourField.AttackModifier, (data, _) =>
                new AbilityUpgradeAttackModifier(data.Id, data.Tags, data.Tier, s_attackModifiers[data.AttackModifier]())),
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
        private IAbilityUpgrade? CreateBehaviour(AbilityUpgradeData data)
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
        private string? Unbuildable(AbilityUpgradeData data, BehaviourField fields)
        {
            foreach ((string property, string parameter) in data.PropertyRefs)
            {
                if (!s_sharedParameters.Contains(parameter))
                    return $"points property '{property}' at '{parameter}', which is no shared parameter. Known: {string.Join(", ", s_sharedParameters.Order(StringComparer.Ordinal))}";

                if (!data.UpgradeProperties.ContainsKey(property))
                    return $"points property '{property}' at '{parameter}' and carries no figure for it to fall back to";
            }

            if (fields.HasFlag(BehaviourField.AttackModifier) && !s_attackModifiers.ContainsKey(data.AttackModifier))
                return $"names attack modifier '{data.AttackModifier}'. Known: {string.Join(", ", s_attackModifiers.Keys.Order(StringComparer.Ordinal))}";

            if (!fields.HasFlag(BehaviourField.EffectId)) return null;

            IEffectProvider? effects = _effects();
            if (effects == null) return $"lays effect '{data.EffectId}' and no effect registry is composed to build it";

            return effects.KeysOf(data.EffectId) == null
                ? $"names effect '{data.EffectId}' the registry cannot build. Known: {string.Join(", ", effects.KnownIds.Order(StringComparer.Ordinal))}"
                : null;
        }

        /// <summary>Fields the behaviour needs and the record did not write.</summary>
        private static string? Missing(AbilityUpgradeData data, BehaviourField fields)
        {
            if (fields.HasFlag(BehaviourField.EffectId) && string.IsNullOrWhiteSpace(data.EffectId)) return "declares no effectId";
            if (fields.HasFlag(BehaviourField.AttackModifier) && string.IsNullOrWhiteSpace(data.AttackModifier)) return "declares no attackModifier";
            return null;
        }

        /// <summary>Fields the record wrote and the behaviour never reads — a line nobody would ever
        /// notice doing nothing.</summary>
        private static string? Extra(AbilityUpgradeData data, BehaviourField fields)
        {
            if (!fields.HasFlag(BehaviourField.EffectId) && !string.IsNullOrWhiteSpace(data.EffectId)) return "writes an effectId its behaviour never reads";
            if (!fields.HasFlag(BehaviourField.ImpactKind) && !string.IsNullOrWhiteSpace(data.ImpactKind)) return "writes an impactKind its behaviour never reads";
            if (!fields.HasFlag(BehaviourField.AttackModifier) && !string.IsNullOrWhiteSpace(data.AttackModifier)) return "writes an attackModifier its behaviour never reads";
            return null;
        }

        /// <summary>Which touches the behaviour works on, or null for every one of them.</summary>
        private static ImpactKind? KindOf(AbilityUpgradeData data) =>
            string.IsNullOrWhiteSpace(data.ImpactKind) ? null : EnumParser.ParseEnum<ImpactKind>(data.ImpactKind);

        private static RecordProperties Numbers(AbilityUpgradeData data) => new(data.Id, data.UpgradeProperties);

        /// <summary>The record's numbers with its <see cref="AbilityUpgradeData.PropertyRefs"/> read off
        /// the host — decorated and at the moment of use, so an augment moving that key is felt here.
        /// A key the ability never declared falls back to the record's own figure.</summary>
        private static RecordProperties Numbers(AbilityUpgradeData data, IAbility? host)
        {
            if (data.PropertyRefs.Count == 0 || host == null) return Numbers(data);

            Dictionary<string, float> values = new(data.UpgradeProperties, StringComparer.Ordinal);
            foreach ((string property, string parameter) in data.PropertyRefs)
                values[property] = host.ValueOr(parameter, values.GetValueOrDefault(property));

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

        private static IAbilityUpgrade? Refused(AbilityUpgradeData data, string complaint)
        {
            Tracker.TrackError($"Augment '{data.Id}' {complaint}");
            return null;
        }
    }
}
