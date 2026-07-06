namespace Core.Ai
{
    /// <summary>One ability entry of a behavior profile: the base utility weight and the AI-facing role.</summary>
    public record AbilityBehavior(string AbilityId, float Weight, AbilityRole Role);
}
