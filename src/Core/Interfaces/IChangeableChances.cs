namespace Core.Interfaces
{
    public interface IChangeableChances
    {
        float Multiplier { get; }
        int[] ChancesAffected { get; }
    }
}
