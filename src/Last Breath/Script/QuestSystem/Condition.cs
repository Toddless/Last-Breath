namespace Playground.Script.QuestSystem
{
    using Godot;
    using Playground.Components;

    public abstract partial class Condition : Resource
    {
        public abstract bool IsMet(PlayerProgress progress);
    }
}
