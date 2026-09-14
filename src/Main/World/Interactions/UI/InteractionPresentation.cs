namespace LastBreath.World.Interactions.UI
{
    using System.Linq;
    using Core.World.Interactions;
    using Godot;

    internal static class InteractionPresentation
    {
        public static Vector2 ScreenPoint(Node2D anchor)
        {
            var point = anchor.GetGlobalTransformWithCanvas().Origin;
            if (anchor.GetViewport().GetParent() is not SubViewportContainer container) return point;
            var viewportSize = anchor.GetViewport().GetVisibleRect().Size;
            var scale = new Vector2(container.Size.X / viewportSize.X, container.Size.Y / viewportSize.Y);
            return container.GetGlobalTransformWithCanvas() * (point * scale);
        }

        public static string Binding(string action)
        {
            var events = InputMap.ActionGetEvents(action);
            return events.FirstOrDefault()?.AsText() ?? action;
        }
    }
}
