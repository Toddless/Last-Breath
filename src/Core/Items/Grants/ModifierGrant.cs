namespace Core.Items.Grants
{
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces.Entity;
    using Interfaces.Items;
    using Modifiers;

    public class ModifierGrant(string id, IReadOnlyList<IModifierInstance> modifiers) : IItemGrant
    {
        public string Id => id;

        public void Attach(IFightable owner)
        {
            foreach (var modifier in modifiers) modifier.ApplyTo(owner);
        }

        public void Detach(IFightable owner)
        {
            foreach (var modifier in modifiers) modifier.RemoveFrom(owner);
        }

        public IItemGrant Copy() => new ModifierGrant(id, modifiers.Select(modifier => modifier.Copy()).ToList());
    }
}
