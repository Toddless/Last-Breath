namespace PassiveTreeEditor.Source.Editing
{
    /// <summary>
    /// Names of the things an edit can change. One spelling each, because a field name is read twice:
    /// it decides whether two edits are the same edit continuing, and it is what the status line calls
    /// the step. Two spellings of one field would mean a run of keystrokes silently breaking into
    /// separate undo steps.
    /// </summary>
    public static class EditFields
    {
        public const string Id = "id";
        public const string Kind = "kind";
        public const string Stance = "stance";
        public const string Hybrid = "hybrid";
        public const string X = "x";
        public const string Y = "y";
        public const string Title = "title";
        public const string Description = "description";
        public const string Ability = "ability";
        public const string Budget = "budget";

        /// <summary>The passive a node grants, and the named numbers it is built from. The three property
        /// gestures are separate fields so that typing a name and stepping a number never merge into one
        /// undo step.</summary>
        public const string Passive = "passive";

        public const string PropertyName = "property name";
        public const string PropertyValue = "property value";
        public const string PropertyList = "properties";

        public const string Parameter = "parameter";
        public const string Knob = "knob";
        public const string ValueType = "value type";
        public const string Value = "value";
        public const string Condition = "condition";

        /// <summary>The two line channels, as the author sees them named on the buttons.</summary>
        public const string Line = "line";

        public const string Context = "context";
    }
}
