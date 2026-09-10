namespace Core.World.Spaces
{
    using System;
    using Godot;

    public sealed record BattleSite(Guid BattleId, ulong SpaceId, Vector2 Position);

    /// <summary>The current battle's exploration-space anchor, independent of arena coordinates.</summary>
    public sealed class BattleSiteRegistry : Session.ISessionResettable
    {
        public BattleSite? Current { get; private set; }

        public void Register(BattleSite site)
        {
            if (site.BattleId == Guid.Empty || site.SpaceId == 0)
                throw new ArgumentException("A battle site requires a live space and battle identity.");
            if (Current != null && Current.BattleId != site.BattleId)
                throw new InvalidOperationException("Only one player battle may own a site.");
            Current = site;
        }

        public void Remove(Guid battleId)
        {
            if (Current?.BattleId == battleId) Current = null;
        }

        public void ResetSession() => Current = null;
    }
}
