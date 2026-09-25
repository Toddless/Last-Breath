namespace Core.Items.Grants
{
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Modifiers;

    public class ModifierGrant(string id, IReadOnlyList<IModifierInstance> modifiers) : IItemGrant
    {
        public string Id => id;

        public string Description
        {
            get;
        } = string.Empty;

        /// <summary>Read access for serialization (save system round-trips the grant).</summary>
        public IReadOnlyList<IModifierInstance> Modifiers => modifiers;

        public void Attach(IFightable owner)
        {
            foreach (var modifier in modifiers) modifier.ApplyTo(owner);
        }

        public void Detach(IFightable owner)
        {
            foreach (var modifier in modifiers) modifier.RemoveFrom(owner);
        }

        public IItemGrant Copy() => new ModifierGrant(id, modifiers.Select(modifier => modifier.Copy()).ToList());

        // Fresh instances with the scaled value as the new BaseValue (BaseValue is ctor-only);
        // roll provenance survives like on every re-mint path.
        public IItemGrant WithScaledValues(float factor) =>
            new ModifierGrant(id, modifiers.Select(IModifierInstance (modifier) =>
            {
                var scaled = new SimpleModifier(modifier.EntityParameter, modifier.ModifierValueType, modifier.BaseValue * factor, modifier.Source, modifier.Weight) { Scope = modifier.Scope };
                SimpleModifier.TransferStamps(modifier, scaled);
                return scaled;
            }).ToList());
    }
}
