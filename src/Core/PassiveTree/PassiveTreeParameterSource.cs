namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Entity.Components;
    using Enums;
    using Interfaces;
    using Modifiers;
    using Modifiers.Conditions;

    /// <summary>The parametric contribution of the taken nodes, published as a modifier source rather
    /// than written onto the entity — a respec is just a rebuild of this map plus a change notification,
    /// so no line ever has to be hunted down on the entity afterward.</summary>
    public sealed class PassiveTreeParameterSource(IConditionProvider conditions) : IParameterModifierSource
    {
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _modifiers = [];

        /// <summary>Conditional lines, kept apart because they're the only ones with anything to release —
        /// their predicates watch a fighter, and a silent drop would leave that watch running.</summary>
        private readonly List<ConditionalModifier> _conditional = [];

        private IFightable? _owner;

        public event Action<IReadOnlyCollection<EntityParameter>>? SourceChanged;

        /// <summary>A snapshot, not the live key set — a consumer's reaction could trigger another
        /// <see cref="Rebuild"/> and invalidate the collection mid-walk.</summary>
        public IReadOnlyCollection<EntityParameter> AffectedParameters => _modifiers.Keys.ToHashSet();

        public IEnumerable<IModifierInstance> GetModifiers(EntityParameter parameter) =>
            _modifiers.TryGetValue(parameter, out List<IModifierInstance>? lines) ? lines.ToArray() : [];

        /// <summary>Replaces the whole map with what the given nodes carry and announces every touched
        /// parameter — gained or newly empty — which is what makes a refund actually land on the entity.</summary>
        public void Rebuild(PassiveTreeDocument document, IEnumerable<string> taken)
        {
            var affected = new HashSet<EntityParameter>(_modifiers.Keys);
            Release();
            _conditional.Clear();
            _modifiers.Clear();

            foreach (string id in taken)
            {
                PassiveNode? node = document.Find(id);
                if (node is null) continue;

                foreach (ModifierLine line in node.Modifiers) Add(line, affected);
            }

            Bind();
            if (affected.Count > 0) SourceChanged?.Invoke(affected);
        }

        /// <summary>Points the conditional lines at the owning fighter (or releases them). This channel is
        /// pulled and knows the fighter only through this call, yet its predicates read that fighter's
        /// state, so the sighting must arrive from the pushed half. Announces only the conditional lines'
        /// own parameters afterward — everything else resolves the same regardless of owner.</summary>
        public void Follow(IFightable? owner)
        {
            if (ReferenceEquals(_owner, owner)) return;

            Release();
            _owner = owner;
            Bind();

            var affected = new HashSet<EntityParameter>(_conditional.Select(modifier => modifier.EntityParameter));
            if (affected.Count > 0) SourceChanged?.Invoke(affected);
        }

        /// <summary>An aggregate line stores under the aggregate itself — the modifier component folds it
        /// into every family member at resolve time, so expanding it here would double-count it. A line
        /// naming an unknown condition is dropped whole, never kept as unconditional.</summary>
        private void Add(ModifierLine line, HashSet<EntityParameter> affected)
        {
            // A flag carries no number and takes no part in the parametric formula.
            if (line.ValueType == ModifierValueType.Flag) return;
            if (!conditions.TryResolve(line.Condition, out ICondition? condition)) return;

            if (!_modifiers.TryGetValue(line.Parameter, out List<IModifierInstance>? lines))
            {
                lines = [];
                _modifiers[line.Parameter] = lines;
            }

            lines.Add(condition is null ? line.ToModifier() : Conditional(line, condition));
            affected.Add(line.Parameter);
        }

        /// <summary>A line that counts only while its condition holds. Lives in the same map as every other
        /// line, never on the fighter's own list — only reachable when he resolves the value.</summary>
        private IModifierInstance Conditional(ModifierLine line, ICondition condition)
        {
            var modifier = new ConditionalModifier(weight: 0f, line.ValueType, line.Parameter, line.Value, condition, PassiveTreeDocument.ModifierSource);
            _conditional.Add(modifier);

            return modifier;
        }

        private void Bind()
        {
            if (_owner is not { } owner) return;

            foreach (ConditionalModifier modifier in _conditional) modifier.Bind(owner);
        }

        private void Release()
        {
            foreach (ConditionalModifier modifier in _conditional) modifier.Unbind();
        }
    }
}
