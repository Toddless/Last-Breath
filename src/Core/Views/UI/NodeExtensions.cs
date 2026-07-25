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
    }
}
