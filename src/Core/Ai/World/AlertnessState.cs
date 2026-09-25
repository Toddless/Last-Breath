namespace Core.Ai.World
{
    /// <summary>
    /// World-mode alertness ladder (STALKER-like). Battle is not a brain state:
    /// while the entity fights, the brain is suspended entirely.
    /// </summary>
    public enum AlertnessState : byte
    {
        /// <summary>Default life: runs the assigned activity (idle/wander/patrol).</summary>
        Calm,

        /// <summary>Heard something: walks to the stimulus point and looks around.</summary>
        Suspicious,

        /// <summary>Sees the target: chases it until contact, sight loss or the leash limit.</summary>
        Alert,

        /// <summary>Lost the target: lingers around the last known position, then calms down.</summary>
        Search,

        /// <summary>Non-aggressive NPC running away from a threat until it feels safe again.</summary>
        Flee
    }
}
