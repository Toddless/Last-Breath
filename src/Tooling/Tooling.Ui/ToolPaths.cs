namespace Tooling.Ui
{
    using System;
    using System.IO;
    using Godot;

    /// <summary>
    /// Where the tool reads its catalogs from. The data root is a setting rather than a constant: run
    /// from the Godot editor it is the project's <c>Data/Shared</c> symlink, run as an exported
    /// executable it is looked for beside the binary and then up the tree, and a run started with
    /// <c>--data &lt;path&gt;</c> reads the folder it was named.
    /// </summary>
    public static class ToolPaths
    {
        private const string SharedDataFolder = "Data/Shared/";

        /// <summary>The catalog of the game's own wording, which sits among the data catalogs and is
        /// read like one.</summary>
        private const string LocalizationFolder = "Localization";

        /// <summary>The switch that names the data root outright, for a binary that sits nowhere near
        /// one — a copy of the tool kept outside the repository, or a second checkout to edit.</summary>
        private const string RootArgument = "--data";

        /// <summary>Where the one physical copy of the catalogs lives in the repository. An exported tool
        /// is written under <c>build/tools/</c>, so walking up from the binary reaches the repository root
        /// and this folder under it: that is how a run from the repo reads what the game reads.</summary>
        private const string RepositoryDataFolder = "src/SharedData";

        /// <summary>How many folders up from the binary the repository is looked for. Three are enough
        /// for where the export writes; the rest is room for a copy moved a little deeper, and a cap so
        /// that a binary run from anywhere else stops instead of walking to the drive.</summary>
        private const int SearchDepth = 8;

        private const string NamedRootHint =
            "--data names a folder that is not there; point it at src/SharedData.";

        private const string EditorRootHint =
            "Data/Shared is a symlink to src/SharedData; restore-links.ps1 puts it back.";

        private const string ExportedRootHint =
            "An exported tool reads Data/Shared beside itself, then src/SharedData above it; " +
            "start it with --data <path> to name the folder outright.";

        /// <summary>The data root this run reads. Outside the editor the executable answers for it, not
        /// <c>res://</c>: an exported <c>res://</c> lives inside the pck, and the tool needs a folder it
        /// can write catalogs back into.</summary>
        public static string SharedDataRoot => NamedRoot ?? FoundRoot;

        /// <summary>Where the .po files this run edits live: beside the catalogs, under the same root, so
        /// a run reading experimental data edits the wording shipped with it.</summary>
        public static string LocalizationRoot => Combine(SharedDataRoot, LocalizationFolder);

        /// <summary>What to try when the root of this run is not there, worded for how the root was
        /// decided: a run told where to look and a run that had to find out are wrong about different
        /// things, and one sentence covering both names neither.</summary>
        public static string RootHint
        {
            get
            {
                if (NamedRoot is not null) return NamedRootHint;

                return InEditor ? EditorRootHint : ExportedRootHint;
            }
        }

        private static bool InEditor => OS.HasFeature("editor");

        /// <summary>
        /// The root this run was told to read, or null when it was told nothing. Taken as written, folder
        /// or not: a path that is not there is named back to the author, and quietly reading somewhere
        /// else instead is how a run edits the wrong copy of the data.
        /// <para>Asked of the user arguments — what follows <c>--</c> and the engine leaves alone — and
        /// then of the whole line, so that <c>Tool.exe --data &lt;path&gt;</c> and the separated form both
        /// arrive here.</para>
        /// </summary>
        private static string? NamedRoot => Named(OS.GetCmdlineUserArgs()) ?? Named(OS.GetCmdlineArgs());

        /// <summary>The root of a run that was not given one: the project's own symlink in the editor,
        /// and whatever an exported binary can find of the data from where it stands.</summary>
        private static string FoundRoot => InEditor
            ? ProjectSettings.GlobalizePath($"res://{SharedDataFolder}")
            : Exported(OS.GetExecutablePath().GetBaseDir());

        /// <summary>The path written after <paramref name="arguments"/>' <c>--data</c>, or null when the
        /// switch is absent or ends the line with nothing after it.</summary>
        private static string? Named(string[] arguments)
        {
            int at = Array.IndexOf(arguments, RootArgument);

            return at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : null;
        }

        /// <summary>Where an exported tool reads from: a data folder dropped beside the binary if there is
        /// one, else the repository's own copy above it. Falls back to the folder beside the binary, so a
        /// run that finds neither names a place the author can put the data in.</summary>
        private static string Exported(string folder)
        {
            string beside = Combine(folder, SharedDataFolder);

            return Directory.Exists(beside) ? beside : Repository(folder) ?? beside;
        }

        /// <summary>The repository's data folder at or above <paramref name="start"/>, or null when no
        /// folder on the way up holds one.</summary>
        private static string? Repository(string start)
        {
            string? folder = start;

            for (var step = 0; step < SearchDepth && folder is not null; step++)
            {
                string candidate = Combine(folder, RepositoryDataFolder);

                if (Directory.Exists(candidate)) return candidate;

                folder = Path.GetDirectoryName(folder);
            }

            return null;
        }

        /// <summary>Normalised to forward slashes because the two halves disagree — Godot hands back a
        /// Godot path and <see cref="Path.Combine"/> appends the platform separator, so the mixed
        /// result is what would end up on screen.</summary>
        private static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');
    }
}
