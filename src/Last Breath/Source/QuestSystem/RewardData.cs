namespace LastBreath.Source.QuestSystem
{
    using Godot;
    using Godot.Collections;

    [GlobalClass]
    public partial class RewardData : Resource
    {
        [Export]
        public Array<Reward> Rewards { get; set; } = [];
    }
}
