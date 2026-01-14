namespace LastBreath.Script.QuestSystem
{
    using Godot;
    using LastBreath.Components;

    public abstract partial class Condition : Resource
    {
        public abstract bool IsMet(PlayerProgress progress);
    }
}
