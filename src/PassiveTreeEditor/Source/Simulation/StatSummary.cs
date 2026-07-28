namespace PassiveTreeEditor.Source.Simulation
{
    using System.Collections.Generic;
    using Core;
    using Core.Enums;
    using Core.Modifiers;
    using Model;

    /// <summary>
    /// What the current allocation is actually worth. The final number is produced by the game's own
    /// <see cref="Calculations.CalculateFloatValue"/> over real <see cref="SimpleModifier"/> instances
    /// rather than by re-adding percentages here — the panel and the battle cannot disagree.
    /// </summary>
    public static class StatSummary
    {
        private const string ModifierSource = "PassiveTree";

        public static TreeSummary Build(PassiveTreeDocument document, AllocationState allocation, BaseStatProfile baseStats)
        {
            var accumulators = new Dictionary<EntityParameter, ParameterAccumulator>();
            var summary = new TreeSummary();

            foreach (string id in allocation.Taken)
            {
                PassiveNode? node = document.Find(id);
                if (node is null) continue;

                Describe(node, summary);

                foreach (ModifierLine line in node.Modifiers)
                {
                    if (line.ValueType == ModifierValueType.Flag) continue;

                    if (line.IsConditional) summary.ConditionalLines++;

                    // An aggregate is bucket-only: the game folds it into every family member at
                    // resolution, so "+2 to all attributes" has to land on all three here too.
                    IReadOnlyList<EntityParameter> targets = AggregateParameters.IsAggregate(line.Parameter)
                        ? AggregateParameters.Members(line.Parameter)
                        : [line.Parameter];

                    foreach (EntityParameter target in targets)
                    {
                        if (!accumulators.TryGetValue(target, out ParameterAccumulator? accumulator))
                        {
                            accumulator = new ParameterAccumulator();
                            accumulators[target] = accumulator;
                        }

                        accumulator.Add(target, line);
                    }
                }
            }

            foreach (KeyValuePair<EntityParameter, ParameterAccumulator> pair in accumulators)
            {
                float baseValue = baseStats[pair.Key];
                summary.Parameters.Add(pair.Value.ToTotal(pair.Key, baseValue));
            }

            summary.Parameters.Sort(static (first, second) =>
                string.CompareOrdinal(first.Parameter.ToString(), second.Parameter.ToString()));

            return summary;
        }

        private static void Describe(PassiveNode node, TreeSummary summary)
        {
            string label = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;

            switch (node.Kind)
            {
                case PassiveNodeKind.Keystone:
                    summary.Keystones.Add(string.IsNullOrWhiteSpace(node.Description) ? label : $"{label} — {node.Description}");
                    break;
                case PassiveNodeKind.Start:
                case PassiveNodeKind.AbilityUnlock:
                    summary.Abilities.Add(node.AbilityId);
                    break;
                case PassiveNodeKind.SocketTier2:
                    summary.SocketsTier2.Add(node.AbilityId);
                    break;
                case PassiveNodeKind.SocketTier3:
                    summary.SocketsTier3.Add(node.AbilityId);
                    break;
                case PassiveNodeKind.Small:
                case PassiveNodeKind.Notable:
                default:
                    break;
            }
        }

        private sealed class ParameterAccumulator
        {
            private readonly List<IModifier> _modifiers = [];

            private float _flat;
            private float _increase;
            private float _multiplicative;
            private int _lines;
            private int _conditionalLines;

            public void Add(EntityParameter parameter, ModifierLine line)
            {
                _modifiers.Add(new SimpleModifier(parameter, line.ValueType, line.Value, ModifierSource));
                _lines++;
                if (line.IsConditional) _conditionalLines++;

                switch (line.ValueType)
                {
                    case ModifierValueType.Flat:
                        _flat += line.Value;
                        break;
                    case ModifierValueType.Increase:
                        _increase += line.Value;
                        break;
                    case ModifierValueType.Multiplicative:
                        _multiplicative += line.Value;
                        break;
                    case ModifierValueType.Flag:
                    default:
                        break;
                }
            }

            public ParameterTotal ToTotal(EntityParameter parameter, float baseValue) => new(
                parameter,
                _flat,
                _increase,
                _multiplicative,
                baseValue,
                Calculations.CalculateFloatValue(_modifiers, baseValue),
                _lines,
                _conditionalLines);
        }
    }

    /// <param name="Flat">Sum of the Flat bucket, added to the base before any scaling.</param>
    /// <param name="Increase">Sum of the Increase bucket; the game applies it as (1 + sum).</param>
    /// <param name="Multiplicative">Sum of the Multiplicative bucket; applied as (1 + sum) after Increase.</param>
    /// <param name="ConditionalLines">How many of the lines carry a condition and were counted as if always on.</param>
    public sealed record ParameterTotal(
        EntityParameter Parameter,
        float Flat,
        float Increase,
        float Multiplicative,
        float BaseValue,
        float Total,
        int Lines,
        int ConditionalLines);

    public sealed class TreeSummary
    {
        public List<ParameterTotal> Parameters { get; } = [];

        public List<string> Keystones { get; } = [];

        public List<string> Abilities { get; } = [];

        public List<string> SocketsTier2 { get; } = [];

        public List<string> SocketsTier3 { get; } = [];

        public int ConditionalLines { get; set; }

        public bool IsEmpty => Parameters.Count == 0 && Keystones.Count == 0 && Abilities.Count == 0;
    }
}
