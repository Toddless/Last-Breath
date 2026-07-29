namespace PassiveTreeEditor.Source.Io
{
    using System.IO;
    using Godot;

    /// <summary>
    /// Where the tool reads and writes. Everything resolves from <c>res://</c> through
    /// <see cref="ProjectSettings.GlobalizePath"/>, so the shared-data symlink is followed by the OS
    /// and the same path works for reading a catalog and for writing the file back.
    /// </summary>
    public static class ToolPaths
    {
        /// <summary>The single physical copy of the game's data, reached through the Data/Shared symlink.</summary>
        public static string SharedDataRoot => ProjectSettings.GlobalizePath("res://Data/Shared/");

        /// <summary>Default home of the tree file: a data catalog next to every other game catalog.
        /// Normalised to forward slashes because the two halves disagree — <c>GlobalizePath</c> hands
        /// back a Godot path and <c>Path.Combine</c> appends the platform separator, so the mixed
        /// result is what ends up quoted in the settings file and in the path field.</summary>
        public static string DefaultTreePath =>
            Path.Combine(SharedDataRoot, PassiveTreeFormat.Catalog, PassiveTreeFormat.DefaultFileName)
                .Replace('\\', '/');

        /// <summary>Tool-only settings (base stat profile, last opened file). Never game data — it
        /// lives in the project folder, not in SharedData.</summary>
        public static string SettingsPath => ProjectSettings.GlobalizePath("res://editor-settings.json");
    }
}
