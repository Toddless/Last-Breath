namespace LastBreath.World.Interactions
{
    using System.Linq;
    using Godot;

    /// <summary>Setup check shared by nodes that own their interaction target and offer their actions through it.</summary>
    public static class InteractionTargetSetup
    {
        private const string MissingTargetFormat = "{0} '{1}' ('{2}') is disabled: it needs its interaction target set in the scene";
        private const string ForeignTargetFormat = "{0} '{1}' ('{2}') is disabled: its interaction target '{3}' is not owned by it";
        private const string UnservedTargetFormat =
            "{0} '{1}' ('{2}') is disabled: its interaction target '{3}' is not its direct child, so it never offers its actions";

        /// <summary>Why the target cannot serve the node, for the node's report; null when the node owns it and offers its actions through it.</summary>
        public static string? FindProblem<T>(T node, InteractionTarget? target, string kind, string id)
            where T : Node, IInteractionOwner, IInteractionSource
        {
            if (target == null) return string.Format(MissingTargetFormat, kind, node.GetPath(), id);
            if (!ReferenceEquals(target.FindInteractionOwner(), node)) return string.Format(ForeignTargetFormat, kind, node.GetPath(), id, target.Name);
            return target.Sources.Contains(node) ? null : string.Format(UnservedTargetFormat, kind, node.GetPath(), id, target.Name);
        }
    }
}
