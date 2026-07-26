namespace Core.Modifiers
{
    using Enums;

    /// <summary>Identity of an item line for duplicate control: WHICH stat it touches and HOW — never the
    /// rolled value, so "+35% damage" and "+33% damage" are one and the same line and may not sit on one
    /// item. Mirrors <see cref="SimpleModifier.Equals"/>. Scope stays out on purpose: in data it is a
    /// property of the value type (flat/inc are local, multi is global), so keying by it would only split
    /// lines that read identically to the player.</summary>
    public readonly record struct ModifierKey(ModifierChannel Channel, int Parameter, ModifierValueType ValueType)
    {
        public static ModifierKey From(EntityParameter parameter, ModifierValueType valueType) =>
            new(ModifierChannel.Entity, (int)parameter, valueType);

        public static ModifierKey From(ContextParameter parameter, ModifierValueType valueType) =>
            new(ModifierChannel.Context, (int)parameter, valueType);

        public static ModifierKey From(IModifier modifier) => From(modifier.EntityParameter, modifier.ModifierValueType);

        public static ModifierKey From(ContextModifierEntry entry) => From(entry.Parameter, entry.ValueType);

        /// <summary>Atomic descriptors have a key; a composite has none HERE — its duplicate identity is the
        /// whole set of its parts' keys (see <see cref="LineIdentity"/>). Operation entries
        /// (<see cref="UpgradeLevelsDescriptor"/>) and grants are not lines at all.</summary>
        public static bool TryFrom(IModifierDescriptor descriptor, out ModifierKey key)
        {
            switch (descriptor)
            {
                case ParameterDescriptor parameter:
                    key = From(parameter.Parameter, parameter.ValueType);
                    return true;
                case ContextDescriptor context:
                    key = From(context.Parameter, context.ValueType);
                    return true;
                default:
                    key = default;
                    return false;
            }
        }
    }
}
