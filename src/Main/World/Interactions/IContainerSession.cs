namespace LastBreath.World.Interactions
{
    using System;
    using Core.World.Interactions;

    /// <summary>An interaction session whose slots the player can take from.</summary>
    public interface IContainerSession : IInteractionSession
    {
        /// <summary>Moves one slot, or every slot for a null slot ID, into the player's bag while the accessibility check holds.</summary>
        InteractionResult Transfer(string? slotId, Func<bool> accessible);
    }
}
