namespace DataEditor.Source.Io
{
    using System.IO;
    using Godot;

    /// <summary>
    /// Where the tool reads its catalogs from. The data root is a setting rather than a constant: run
    /// from the Godot editor it is the project's <c>Data/Shared</c> symlink, run as an exported
    /// executable it is a folder beside the binary.
    /// </summary>
    public static class ToolPaths
    {
        private const string SharedDataFolder = "Data/Shared/";

        /// <summary>The data root this run reads. Outside the editor the executable's own folder
        /// answers, not <c>res://</c>: an exported <c>res://</c> lives inside the pck, and the tool
        /// needs a folder it can write catalogs back into.</summary>
        public static string SharedDataRoot => OS.HasFeature("editor")
            ? ProjectSettings.GlobalizePath($"res://{SharedDataFolder}")
            : Combine(OS.GetExecutablePath().GetBaseDir(), SharedDataFolder);

        /// <summary>Normalised to forward slashes because the two halves disagree — Godot hands back a
        /// Godot path and <see cref="Path.Combine"/> appends the platform separator, so the mixed
        /// result is what would end up on screen.</summary>
        private static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');
    }
}
