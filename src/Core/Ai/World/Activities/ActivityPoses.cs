namespace Core.Ai.World.Activities
{
    /// <summary>Activity pose clip names (the body plays them via IWorldAgent.SetActivityPose;
    /// a missing clip degrades to Idle — poses are flavor, not logic).</summary>
    public static class ActivityPoses
    {
        public const string Rest = "Activity_Rest";
        public const string Sleep = "Activity_Sleep";
        public const string Work = "Activity_Work";
    }
}
