namespace Core.Enums
{
    /// <summary>How a chance roll is settled: <see cref="Lucky"/> and <see cref="Unlucky"/> roll twice and keep
    /// the better and the worse draw respectively. Values are signed so sources of both kinds cancel one another.</summary>
    public enum ChanceLuck
    {
        Neutral = 0,
        Lucky = 1,
        Unlucky = -1
    }
}
