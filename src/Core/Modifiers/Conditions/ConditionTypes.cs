namespace Core.Modifiers.Conditions
{
    /// <summary>Discriminators matched against the record's "type" property. A new predicate adds a
    /// name here and a factory class — no consumer parses the record itself.</summary>
    public static class ConditionTypes
    {
        public const string ResourceThreshold = "ResourceThreshold";
        public const string ResourceState = "ResourceState";
        public const string Status = "Status";
        public const string Effect = "Effect";
        public const string Stance = "Stance";
    }

    /// <summary>Property names of a condition record, in one place: the format spec the data follows.</summary>
    public static class ConditionFields
    {
        /// <summary>Which predicate the entry builds — see <see cref="ConditionTypes"/>.</summary>
        public const string Type = "type";

        /// <summary>Inverts the predicate. Shared by every type; absent means false.</summary>
        public const string Negate = "negate";

        /// <summary>Health, Mana or Barrier — required by every predicate over a vital, and exactly one
        /// of them.</summary>
        public const string Resource = "resource";

        /// <summary>Threshold as a fraction of the resource maximum. Required; the top of the range is a
        /// band below one, because a threshold releases its line a band above itself.</summary>
        public const string Value = "value";

        /// <summary>Full or Empty; required.</summary>
        public const string State = "state";

        /// <summary>Status names and/or aggregate aliases, OR-ed into one mask.</summary>
        public const string Statuses = "statuses";

        /// <summary>Which effects are counted — see <see cref="EffectScope"/>; required.</summary>
        public const string Scope = "scope";

        /// <summary>Effect id counted by the Stacks scope; required by that scope only.</summary>
        public const string Id = "id";

        /// <summary>How many the scope must find; absent means one.</summary>
        public const string Count = "count";

        /// <summary>Dexterity, Strength or Intelligence.</summary>
        public const string Stance = "stance";
    }
}
