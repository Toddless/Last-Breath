namespace NarrativeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Json;

    /// <summary>
    /// Everything a reading of the whole narrative can go out of date under: every document the run has
    /// open, every file laid down while it is, and the stack the tool files its steps on.
    /// <para>Every catalog and not the narrative ones alone — an id a dialogue points at is written in
    /// another catalog, so an answer is stale as soon as ANY of them is typed into. The stack covers what
    /// announces nothing of its own, the .po files among them, whose wording is the very thing a
    /// missing-text finding is about.</para>
    /// </summary>
    internal sealed class DocumentWatch
    {
        private readonly List<JsonTreeDocument> _watched = [];

        private readonly List<CatalogView> _catalogs = [];

        /// <summary>Everything the tool has open. Setting it moves the watch and says so once.</summary>
        public CatalogWorkspace? Workspace
        {
            get;
            set
            {
                Unwatch();
                field = value;
                Watch();
                Changed?.Invoke();
            }
        }

        public EditHistory? History
        {
            get;
            set
            {
                if (field is { } watched) watched.Changed -= Stepped;
                field = value;
                if (field is { } history) history.Changed += Stepped;
            }
        }

        /// <summary>Something under the watch moved, whatever it was.</summary>
        public event Action? Changed;

        /// <summary>Lets go of everything watched. Called when the panel holding it leaves the tree: a
        /// handler on a document outliving its panel is a redraw of a control nobody has.</summary>
        public void Stop()
        {
            Unwatch();
            History = null;
        }

        private void Watch()
        {
            if (Workspace is not { } workspace) return;

            foreach (CatalogView view in workspace.Catalogs)
            {
                view.FileAdded += Added;
                _catalogs.Add(view);

                foreach (CatalogFile file in view.Files) Watch(file);
            }
        }

        private void Watch(CatalogFile file)
        {
            file.Document.Changed += Stale;
            _watched.Add(file.Document);
        }

        private void Unwatch()
        {
            foreach (JsonTreeDocument document in _watched) document.Changed -= Stale;
            foreach (CatalogView view in _catalogs) view.FileAdded -= Added;

            _watched.Clear();
            _catalogs.Clear();
        }

        /// <summary>Takes a freshly laid file under the same watch: a record written into a file that did
        /// not exist when the last reading was made is a record that reading never saw.</summary>
        private void Added(CatalogFile file)
        {
            Watch(file);
            Changed?.Invoke();
        }

        private void Stale(JsonPointer pointer) => Changed?.Invoke();

        private void Stepped() => Changed?.Invoke();
    }
}
