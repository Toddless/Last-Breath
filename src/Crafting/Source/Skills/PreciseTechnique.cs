namespace Crafting.Source.Skills
{
    using Godot;
    using Utilities;
    using Core.Interfaces.Entity;

    [Tool]
    [GlobalClass]
    public partial class PreciseTechnique : SkillBase
    {
        [Export] public float Value { get; set; }
        [Export] public float Duration { get; set; }
        [Export] public bool Stackable { get; set; }

        public override void Attach(IEntity owner)
        {

        }

        public override void Detach(IEntity owner)
        {

        }

        protected override string GetDescription() => Localization.LocalizeDescription(Id);
    }
}
