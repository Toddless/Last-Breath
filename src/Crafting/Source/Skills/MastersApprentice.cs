namespace Crafting.TestResources.Skills
{
    using Godot;
    using Utilities;
    using Core.Interfaces.Entity;

    [Tool]
    [GlobalClass]
    public partial class MastersApprentice : SkillBase
    {
        [Export] private int Percent {  get; set; }

        public override void Attach(IEntity owner)
        {

        }
            
        public override void Detach()
        {

        }

        protected override string GetDescription() => Localizator.LocalizeDescriptionFormated(Id, [Percent]);
    }
}
