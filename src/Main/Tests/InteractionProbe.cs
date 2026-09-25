namespace LastBreath.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.World.Interactions;
    using Godot;
    using World.Interactions;

    public partial class InteractionProbe : Node2D, IInteractionSource
    {
        /// <summary>Whether both actions are enabled; a change drops the cached offer of the child targets.</summary>
        public bool Available
        {
            get;
            set
            {
                field = value;
                InvalidateOffers();
            }
        } = true;

        /// <summary>Whether the safe action is offered beside the dangerous one; a change drops the cached offer of the child targets.</summary>
        public bool HasChoice
        {
            get;
            set
            {
                field = value;
                InvalidateOffers();
            }
        } = true;

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

        /// <summary>Tells the child targets, which offer the probe's actions, that the offer changed.</summary>
        private void InvalidateOffers()
        {
            for (int i = 0; i < GetChildCount(); i++)
                if (GetChild(i) is InteractionTarget target) target.InvalidateOffer();
        }
    }
}
