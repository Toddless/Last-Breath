namespace Core.Entity.NpcModifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity;

    public class NpcModifiersComponent(IFightable owner) : INpcModifiersComponent
    {
        private readonly List<INpcModifier> _modifiers = [];

        public IReadOnlyList<INpcModifier> AllModifiers => _modifiers;

        public event System.Action<INpcModifier>? ModifierAdded;

        public void AddModifiers(List<INpcModifier> modifiers) => modifiers.ForEach(AddModifier);

        public void AddModifier(INpcModifier modifier)
        {
            if (modifier.IsUnique && _modifiers.Any(x => x.Id == modifier.Id)) return;

            modifier.Attach(owner);
            _modifiers.Add(modifier);
            ModifierAdded?.Invoke(modifier);
        }

        public void RemoveModifier(string instanceId)
        {
            var modifier = _modifiers.FirstOrDefault(x => x.InstanceId == instanceId);
            modifier?.Detach(owner);
            if (modifier != null) _modifiers.Remove(modifier);
        }
    }
}
