namespace Tooling.Catalogs
{
    using System;
    using Tooling.Json;

    /// <summary>
    /// Whether anything of the run has been written since an answer about it was last worked out. Held
    /// apart because both readings of the references ask it — which words a field may hold, and which
    /// fields hold one word — and both are worked out again the first time they are asked for after a
    /// change rather than when the change arrives: a keystroke invalidates them, and a walk over every
    /// document per keystroke is a walk nobody reads.
    /// <para>A file joining a catalog while the run is open is a change like any other: what was worked
    /// out was worked out without it.</para>
    /// </summary>
    internal sealed class CatalogChanges
    {
        private bool _written = true;

        public CatalogChanges(CatalogWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            foreach (CatalogView catalog in workspace.Catalogs)
            {
                foreach (CatalogFile file in catalog.Files) Watch(file);

                catalog.FileAdded += Watch;
            }
        }

        /// <summary>Takes the news that the run has been written since this was last asked, and answers
        /// whether there was any: asking consumes it, because the caller is the one working the answer out
        /// again.</summary>
        public bool TakeChange()
        {
            if (!_written) return false;

            _written = false;

            return true;
        }

        private void Watch(CatalogFile file)
        {
            file.Document.Changed += Touched;
            _written = true;
        }

        private void Touched(JsonPointer pointer) => _written = true;
    }
}
