namespace LastBreath.World.Interactions
{
    /// <summary>A node that gives its identity and availability to every interaction target whose nearest owner ancestor it is.</summary>
    public interface IInteractionOwner
    {
        /// <summary>Object ID of the owner's targets, prefix included; empty when the owner has no valid identity.</summary>
        string InteractionId { get; }

        /// <summary>Whether the owner can be interacted with now.</summary>
        bool IsInteractable { get; }
    }
}
