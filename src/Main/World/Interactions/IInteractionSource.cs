namespace LastBreath.World.Interactions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.World.Interactions;

    public interface IInteractionSource
    {
        IEnumerable<InteractionAction> Actions();

        /// <summary>Runs the action within the request: the controller serves one command at a time, so the task never waits out the action's duration.
        /// An effect finished by the return reports Completed; anything that runs on continues as a session or its own process and reports Started.</summary>
        Task<InteractionResult> Execute(string actionId);
    }
}
