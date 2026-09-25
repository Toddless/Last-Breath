namespace Battle.Source.RequestHandlers
{
    using Core;
    using Core.Constants;
    using Godot;

    /// <summary>Where a view handler gets an ability's picture. One place, because more than one screen
    /// shows abilities and a missing file has to be reported the same way in all of them.</summary>
    internal static class AbilityArt
    {
        /// <summary>A missing icon is a report and an empty image, not an engine error.</summary>
        public static Texture2D? LoadIcon(string abilityId)
        {
            string path = AssetPaths.AbilityIcon(abilityId);
            if (ResourceLoader.Exists(path)) return ResourceLoader.Load<Texture2D>(path);

            Tracker.TrackNotFound($"Ability icon not found: {path}");
            GD.Print($"Ability icon not found: {path}");
            return null;
        }
    }
}
