namespace Crafting.TestResources.Skills
{
    using Godot;
    using Utilities;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Entity;

    [Tool]
    [GlobalClass]
    public partial class EnhancedMastery : SkillBase
    {
        [Export] private string MasteryId { get; set; } = string.Empty;
        [Export] private string[] MasteryPool { get; set; } = [];
        [Export] private int MinBonusLevel { get; set; } = 1;
        [Export] private int MaxBonusLevel { get; set; }
        [Export] private int CurrentBonusLevel { get; set; }

        public override void Attach(IEntity owner)
        {

        }

        public override void Detach()
        {

        }

        protected override string GetDescription()
        {
            return Localizator.LocalizeDescriptionFormated(Id, [Localizator.Localize(MasteryId), CurrentBonusLevel]);
        }

        public override ISkill? Copy()
        {
            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            var copy = (EnhancedMastery)DuplicateDeep();

            copy.MasteryPool = [];
            copy.MasteryId = MasteryPool[rnd.RandiRange(0, MasteryPool.Length - 1)];
            copy.CurrentBonusLevel = rnd.RandiRange(MinBonusLevel, MaxBonusLevel);
            return copy;
        }
    }
}
