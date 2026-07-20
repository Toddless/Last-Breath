namespace Core.Ai.World.SmartPoints
{
    using Godot;

    /// <summary>
    /// The world's directory of claimable spots. Activities claim in Enter and release in Exit;
    /// the death insurance (Exit never runs for the dead) is a bus subscription releasing by
    /// claimant id — wired where the registry is registered in DI.
    /// </summary>
    public interface ISmartPointRegistry
    {
        void Register(ISmartPoint point);
        void Unregister(ISmartPoint point);

        /// <summary>
        /// Nearest free point of the tag, claimed for the claimant. A claimant already holding a
        /// point of this tag keeps it (idempotent re-claim). Null = everything is taken or no
        /// such points exist — callers degrade to resting at home.
        /// </summary>
        ISmartPoint? TryClaim(string tag, string claimantId, Vector2 from);

        /// <summary>Drops every claim held by the claimant (activity exit, death insurance).</summary>
        void Release(string claimantId);
    }
}
