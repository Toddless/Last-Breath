namespace Core.Modifiers
{
    using Enums;

    /// <summary>Identity of an item line for duplicate control: WHICH stat it touches, HOW, and WHEN it
    /// counts — never the rolled value, so "+35% damage" and "+33% damage" are one and the same line and
    /// may not sit on one item. Mirrors <see cref="SimpleModifier.Equals"/> plus the predicate the line is
    /// held up by: "+10% armor" and "+10% armor while wounded" are two lines, priced apart and rollable
    /// side by side. Scope stays out on purpose: in data it is a property of the value type (flat/inc are
    /// local, multi is global), so keying by it would only split lines that read identically to the player.</summary>
    public readonly record struct ModifierKey(ModifierChannel Channel, int Parameter, ModifierValueType ValueType, string? Condition = null)
    {
        private readonly string? _condition = Normalize(Condition);

        /// <summary>The predicate id, always in the catalog's own comparison form. Normalized by the
        /// property itself, so no way in — the positional constructor, a <c>with</c> expression, a factory
        /// below — can seat a key that differs from another only by how the id was written.</summary>
        public string? Condition
        {
            get => _condition;
            init => _condition = Normalize(value);
        }

        public static ModifierKey From(EntityParameter parameter, ModifierValueType valueType, string? condition = null) =>
            new(ModifierChannel.Entity, (int)parameter, valueType, Normalize(condition));

        public static ModifierKey From(ContextParameter parameter, ModifierValueType valueType, string? condition = null) =>
            new(ModifierChannel.Context, (int)parameter, valueType, Normalize(condition));

        public static ModifierKey From(IModifier modifier) =>
            From(modifier.EntityParameter, modifier.ModifierValueType, (modifier as SimpleModifier)?.ConditionId);

        public static ModifierKey From(ContextModifierEntry entry) => From(entry.Parameter, entry.ValueType);

        /// <summary>Atomic descriptors have a key; a composite has none HERE — its duplicate identity is the
        /// whole set of its parts' keys (see <see cref="LineIdentity"/>). Operation entries
        /// (<see cref="UpgradeLevelsDescriptor"/>) and grants are not lines at all.</summary>
        public static bool TryFrom(IModifierDescriptor descriptor, out ModifierKey key) => TryFrom(descriptor, null, out key);

        /// <summary>The same, for an entry that sits inside a composite: the parts of a bundle carry no
        /// predicate of their own (the root holds the one that gates them all), so the root's travels down
        /// as <paramref name="inherited"/> — otherwise a gated bundle would key exactly like a free one.
        /// It also OUTRANKS anything found below, the way the mint stamps it: a part is keyed by the gate
        /// it will actually be worn under, never by one the bundle it belongs to has overruled.</summary>
        public static bool TryFrom(IModifierDescriptor descriptor, string? inherited, out ModifierKey key)
        {
            switch (descriptor)
            {
                case ParameterDescriptor parameter:
                    key = From(parameter.Parameter, parameter.ValueType, inherited ?? parameter.Condition);
                    return true;
                case ContextDescriptor context:
                    key = From(context.Parameter, context.ValueType, inherited ?? context.Condition);
                    return true;
                default:
                    key = default;
                    return false;
            }
        }

        /// <summary>Condition ids are compared the way the catalog itself matches them — ignoring case —
        /// so the same predicate written in two files stays one condition here too. A blank id is no
        /// condition at all.</summary>
        private static string? Normalize(string? condition) =>
            string.IsNullOrWhiteSpace(condition) ? null : condition.Trim().ToLowerInvariant();
    }
}
