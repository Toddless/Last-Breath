namespace Core.Components
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;

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
