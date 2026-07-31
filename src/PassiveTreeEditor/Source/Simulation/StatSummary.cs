namespace PassiveTreeEditor.Source.Simulation
{
    using System.Collections.Generic;
    using Core;
    using Core.Enums;
    using Core.Modifiers;
    using Core.PassiveTree;

    /// <summary>
    /// What the current allocation is actually worth. The final number is produced by the game's own
    /// <see cref="Calculations.CalculateFloatValue"/> over real <see cref="SimpleModifier"/> instances
    /// rather than by re-adding percentages here — the panel and the battle cannot disagree.
    /// </summary>
    public static class StatSummary
    {
        public static TreeSummary Build(PassiveTreeDocument document, AllocationState allocation, BaseStatProfile baseStats)
        {
            var accumulators = new Dictionary<EntityParameter, ParameterAccumulator>();
            var knobs = new Dictionary<(ContextParameter Parameter, ModifierValueType ValueType), ContextAccumulator>();
            var summary = new TreeSummary();

            foreach (string id in allocation.Taken)
            {
                PassiveNode? node = document.Find(id);
                if (node is null) continue;

                Describe(node, summary);
                foreach (ContextModifierLine line in node.ContextModifiers) Accumulate(line, knobs, summary);

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

            foreach (KeyValuePair<(ContextParameter Parameter, ModifierValueType ValueType), ContextAccumulator> pair in knobs)
                summary.Context.Add(pair.Value.ToTotal(pair.Key.Parameter, pair.Key.ValueType));

            summary.Context.Sort(static (first, second) =>
            {
                int byKnob = string.CompareOrdinal(first.Parameter.ToString(), second.Parameter.ToString());
                return byKnob != 0 ? byKnob : first.ValueType.CompareTo(second.ValueType);
            });

            return summary;
        }

        /// <summary>
        /// Context lines are summed per knob and per bucket — which is exactly what reaches a fighter,
        /// since a pipeline reads one modifier per knob and stacking one per node would compound them.
        /// The total stays out of the parameter table: no context knob takes part in parameter math.
        /// </summary>
        private static void Accumulate(
            ContextModifierLine line,
            Dictionary<(ContextParameter Parameter, ModifierValueType ValueType), ContextAccumulator> knobs,
            TreeSummary summary)
        {
            if (line.IsConditional) summary.ConditionalLines++;

            (ContextParameter Parameter, ModifierValueType ValueType) key = (line.Parameter, line.ValueType);
            if (!knobs.TryGetValue(key, out ContextAccumulator? accumulator))
            {
                accumulator = new ContextAccumulator();
                knobs[key] = accumulator;
            }

            accumulator.Add(line);
        }

        private static void Describe(PassiveNode node, TreeSummary summary)
        {
            if (node.Kind == PassiveNodeKind.Keystone)
            {
                string label = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;
                summary.Keystones.Add(string.IsNullOrWhiteSpace(node.Description) ? label : $"{label} — {node.Description}");
                return;
            }

            // An empty reference is something Check reports, not a blank entry in the totals.
            if (!NodeKindRules.For(node.Kind).RequiresAbility || string.IsNullOrWhiteSpace(node.AbilityId)) return;

            summary.UnlocksOf(node.Kind).Add(node.AbilityId);
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
                _modifiers.Add(new SimpleModifier(parameter, line.ValueType, line.Value, PassiveTreeDocument.ModifierSource));
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

        private sealed class ContextAccumulator
        {
            private float _value;
            private int _lines;
            private int _conditionalLines;

            public void Add(ContextModifierLine line)
            {
                _value += line.Value;
                _lines++;
                if (line.IsConditional) _conditionalLines++;
            }

            public ContextTotal ToTotal(ContextParameter parameter, ModifierValueType valueType) =>
                new(parameter, valueType, _value, _lines, _conditionalLines);
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

    /// <param name="Value">Sum of the taken lines in this bucket — what a fighter would end up carrying
    /// for the knob. A flag bucket sums to the number of switches taken, one being enough.</param>
    /// <param name="ConditionalLines">How many of the lines carry a condition and were counted as if
    /// always on.</param>
    public sealed record ContextTotal(
        ContextParameter Parameter,
        ModifierValueType ValueType,
        float Value,
        int Lines,
        int ConditionalLines);

    public sealed class TreeSummary
    {
        private readonly Dictionary<PassiveNodeKind, List<string>> _unlocks = [];

        public List<ParameterTotal> Parameters { get; } = [];

        /// <summary>Pipeline knobs, kept apart from <see cref="Parameters"/>: they never enter the
        /// parameter formula, so folding them into that table would be a lie about where they land.</summary>
        public List<ContextTotal> Context { get; } = [];

        public List<string> Keystones { get; } = [];

        /// <summary>Ability references grouped by the class of node that granted them. A class with
        /// nothing taken has no entry at all, so the panel never prints an empty row.</summary>
        public IReadOnlyDictionary<PassiveNodeKind, List<string>> Unlocks => _unlocks;

        public int ConditionalLines { get; set; }

        public bool IsEmpty => Parameters.Count == 0 && Context.Count == 0 && Keystones.Count == 0 && _unlocks.Count == 0;

        /// <summary>The write end of <see cref="Unlocks"/>: the list for a class, created on demand.</summary>
        public List<string> UnlocksOf(PassiveNodeKind kind)
        {
            if (!_unlocks.TryGetValue(kind, out List<string>? list))
            {
                list = [];
                _unlocks[kind] = list;
            }

            return list;
        }
    }
}
