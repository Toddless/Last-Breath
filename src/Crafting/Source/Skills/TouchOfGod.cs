namespace Crafting.Source.Skills
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

    [Tool]
    [GlobalClass]
    public partial class TouchOfGod : SkillBase
    {
        [Export] public EquipmentType EquipmentType { get; private set; }
        [Export] public int ManaCost { get; private set; }
        [Export] public int Duration { get; private set; }
        [Export] private int HealAmount { get; set; }

        public override void Attach(IEntity owner)
        {

        }

        public override void Detach(IEntity owner)
        {

        }

        protected override string GetDescription() => Localizator.LocalizeDescriptionFormated(Id, [HealAmount]);
    }
}
