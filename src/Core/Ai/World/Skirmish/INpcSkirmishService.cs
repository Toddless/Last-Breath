namespace Core.Ai.World.Skirmish
{

    /// <summary>Runs the abstract NPC-vs-NPC skirmishes; ticked by NpcWorldDirector.</summary>
    public interface INpcSkirmishService
    {
        /// <summary>Starts a skirmish between the two NPCs' squads if both sides are free and hostile.</summary>
        bool TryStart(ISkirmishParticipant initiator, ISkirmishParticipant target);

        void Tick(float delta);
    }
}
