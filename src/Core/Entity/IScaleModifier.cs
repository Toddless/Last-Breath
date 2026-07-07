namespace Core.Entity
{
    public interface IScaleModifier : INpcModifier
    {
        float ScaleFactor { get; }
    }
}
