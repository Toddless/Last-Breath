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

    /// <summary>
    /// The parametric contribution of the taken nodes, published as a modifier source instead of as
    /// modifiers written onto the entity. The entity asks this object what it holds at the moment it
    /// resolves a value, so a respec is a rebuild of this map plus a change notification: no line has
    /// to be hunted down on the entity afterwards, and the tree cannot leave a contribution behind.
    /// </summary>
    public sealed class PassiveTreeParameterSource(IConditionProvider conditions) : IParameterModifierSource
    {
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _modifiers = [];

        /// <summary>The conditional lines of the current contribution, kept apart because they are the only
        /// ones with anything to release: their predicates watch a fighter, and a rebuild that dropped them
        /// without a word would leave that watch running for a line nobody counts any more.</summary>
        private readonly List<ConditionalModifier> _conditional = [];

        private IFightable? _owner;

        public event Action<IReadOnlyCollection<EntityParameter>>? SourceChanged;

        /// <summary>A snapshot, not the live key set: a consumer walks these to react to the source,
        /// and a reaction that ends in another <see cref="Rebuild"/> would invalidate the collection
        /// mid-walk. The same reason the map itself is never handed out.</summary>
        public IReadOnlyCollection<EntityParameter> AffectedParameters => _modifiers.Keys.ToHashSet();

        public IEnumerable<IModifierInstance> GetModifiers(EntityParameter parameter) =>
            _modifiers.TryGetValue(parameter, out List<IModifierInstance>? lines) ? lines.ToArray() : [];

        /// <summary>
        /// Replaces the whole map with what the given nodes carry and announces every parameter that
        /// was touched — both the ones that gained lines and the ones that just lost their last one,
        /// which is what makes a refund actually land on the entity.
        /// </summary>
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

        /// <summary>
        /// Points the conditional lines at the fighter carrying the contribution, or lets them go when it
        /// is carried by nobody. This half of the tree is pulled rather than handed over — it reaches a
        /// fighter as a registered source and knows him only through this call — yet its predicates read
        /// the state of that same fighter, so the sighting has to arrive from the half that is handed over.
        /// <para>Announced afterwards, and only for the parameters the conditional lines stand on: before
        /// the fighter is known a conditional line is met by nothing, so the values resolved until now were
        /// resolved without it. Everything else the contribution holds resolves to the same number either
        /// way, and every fighter a scene builds would otherwise re-resolve the whole of the tree for
        /// nothing.</para>
        /// </summary>
        public void Follow(IFightable? owner)
        {
            if (ReferenceEquals(_owner, owner)) return;

            Release();
            _owner = owner;
            Bind();

            var affected = new HashSet<EntityParameter>(_conditional.Select(modifier => modifier.EntityParameter));
            if (affected.Count > 0) SourceChanged?.Invoke(affected);
        }

        /// <summary>
        /// An aggregate line is stored under the aggregate itself: the modifier component folds an
        /// aggregate bucket into every family member while resolving a value, so expanding "+2 to all
        /// attributes" into three lines here would count it twice.
        /// <para>A line naming a condition the catalog does not hold is dropped whole rather than kept as
        /// an unconditional one — the refusal is the catalog's, and letting the line through would hand the
        /// character a bonus that was written to be paid for.</para>
        /// </summary>
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

        /// <summary>A line that only counts while its condition holds. It goes into the same map as every
        /// other line and is never written onto the fighter: value resolution skips it while it is off, and
        /// the flip refreshes the parameter — the modifier does not have to live in the fighter's own list
        /// for either, only to be reachable when he resolves the value.</summary>
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
