namespace Playground.Script.Inventory
{
    using Godot;
    using Playground.Script.Items;
    using Playground.Script.UI;

    public abstract partial class BaseSlot<T> : TextureButton
        where T : Item
    {
        private const string MainLayer = "Main/MainLayer";
        protected ItemDescription? ItemDescription;

        [Export] protected Texture2D? DefaltTexture;

        public T? CurrentItem { get; protected set; }


        protected void OnMouseExit()
        {
            if (ItemDescription != null)
            {
                ItemDescription.QueueFree();
                ItemDescription = null;
            }
        }

        protected void OnMouseEnter()
        {
            if (CurrentItem == null || CurrentItem.GetItemStats().Count < 1) return;
            var description = ItemDescription.InitializeAsPackedScene().Instantiate<ItemDescription>();
            description.GlobalPosition = GetGlobalMousePosition() + new Vector2(50, 10);
            description.SetStats(CurrentItem.GetItemStats());
            description.SetItemImage(CurrentItem.FullImage);
            description.SetItemDescription(CurrentItem.Description?.Text ?? string.Empty);
            GetTree().Root.GetNode<CanvasLayer>(MainLayer).CallDeferred(MethodName.AddChild, description);
            ItemDescription = description;
        }
    }
}
