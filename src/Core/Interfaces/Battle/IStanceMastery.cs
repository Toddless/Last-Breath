namespace Core.Interfaces.Battle
{
    using Enums;

    public interface IStanceMastery : IMastery
    {
        float GetValueForParameter(float baseValue);
    }
}
