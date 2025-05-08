namespace Playground.Script.Items
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public partial class EquipItem : Item
    {
        protected const float From = 0.8f;
        protected const float To = 1.2f;

        protected RandomNumberGenerator Rnd = new();
        protected List<IModifier> BaseModifiers = [];
        protected List<IEffect> Effects = [];
        public ICharacter? Owner { get; private set; }

        public EquipmentPart EquipmentPart;

        public virtual void OnEquip(ICharacter owner)
        {
            Owner = owner;
            BaseModifiers.ForEach(Owner.Modifiers.AddTemporaryModifier);
            Effects.ForEach(Owner.Effects.AddTemporaryEffect);
        }

        public virtual void OnUnequip()
        {
            BaseModifiers.ForEach(Owner!.Modifiers.RemoveTemporaryModifier);
            Effects.ForEach(Owner.Effects.RemoveEffect);
            Owner = null;
        }

        protected virtual void LoadData(GlobalRarity rarity) { }
    }
}
