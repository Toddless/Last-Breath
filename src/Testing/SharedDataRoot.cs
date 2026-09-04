namespace LastBreathTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Core.Data.GameData;
    using Tooling.Catalogs;

    /// <summary>The shipped data as a test reaches it. Every project links the common catalogs as a
    /// <c>Data/Shared</c> symlink next to its build output, so a test that reads what the game ships walks
    /// up from where it is running until it finds one.</summary>
    internal static class SharedData
    {
        private const string Link = "Data";
        private const string Shared = "Shared";

        public static string Root()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, Link, Shared);
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"{Link}/{Shared} symlink not found above {AppContext.BaseDirectory}");
        }

        /// <summary>The folder one shipped catalog is read from.</summary>
        public static string Catalog(string catalog) => Path.Combine(Root(), catalog);

        /// <summary>The files one shipped catalog is written across, in the order the game reads them. A
        /// catalog holds as many files as its author cared to split it into, so a test asking what the game
        /// ships asks the folder and never a file name.</summary>
        public static IReadOnlyList<string> Files(string catalog) =>
            CatalogWorkspace.FilePaths(Catalog(catalog));
    }

    /// <summary>Every catalog the game names, read off the constants themselves so that a name added to
    /// <see cref="DataCatalog"/> is one the guards pick up without being told about it.</summary>
    internal static class DataCatalogNames
    {
        public static IReadOnlyList<string> All() =>
        [
            .. typeof(DataCatalog)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
        ];
    }
}
