namespace Core.Constants
{
    /// <summary>
    /// Canonical res:// roots of the shared asset tree — the single physical copy lives in
    /// SharedData/Assets and reaches each project through the Data/Shared symlink.
    /// NOTE: Crafting mounts the symlink at Internal/Data/Shared — prefix accordingly there.
    /// </summary>
    public static class AssetPaths
    {
        public const string Icons = "res://Data/Shared/Assets/Icons/";

        public static string AbilityIcon(string abilityId) => $"{Icons}{abilityId}.png";
    }
}
