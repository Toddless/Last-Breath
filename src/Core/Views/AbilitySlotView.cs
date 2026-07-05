namespace Core.Views
{
    using Godot;

    /// <summary>One node of the stance mastery tree: the ability's id, icon and unlock state.</summary>
    public record AbilitySlotView(string AbilityId, Texture2D? Icon, AbilityState State, int UnlockLevel);
}
