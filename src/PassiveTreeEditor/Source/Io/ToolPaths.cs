namespace PassiveTreeEditor.Source.Io
{
    using System.IO;
    using Core.Data.GameData;
    using Core.PassiveTree;
    using Godot;

    /// <summary>
    /// Where the tool reads and writes. The data root is a setting rather than a constant: run from the
    /// Godot editor it is the project's <c>Data/Shared</c> symlink, run as an exported executable it is
    /// a folder beside the binary, and either one can be pointed elsewhere from the header.
    /// </summary>
    public static class ToolPaths
    {
        private const string SharedDataFolder = "Data/Shared/";
        private const string SettingsFileName = "editor-settings.json";

        /// <summary>The data root used until one is chosen. Outside the editor the executable's own
        /// folder answers, not <c>res://</c>: an exported <c>res://</c> lives inside the pck, and the
        /// tool needs a folder it can write a tree back into.</summary>
        public static string DefaultSharedDataRoot => OS.HasFeature("editor")
            ? ProjectSettings.GlobalizePath($"res://{SharedDataFolder}")
            : Combine(OS.GetExecutablePath().GetBaseDir(), SharedDataFolder);

        /// <summary>Tool-only settings (data root, base stat profile, last opened file). Never game
        /// data. Kept in the user directory: it is writable in both runs and it is the same folder for
        /// both, so a root chosen in the exported tool is the root the editor run starts on.</summary>
        public static string SettingsPath => ProjectSettings.GlobalizePath($"user://{SettingsFileName}");

        /// <summary>Where settings lived while the tool only ever ran from the editor. Read once, when
        /// the current file does not exist yet, so a session's preferences survive the move.</summary>
        public static string LegacySettingsPath => ProjectSettings.GlobalizePath($"res://{SettingsFileName}");

        /// <summary>Default home of the tree file: a data catalog next to every other game catalog.</summary>
        public static string DefaultTreePath(string dataRoot) =>
            Combine(dataRoot, DataCatalog.PassiveTree, PassiveTreeFormat.DefaultFileName);

        /// <summary>Normalised to forward slashes because the two halves disagree — Godot hands back a
        /// Godot path and <see cref="Path.Combine"/> appends the platform separator, so the mixed result
        /// is what would end up quoted in the settings file and in the path field.</summary>
        private static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');
    }
}
