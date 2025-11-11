namespace Crafting.Source.Skills
{
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

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

        public override void Detach()
        {

        }

        protected override string GetDescription() => Localizator.LocalizeDescription(Id);
    }
}
