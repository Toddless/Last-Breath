namespace LastBreath.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.World.Interactions;
    using Godot;
    using World.Interactions;

    public partial class InteractionProbe : Node2D, IInteractionSource
    {
        private const string SafeLabelKey = "UI_Interaction_Open";
        private const string DangerLabelKey = "UI_Interaction_Travel";
        private const string WindowLabelKey = "UI_Interaction_Talk";
        private readonly List<string> _executed = [];
        public const string SafeAction = "safe";
        public const string DangerAction = "danger";
        public const string WindowAction = "window";

        /// <summary>Whether every action is enabled; a change drops the cached offer of the child targets.</summary>
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

        /// <summary>Whether the dangerous action is listed before the safe one; a change drops the cached offer of the child targets.</summary>
        public bool DangerFirst
        {
            get;
            set
            {
                field = value;
                InvalidateOffers();
            }
        }

        /// <summary>Reason the safe action gives while disabled; a change drops the cached offer of the child targets.</summary>
        public string ReasonKey
        {
            get;
            set
            {
                field = value;
                InvalidateOffers();
            }
        } = InteractionReasonKeys.Unavailable;

        /// <summary>What the window action runs, listed last while set; a change drops the cached offer of the child targets.</summary>
        public Func<InteractionResult>? OpenWindow
        {
            get;
            set
            {
                field = value;
                InvalidateOffers();
            }
        }

        /// <summary>IDs of the executed actions in execution order.</summary>
        public IReadOnlyList<string> ExecutedActions => _executed;

        public int Executed => _executed.Count;

        /// <summary>Tells the child targets, which offer the probe's actions, that the offer changed.</summary>
        private void InvalidateOffers()
        {
            for (int i = 0; i < GetChildCount(); i++)
                if (GetChild(i) is InteractionTarget target) target.InvalidateOffer();
        }

        public IEnumerable<InteractionAction> Actions()
        {
            var danger = new InteractionAction(DangerAction, DangerLabelKey, Available, ExplicitChoice: true);
            if (DangerFirst) yield return danger;
            if (HasChoice) yield return new(SafeAction, SafeLabelKey, Available, Available ? null : ReasonKey);
            if (!DangerFirst) yield return danger;
            if (OpenWindow != null) yield return new(WindowAction, WindowLabelKey, Available);
        }

        public Task<InteractionResult> Execute(string actionId)
        {
            _executed.Add(actionId);
            return Task.FromResult(actionId == WindowAction && OpenWindow is { } open ? open() : InteractionResult.Completed);
        }
    }
}
