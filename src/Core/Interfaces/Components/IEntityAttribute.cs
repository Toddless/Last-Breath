namespace Core.Interfaces.Components
{
    using Enums;
    using System.Collections.Generic;

    public interface IEntityAttribute
    {
        int Total { get; set; }
        int InvestedPoints { get; }
        IReadOnlyCollection<IModifierInstance> Modifiers { get; }

        void IncreasePoints();
        void IncreasePointsByAmount(int amount);
        void DecreasePoints();
        void DecreasePointsByAmount(int amount);
        void OnParameterChanges(EntityParameter parameter, float value);
    }
}
