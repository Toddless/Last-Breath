namespace LastBreath.Script.QuestSystem
{
    using Godot;
    using LastBreath.Components;

    [GlobalClass]
    public partial class NpcInteractionCondition : Condition
    {
        [Export(PropertyHint.File, "*.tres, *.res")]
        public string NpcNameKey { get; set; } = string.Empty;

        public override bool IsMet(PlayerProgress progress) => progress.InteractedNpcs.Contains(NpcNameKey);
    }
}
