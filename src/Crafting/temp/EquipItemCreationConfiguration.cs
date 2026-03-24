namespace Crafting.TestResources
{
    using Utilities;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces.UI;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class EquipItemCreationConfiguration(IEquipItem item, ICraftingMastery mastery) : IEquipItemUiConfiguration
    {
        public void Configure(IEquipItemUi ui)
        {
            ui.SetItemName(item.DisplayName);
            ui.SetItemDescription(item.Description);
            ui.SetItemRarity(FormatRarity(item.Rarity));
            ui.SetItemPiece(Localization.Localize(item.EquipmentPiece.ToString()));
            ui.SetItemBaseStats(FormatItemModifiers(item.BaseModifiers).ConvertAll(valueTuple => valueTuple.ModifierText));
            ui.SetItemIcon(item.Icon);
            ui.SetItemEffectName(Localization.Localize(item.ItemEffect));
            ui.SetItemEffectDescription(Localization.LocalizeDescription(item.ItemEffect));
            ui.SetItemAdditionalStats(FormatAdditionalModifiers());
        }

        private string FormatRarity(Rarity itemRarity) => itemRarity switch
        {
            Rarity.Common => "???",
            _ => Localization.Localize(itemRarity.ToString())
        };

        private List<(string ModifierText, int Identifier)> FormatItemModifiers(IReadOnlyList<IModifier> itemModifiers)
        {
            var formatedModifiers = new List<(string ModifierText, int Identifier)>();

            float currentValueMultiplier = mastery.GetCurrentValueMultiplier();
            float minValueMultiplier = mastery.GetCurrentMinRange();
            float maxValueMultiplier = mastery.GetCurrentMaxRange();

            foreach (IModifier modifier in itemModifiers)
            {
                modifier.Value = currentValueMultiplier * modifier.BaseValue;
                formatedModifiers.Add((Localization.Format(modifier, minValueMultiplier, maxValueMultiplier), modifier.GetHashCode()));
            }

            return formatedModifiers;
        }


        private List<(string ModifierText, int Identifier)> FormatAdditionalModifiers() => item.AdditionalModifiers.Count == 0
            ? [("???", 1), ("???", 2), ("???", 3), ("???", 4)]
            : FormatItemModifiers(item.AdditionalModifiers);
    }
}
