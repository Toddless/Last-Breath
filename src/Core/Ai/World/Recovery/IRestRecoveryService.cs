namespace Core.Ai.World.Recovery
{
    using System;
    using Entity;
    using Godot;

    /// <summary>
    /// Out-of-combat rest: every live, non-fighting participant standing inside a recovery zone
    /// regains health, mana and barrier at the configured per-game-minute rates. Zones are
    /// registered by scene nodes (campfire, spawn points); participants register themselves
    /// (player and NPC bodies). Ticked by the world heartbeat (NpcWorldDirector) with real delta.
    /// </summary>
    public interface IRestRecoveryService
    {
        /// <summary>Position is a delegate: the owner node moves rarely, but freed nodes must not
        /// be dereferenced — owners are OBLIGED to unregister on _ExitTree.
        /// <paramref name="canRest"/> gates who the zone heals (an NPC camp only rests those its
        /// owners don't consider an enemy); null = everyone (campfires).</summary>
        void RegisterZone(object owner, Func<Vector2> position, float radius, Func<IFightable, bool>? canRest = null);

        void UnregisterZone(object owner);

        void RegisterParticipant(IFightable entity, Func<Vector2> position);

        void UnregisterParticipant(IFightable entity);

        void Tick(float realDelta);
    }
}
