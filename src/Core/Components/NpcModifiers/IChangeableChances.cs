namespace Core.Components.NpcModifiers
{
    public interface IChangeableChances
    {
        float Multiplier { get; }
        int[] ChancesAffected { get; }
    }
}
