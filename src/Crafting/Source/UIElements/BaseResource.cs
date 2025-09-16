namespace Crafting.Source.UIElements
{
    using System;
    using Godot;


    public abstract partial class BaseResource : Control
    {
        protected string? ResourceId;
        protected Func<string, int>? AmountHave;
        protected Action<string, int>? ResourceConsumed;
        [Export] protected Control? Container;

        public virtual void AddCraftingResource(string resource, Func<string, int> amounHave, int amountNeed = 1)
        {
            ResourceId = resource;
            var templ = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
            templ.SetIcon(ItemDataProvider.Instance?.GetItemIcon(ResourceId));
            templ.SetText(ItemDataProvider.Instance?.GetItemDisplayName(ResourceId) ?? string.Empty, amounHave.Invoke(ResourceId), amountNeed);
            ResourceConsumed += (displayName, amountHave) =>
            {
                templ.SetText(displayName, amountHave, amountNeed);
            };
            AmountHave = amounHave;
            Container?.AddChild(templ);
        }

        public bool CanClear() => !string.IsNullOrWhiteSpace(ResourceId) && AmountHave?.Invoke(ResourceId) < 1;

        public virtual void ConsumeResource()
        {
            if (!string.IsNullOrWhiteSpace(ResourceId))
            {
                var displayName = ItemDataProvider.Instance?.GetItemDisplayName(ResourceId) ?? string.Empty;
                ResourceConsumed?.Invoke(displayName, AmountHave?.Invoke(ResourceId) ?? 0);
            }
        }

        public virtual void RemoveCraftingResource()
        {
            ResourceId = null;
            AmountHave = null;
            ResourceConsumed = null;
        }
    }
}
