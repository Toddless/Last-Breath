namespace Core.Entity.NpcModifiers
{
    public interface IChangeableChances
    {
        float Multiplier { get; }
        int[] ChancesAffected { get; }
    }
}
