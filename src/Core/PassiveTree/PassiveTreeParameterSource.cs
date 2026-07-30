namespace Core.PassiveTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity.Components;
    using Enums;
    using Modifiers;

    /// <summary>
    /// The parametric contribution of the taken nodes, published as a modifier source instead of as
    /// modifiers written onto the entity. The entity asks this object what it holds at the moment it
    /// resolves a value, so a respec is a rebuild of this map plus a change notification: no line has
    /// to be hunted down on the entity afterwards, and the tree cannot leave a contribution behind.
    /// </summary>
    public sealed class PassiveTreeParameterSource : IParameterModifierSource
    {
        private readonly Dictionary<EntityParameter, List<IModifierInstance>> _modifiers = [];

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
            _modifiers.Clear();

            foreach (string id in taken)
            {
                PassiveNode? node = document.Find(id);
                if (node is null) continue;

                foreach (ModifierLine line in node.Modifiers) Add(line, affected);
            }

            if (affected.Count > 0) SourceChanged?.Invoke(affected);
        }

        /// <summary>
        /// An aggregate line is stored under the aggregate itself: the modifier component folds an
        /// aggregate bucket into every family member while resolving a value, so expanding "+2 to all
        /// attributes" into three lines here would count it twice.
        /// </summary>
        private void Add(ModifierLine line, HashSet<EntityParameter> affected)
        {
            // A flag carries no number and takes no part in the parametric formula.
            if (line.ValueType == ModifierValueType.Flag) return;

            if (!_modifiers.TryGetValue(line.Parameter, out List<IModifierInstance>? lines))
            {
                lines = [];
                _modifiers[line.Parameter] = lines;
            }

            lines.Add(new SimpleModifier(line.Parameter, line.ValueType, line.Value, PassiveTreeDocument.ModifierSource));
            affected.Add(line.Parameter);
        }
    }
}
