namespace LastBreath.Npc
{
    using Core.Entity;
    using Godot;
    using World.Interactions;

    [GlobalClass]
    public partial class NpcInteractionTarget : InteractionTarget
    {
        protected override string StableObjectId => GetParent() is INpc npc ? "npc/" + npc.InstanceId : base.StableObjectId;
        public override bool IsAvailable => GetParent() is BaseNpc { IsAlive: true, IsFighting: false };
    }
}
