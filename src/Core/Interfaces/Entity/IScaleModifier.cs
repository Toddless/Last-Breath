namespace Core.Interfaces.Entity
{
    public interface IScaleModifier : INpcModifier
    {
        float ScaleFactor { get; }
    }
}
