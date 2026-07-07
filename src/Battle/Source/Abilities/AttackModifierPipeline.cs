namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;

    /// <summary>
    /// Ability-scoped pre-attack mutators (unevadable, first-attack-crit, ...), applied to every
    /// attack context of the series BEFORE it is scheduled. Shared by the attack-series abilities
    /// so each doesn't keep its own dictionary copy.
    /// </summary>
    public class AttackModifierPipeline
    {
        private readonly Dictionary<string, IAttackModifier> _modifiers = [];

        public void Add(IAttackModifier modifier) => _modifiers.TryAdd(modifier.Id, modifier);

        public void Remove(string id) => _modifiers.Remove(id);

        public void ApplyAll(IAttackContext context)
        {
            foreach (var modifier in _modifiers.Values.ToList())
                modifier.Apply(context);
        }
    }
}
