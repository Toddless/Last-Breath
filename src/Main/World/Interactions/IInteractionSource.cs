namespace LastBreath.World.Interactions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.World.Interactions;

    public interface IInteractionSource
    {
        IEnumerable<InteractionAction> Actions();
        Task<InteractionResult> Execute(string actionId);
    }
}
