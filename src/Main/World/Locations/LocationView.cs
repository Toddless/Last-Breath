namespace LastBreath.World.Locations
{
    using Godot;

    internal sealed class LocationView
    {
        public LocationRoot Root { get; }
        public SubViewportContainer Container { get; }
        public SubViewport Viewport => (SubViewport)Root.GetViewport();

        public LocationView(LocationRoot root, SubViewportContainer container)
        {
            Root = root;
            Container = container;
        }

        public void Present(bool active)
        {
            Container.Visible = active;
            Viewport.GuiDisableInput = !active;
            Viewport.AudioListenerEnable2D = active;
        }

        public static LocationView Create(LocationRoot root, Node host, Vector2 size, SubViewport reference)
        {
            var container = new SubViewportContainer { Name = root.LocationId + "View", Stretch = true, Size = size, Visible = false };
            var viewport = new SubViewport
            {
                Name = root.LocationId + "Space", World2D = new World2D(),
                PhysicsObjectPicking = true, GuiDisableInput = true,
                CanvasItemDefaultTextureFilter = reference.CanvasItemDefaultTextureFilter,
                Msaa2D = reference.Msaa2D, UseHdr2D = reference.UseHdr2D
            };
            root.ProcessMode = Node.ProcessModeEnum.Disabled;
            viewport.AddChild(root);
            container.AddChild(viewport);
            host.AddChild(container);
            return new LocationView(root, container);
        }
    }
}
