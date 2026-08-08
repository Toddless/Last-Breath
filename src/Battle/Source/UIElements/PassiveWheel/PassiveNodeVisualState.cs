namespace Battle.Source.UIElements.PassiveWheel
{
    /// <summary>What a node is to the character right now. Hover and selection are deliberately not
    /// here: they belong to the cursor, they move on every mouse motion, and they are drawn by one
    /// layer for every node instead of being pushed into each of them.</summary>
    public enum PassiveNodeVisualState
    {
        Idle,

        /// <summary>On the previewed route to the node under the cursor.</summary>
        OnPath,

        Taken
    }
}
