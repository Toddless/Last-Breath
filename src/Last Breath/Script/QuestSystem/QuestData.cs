namespace LastBreath.Script.QuestSystem
{
    using Godot;
    using Godot.Collections;
    using LastBreath.Resource.Quests;

    [GlobalClass]
    public partial class QuestData : Resource
    {
        [Export]
        public Array<Quest> Quests { get; set; } = [];
    }
}
