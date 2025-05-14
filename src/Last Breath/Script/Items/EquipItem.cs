namespace Playground.Script.Items
{
    using System.Collections.Generic;
    using System.Text;
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
        public EquipmentPart EquipmentPart { get; protected set; }

        public virtual void OnEquip(ICharacter owner)
        {
            Owner = owner;
            BaseModifiers.ForEach(Owner.Modifiers.AddTemporaryModifier);
            Effects.ForEach(Owner.Effects.AddTemporaryEffect);
        }

        public virtual void OnUnequip()
        {
            BaseModifiers.ForEach(Owner.Modifiers.RemoveTemporaryModifier);
            Effects.ForEach(Owner.Effects.RemoveEffect);
            Owner = null;
        }

        public override List<string> GetItemStats()
        {
            List<string> list = [];
            foreach (var modifier in BaseModifiers)
            {
                StringBuilder stringBuilder = new();
                stringBuilder.Append(modifier.Parameter);
                stringBuilder.Append(':');
                stringBuilder.Append(' ');
                stringBuilder.Append(modifier.Value);
                list.Add(stringBuilder.ToString());
            }
            return list;
        }

        public virtual void UpgradeItemLevel() { }
        protected virtual void UpdateItem() { }
        protected virtual void LoadData() { }
        protected virtual void SetEffects() { }

    }
}
