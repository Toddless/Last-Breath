namespace Core.World.Spaces
{
    using Godot;

    /// <summary>Physical space membership is valid only while both nodes are in the scene tree.</summary>
    public static class SpatialAccess
    {
        public static bool SharesSpace(Node2D left, Node2D right) =>
            NativeSpatialQuery.Instance.SharesSpace(left, right);

        public static Node2D GetSpaceRoot(Node2D node)
        {
            var root = node;
            for (Node? ancestor = node.GetParent(); ancestor != null && ancestor is not Viewport && ancestor != node.GetTree().CurrentScene; ancestor = ancestor.GetParent())
                if (ancestor is Node2D candidate) root = candidate;
            return root;
        }

        public static bool CanReceiveInput(Node node) =>
            node.IsInsideTree() && !node.GetViewport().GuiDisableInput;

        public static bool HasTextFocus(Node node) =>
            node.GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit
            || node.GetTree().Root.GuiGetFocusOwner() is LineEdit or TextEdit;
    }
}
