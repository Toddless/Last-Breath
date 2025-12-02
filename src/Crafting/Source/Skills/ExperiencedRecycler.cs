namespace Crafting.Source.Skills
{
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

    [Tool]
    [GlobalClass]
    public partial class ExperiencedRecycler : SkillBase
    {
        [Export] private int Percent { get; set; }

        public override void Attach(IEntity owner)
        {

        }

        public override void Detach(IEntity owner)
        {

        }

        protected override string GetDescription()
        {
            return Localizator.LocalizeDescriptionFormated(Id, [Percent]);
        }
    }
}
