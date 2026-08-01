namespace Core.Modifiers.Conditions
{
    /// <summary>Discriminators matched against the record's "type" property. A new predicate adds a
    /// name here and a factory class — no consumer parses the record itself.
    /// <para>The names starting with Target are read about the fighter the owner is hitting rather than
    /// about the owner; everything else about the owner. Which of the two a record builds is the record's
    /// business alone — a line carries an id and nothing more, so the two families share this list and the
    /// one catalog behind it.</para></summary>
    public static class ConditionTypes
    {
        public const string ResourceThreshold = "ResourceThreshold";
        public const string ResourceState = "ResourceState";
        public const string Status = "Status";
        public const string Effect = "Effect";
        public const string Stance = "Stance";
        public const string TargetResourceThreshold = "TargetResourceThreshold";
        public const string TargetStatus = "TargetStatus";
    }

    /// <summary>Property names of a condition record, in one place: the format spec the data follows.</summary>
    public static class ConditionFields
    {
        /// <summary>The array a catalog file holds its entries under — the one name that belongs to the
        /// file rather than to a record.</summary>
        public const string Entries = "conditions";

        /// <summary>What the entry is called in the catalog. A consumer carries this and nothing else,
        /// so the whole record is written once and read by everyone who names it.</summary>
        public const string Id = "id";

        /// <summary>Which predicate the entry builds — see <see cref="ConditionTypes"/>.</summary>
        public const string Type = "type";

        /// <summary>Inverts the predicate. Shared by every type; absent means false.</summary>
        public const string Negate = "negate";

        /// <summary>Health, Mana or Barrier — required by every predicate over a vital, and exactly one
        /// of them.</summary>
        public const string Resource = "resource";

        /// <summary>Threshold as a fraction of the resource maximum. Required; how much of the range is
        /// usable is the type's own answer — the owner-side threshold stops a band below one because it
        /// releases its line a band above itself, the target-side one takes the whole share.</summary>
        public const string Value = "value";

        /// <summary>Full or Empty; required.</summary>
        public const string State = "state";

        /// <summary>Status names and/or aggregate aliases, OR-ed into one mask.</summary>
        public const string Statuses = "statuses";

        /// <summary>Which effects are counted — see <see cref="EffectScope"/>; required.</summary>
        public const string Scope = "scope";

        /// <summary>Which effect the Stacks scope counts; required by that scope only. Named apart from
        /// <see cref="Id"/> because both live in the same flat record and mean different things: one is
        /// what the entry is called, the other is what it looks for.</summary>
        public const string EffectId = "effectId";

        /// <summary>How many the scope must find; absent means one.</summary>
        public const string Count = "count";

        /// <summary>Dexterity, Strength or Intelligence.</summary>
        public const string Stance = "stance";
    }
}
