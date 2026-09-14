namespace LastBreath.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.World.Interactions;
    using Godot;
    using World.Interactions;

    public partial class InteractionProbe : Node2D, IInteractionSource
    {
        public bool Available { get; set; } = true;
        public bool HasChoice { get; set; } = true;
        public int Executed { get; private set; }
        public IEnumerable<InteractionAction> Actions() => HasChoice
            ? [new("safe", "UI_Interaction_Open", Available, Available ? null : "UI_Interaction_Unavailable"),
               new("danger", "UI_Interaction_Travel", Available, ExplicitChoice: true)]
            : [new("danger", "UI_Interaction_Travel", Available, ExplicitChoice: true)];
        public Task<InteractionResult> Execute(string actionId)
        {
            Executed++;
            return Task.FromResult(InteractionResult.Completed);
        }
    }
}
