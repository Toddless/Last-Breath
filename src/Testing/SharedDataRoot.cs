namespace LastBreathTest
{
    using System;
    using System.IO;

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
    }
}
