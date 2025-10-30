namespace Crafting.TestResources.Skills
{
    using Core.Interfaces;
    using Godot;
    using Utilities;

    [Tool]
    [GlobalClass]
    public partial class PreciseTechnique : SkillBase
    {
        [Export] public float Value { get; set; }
        [Export] public float Duration { get; set; }
        [Export] public bool Stackable { get; set; }

        public override void Attach(ICharacter owner)
        {

        }

        public override void Detach()
        {

        }

        protected override string GetDescription() => Localizator.LocalizeDescription(Id);
    }
}
