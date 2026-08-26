namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;

    /// <summary>Which maximum caps which resistance, and what a resistance is worth once the cap is applied.
    /// The cap is a parameter of its own, so a total above it is legal: only the effective value mitigates,
    /// while everything above the maximum answers to resistance shred first.</summary>
    public static class ResistanceParameters
    {
        /// <summary>Maximum every entity is born with, as a fraction.</summary>
        public const float DefaultMaximum = 0.75f;

        private static readonly IReadOnlyDictionary<EntityParameter, EntityParameter> s_maximums =
            new Dictionary<EntityParameter, EntityParameter>
            {
                [EntityParameter.FireResistance] = EntityParameter.FireResistanceMaximum,
                [EntityParameter.ColdResistance] = EntityParameter.ColdResistanceMaximum,
                [EntityParameter.LightningResistance] = EntityParameter.LightningResistanceMaximum,
                [EntityParameter.PoisonResistance] = EntityParameter.PoisonResistanceMaximum,
            };

        /// <summary>The maximum parameters themselves.</summary>
        public static IReadOnlyList<EntityParameter> Maximums { get; } = s_maximums.Values.ToArray();

        /// <summary>The maximum capping this resistance; null for a parameter that is not a resistance.</summary>
        public static EntityParameter? MaximumFor(EntityParameter resistance) =>
            s_maximums.TryGetValue(resistance, out EntityParameter maximum) ? maximum : null;

        /// <summary>What actually mitigates: the total cut down to the current maximum.</summary>
        public static float Effective(float total, float maximum) => Math.Min(total, maximum);

        /// <summary>The same value read off an entity, both halves at once.</summary>
        public static float Effective(IEntityParametersComponent parameters, EntityParameter resistance) =>
            MaximumFor(resistance) is { } maximum
                ? Effective(parameters.GetValueForParameter(resistance), parameters.GetValueForParameter(maximum))
                : parameters.GetValueForParameter(resistance);

        /// <summary>The reserve above the cap — everything the total carries that mitigates nothing and
        /// answers to shred first. Never negative: a total under its maximum holds no reserve at all.</summary>
        public static float Overcap(float total, float maximum) => Math.Max(0f, total - maximum);

        /// <summary>The same reserve read off an entity. A parameter no maximum caps has none: nothing of it
        /// sits above a cap, so a keystone paid per unit of reserve is paid nothing for it.</summary>
        public static float Overcap(IEntityParametersComponent parameters, EntityParameter resistance) =>
            MaximumFor(resistance) is { } maximum
                ? Overcap(parameters.GetValueForParameter(resistance), parameters.GetValueForParameter(maximum))
                : 0f;
    }
}
