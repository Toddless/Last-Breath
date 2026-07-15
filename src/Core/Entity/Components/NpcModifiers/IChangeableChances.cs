namespace Core.Entity.Components.NpcModifiers
{
    public interface IChangeableChances
    {
        float Multiplier { get; }
        int[] ChancesAffected { get; }
    }
}
