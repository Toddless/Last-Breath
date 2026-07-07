namespace Crafting.Source.UIElements
{
    using System.Linq;
    using Core.Items;
    using Core.Localization;
    using Core.Views.UI;

    public class ItemDemoConfiguration(IEquipItem item) : IItemUiConfiguration
    {
        public void Configure(IItemUi ui)
        {
            ui.SetItemName(item.DisplayName);
            ui.SetItemUpgradeLevel(item.UpdateLevel > 0 ? $"+{item.UpdateLevel}" : string.Empty);
            ui.SetItemDescription(item.Description);
            ui.SetItemIcon(item.Icon);
            ui.SetItemPiece(Localization.Localize(item.EquipmentPiece.ToString()));
            ui.SetItemRarity(Localization.Localize(item.Rarity.ToString()));
            ui.SetItemBaseStats(item.Implicits.Select(Localization.Format).ToList());
            ui.SetItemAdditionalStats(item.Modifiers.Select(it => (Localization.Format(it), it.GetHashCode())).ToList());
            ui.SetItemEffectName(Localization.Localize(item.ItemEffect));
            ui.SetItemEffectDescription(Localization.Localize(item.ItemEffect));
        }
    }
}
