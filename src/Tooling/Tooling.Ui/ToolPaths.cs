namespace Tooling.Ui
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

        /// <summary>The catalog of the game's own wording, which sits among the data catalogs and is
        /// read like one.</summary>
        private const string LocalizationFolder = "Localization";

        /// <summary>The data root this run reads. Outside the editor the executable's own folder
        /// answers, not <c>res://</c>: an exported <c>res://</c> lives inside the pck, and the tool
        /// needs a folder it can write catalogs back into.</summary>
        public static string SharedDataRoot => OS.HasFeature("editor")
            ? ProjectSettings.GlobalizePath($"res://{SharedDataFolder}")
            : Combine(OS.GetExecutablePath().GetBaseDir(), SharedDataFolder);

        /// <summary>Where the .po files this run edits live: beside the catalogs, under the same root, so
        /// a run reading experimental data edits the wording shipped with it.</summary>
        public static string LocalizationRoot => Combine(SharedDataRoot, LocalizationFolder);

        /// <summary>Normalised to forward slashes because the two halves disagree — Godot hands back a
        /// Godot path and <see cref="Path.Combine"/> appends the platform separator, so the mixed
        /// result is what would end up on screen.</summary>
        private static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');
    }
}
