namespace Core.Views.UI
{
    using System.Collections.Generic;
    using Godot;

    public interface IItemUi
    {
        void SetItemName(string name);
        void SetItemDescription(string description);
        void SetItemRarity(string rarity);
        void SetItemPiece(string piece);
        void SetItemBaseStats(List<string> baseStats);
        void SetItemIcon(Texture2D? icon);
        void SetItemEffectName(string name);
        void SetItemEffectDescription(string description);
        void SetItemAdditionalStats(List<(string ModifierText, int Identifier)> additionalStats);
        void SetItemUpgradeLevel(string level);
    }
}
