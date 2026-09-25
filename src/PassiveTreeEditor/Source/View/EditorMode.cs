namespace PassiveTreeEditor.Source.View
{
    /// <summary>What a left click means right now. Explicit modes beat modifier-key combinations
    /// here: the tool is used for long layout sessions, and a visible mode is a visible state.</summary>
    public enum EditorMode
    {
        /// <summary>Pick, move and box-select nodes.</summary>
        Select,

        /// <summary>Drop a new node of the chosen class.</summary>
        Add,

        /// <summary>Click two nodes to connect or disconnect them.</summary>
        Link,

        /// <summary>Spend points as the player would.</summary>
        Simulate
    }
}
