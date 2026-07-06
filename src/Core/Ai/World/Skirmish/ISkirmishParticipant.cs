namespace Core.Ai.World.Skirmish
{
    using Enums;
    using Godot;
    using Interfaces.Entity;

    /// <summary>
    /// An NPC as the abstract NPC-vs-NPC combat sees it: the strength inputs (level, rarity,
    /// type), the world position and the two ways a lost skirmish ends — dying into the
    /// body lifecycle or being burned by the winners.
    /// </summary>
    public interface ISkirmishParticipant
    {
        string InstanceId { get; }
        Fractions Fraction { get; }
        int Level { get; }
        Rarity Rarity { get; }
        EntityType EntityType { get; }
        Vector2 Position { get; }
        bool IsFighting { get; set; }
        bool IsAlive { get; }
        IEntityGroup? Group { get; }

        /// <summary>Dies outside a player battle: health to zero, the body lifecycle takes over.</summary>
        void DefeatInWorld();

        /// <summary>Burns the lying body (final death). False when there is nothing to burn.</summary>
        bool TryBurnBody();
    }
}
