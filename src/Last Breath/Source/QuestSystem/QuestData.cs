namespace LastBreath.Source.QuestSystem
{
    using Godot;
    using Godot.Collections;

    [GlobalClass]
    public partial class QuestData : Resource
    {
        [Export]
        public Array<Quest> Quests { get; set; } = [];
    }
}
