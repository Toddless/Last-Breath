namespace Crafting.TestResources.TestSkills
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces;
    using Utilities;

    [Tool]
    [GlobalClass]
    public partial class TouchOfGod : SkillBase
    {
        [Export] public EquipmentType EquipmentType { get; private set; }
        [Export] public int ManaCost { get; private set; }
        [Export] public int Duration { get; private set; }
        [Export] private int HealAmount { get; set; }

        public override void Attach(ICharacter owner)
        {

        }

        public override void Detach()
        {

        }

        protected override string GetDescription() => Localizator.LocalizeDescriptionFormated(Id, [HealAmount]);
    }
}
