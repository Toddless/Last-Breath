namespace Crafting.Source.UIElements
{
    using System;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class RecipeTree : Tree
    {
        [Signal] public delegate void LeftClickEventHandler(string id);
        [Signal] public delegate void RightClickEventHandler(string id, TreeItem item);
        [Signal] public delegate void CtrLeftClickEventHandler(string id, TreeItem item);

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb && mb.Pressed)
            {
                var mousePosition = mb.Position;
                var item = GetItemAtPosition(mousePosition);
                if (item == null) return;
                var meta = item.GetMetadata(0).AsString();

                if (meta.Equals("category", StringComparison.OrdinalIgnoreCase)) return;

                switch (true)
                {
                    case var _ when mb.ButtonIndex == MouseButton.Left:
                        if (mb.CtrlPressed) EmitSignal(SignalName.CtrLeftClick, meta, item);
                        else EmitSignal(SignalName.LeftClick, meta);
                        break;
                    case var _ when mb.ButtonIndex == MouseButton.Right:
                        EmitSignal(SignalName.RightClick, meta, item);
                        break;
                }
                AcceptEvent();
            }
        }
    }
}
