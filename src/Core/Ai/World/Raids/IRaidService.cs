namespace Core.Ai.World.Raids
{
    /// <summary>Ticked by NpcWorldDirector; owns the raid lifecycle. An active raid blocks saving.</summary>
    public interface IRaidService
    {
        bool IsRaidActive { get; }

        /// <summary>Seconds until raids may roll again; settable for the save-load path.</summary>
        float CooldownRemaining { get; set; }

        void Tick(float delta);
    }
}
