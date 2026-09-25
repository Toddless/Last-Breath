namespace Core.Views.UI
{
    using Godot;

    public static class NodeExtensions
    {
        /// <summary>Queues every child for deletion — the standard "clear the container" move.</summary>
        public static void QueueFreeChildren(this Node node)
        {
            foreach (var child in node.GetChildren())
                child.QueueFree();
        }

        /// <summary>The node itself when it is a <typeparamref name="T"/>, else its nearest ancestor that is; null when none is.</summary>
        public static T? FindSelfOrAncestor<T>(this Node node) where T : class
        {
            for (Node? current = node; current != null; current = current.GetParent())
                if (current is T match) return match;
            return null;
        }
    }
}
