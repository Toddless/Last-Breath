namespace Core.PassiveTree
{
    /// <summary>
    /// The node classes of the martial-arts tree. The name is what lands in JSON, so renaming a
    /// member is a data migration — parsing is strict and an unknown name fails the file loudly.
    /// </summary>
    public enum PassiveNodeKind
    {
        /// <summary>One or two modifier lines.</summary>
        Small,

        /// <summary>Cluster finisher: one to three modifier lines.</summary>
        Notable,

        /// <summary>Changes a rule rather than a number: free rule text plus optional lines.</summary>
        Keystone,

        /// <summary>Opens an ability. The tier-1 socket comes bundled with it and is not a node.</summary>
        AbilityUnlock,

        /// <summary>Opens the tier-2 socket of the referenced ability.</summary>
        SocketTier2,

        /// <summary>Opens the tier-3 socket of the referenced ability.</summary>
        SocketTier3,

        /// <summary>One of the three seeds: granted, not bought, and never spends a point.</summary>
        Start
    }
}
