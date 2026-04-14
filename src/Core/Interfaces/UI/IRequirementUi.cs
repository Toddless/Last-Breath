namespace Core.Interfaces.UI
{
    using System;
    using Godot;

    public interface IRequirementUi
    {
        bool IsClickable { get; set; }
        string Id { get; set; }
        bool IsResource { get; set; }

        event Action<IRequirementUi>? LeftClick, RightClick;

        void SetIcon(Texture2D? icon);
        void SetDisplayText(string displayText, int have, int need);
    }
}
