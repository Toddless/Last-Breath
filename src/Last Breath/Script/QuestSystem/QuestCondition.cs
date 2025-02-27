namespace Playground.Script.QuestSystem
{
    using Godot;
    using Playground.Components;

    public abstract partial class QuestCondition : Resource
    {
        public abstract bool IsMet(PlayerProgress progress);
    }
}
