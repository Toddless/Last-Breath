namespace Playground.Script.QuestSystem
{
    using Godot;
    using Godot.Collections;
    using Playground.Resource.Quests;

    [GlobalClass]
    public partial class QuestCollection : Resource
    {
        [Export]
        public Array<Quest> Quests { get; set; } = [];
    }
}
