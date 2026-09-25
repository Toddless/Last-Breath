namespace Core.Data.GameData
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Layers data sources: a catalog lives WHOLLY in the first source that has it — no file
    /// merging between sources, so a project-local catalog overrides the shared one entirely
    /// (copy a catalog next to the Shared link to experiment, delete it to fall back).
    /// </summary>
    public class CompositeDataSource(IReadOnlyList<IGameDataSource> sources) : IGameDataSource
    {
        public IReadOnlyList<GameDataFile> ReadCatalog(string catalog)
        {
            foreach (var source in sources)
            {
                try
                {
                    return source.ReadCatalog(catalog);
                }
                catch (DirectoryNotFoundException)
                {
                    // try the next layer
                }
            }

            throw new DirectoryNotFoundException($"Data catalog '{catalog}' does not exist in any source");
        }
    }
}
