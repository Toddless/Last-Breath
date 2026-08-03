namespace PassiveTreeEditor.Source.Validation
{
    /// <summary>
    /// What a finding is about. The kind is what the tool sorts and groups by; the sentence shown to
    /// the author lives on the finding itself, so wording never becomes something a caller has to know.
    /// </summary>
    public enum TreeIssueKind
    {
        /// <summary>A seed the design calls for is not in the tree: the core, or the one belonging to
        /// a stance. It has no node to point at — the finding is about the seed that is missing.</summary>
        StartPointMissing,

        /// <summary>A second start point where the design has one — a second core, or a second seed on
        /// the same stance.</summary>
        StartPointExtra,

        /// <summary>No start point at all — nothing in the tree can be reached, so reachability is
        /// reported once here instead of once per node.</summary>
        NoStartPoint,

        /// <summary>Two nodes answer to the same id.</summary>
        DuplicateId,

        /// <summary>A class that must name an ability names none.</summary>
        AbilityMissing,

        /// <summary>The named ability is in no catalog the tool read.</summary>
        AbilityUnknown,

        /// <summary>The named ability exists but is internal-cast only and never appears in trees.</summary>
        AbilityHidden,

        /// <summary>A class that references no ability carries a reference anyway — what a kind change
        /// leaves behind.</summary>
        AbilityStray,

        /// <summary>A socket opens a slot on an ability nothing in the tree unlocks — neither an
        /// unlock node nor a stance seed.</summary>
        SocketWithoutUnlock,

        /// <summary>The node has no edges at all.</summary>
        Detached,

        /// <summary>The node has edges, but no chain of them reaches a start point.</summary>
        Unreachable,

        /// <summary>Fewer modifier lines than the class calls for.</summary>
        TooFewLines,

        /// <summary>More modifier lines than the class allows.</summary>
        TooManyLines,

        /// <summary>A keystone with no rule text — the whole of what a keystone is.</summary>
        KeystoneWithoutRule
    }

    /// <param name="Kind">Which rule produced the finding.</param>
    /// <param name="NodeId">The node the finding is about; empty when it is about the tree itself and
    /// there is nowhere to jump to.</param>
    /// <param name="Message">The sentence shown to the author, without the node id in front of it.</param>
    public readonly record struct TreeIssue(TreeIssueKind Kind, string NodeId, string Message)
    {
        public bool HasNode => NodeId.Length > 0;

        /// <summary>The finding as one line: the node it belongs to, then what is wrong with it.</summary>
        public string Text => HasNode ? $"{NodeId}: {Message}" : Message;
    }
}
