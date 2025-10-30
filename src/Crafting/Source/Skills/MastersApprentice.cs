namespace Crafting.TestResources.Skills
{
    using Godot;
    using Core.Interfaces;
    using Utilities;

    [Tool]
    [GlobalClass]
    public partial class MastersApprentice : SkillBase
    {
        [Export] private int Percent {  get; set; }

        public override void Attach(ICharacter owner)
        {

        }
            
        public override void Detach()
        {

        }

        protected override string GetDescription() => Localizator.LocalizeDescriptionFormated(Id, [Percent]);
    }
}
