namespace NarrativeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Tooling.Json;
    using Tooling.Localization;
    using Tooling.Narrative;
    using static Tooling.Text.Format;

    /// <summary>
    /// The outline of one record drawn as rows of a tree, and the way back: the row the author stands
    /// on is an address in the document, which is what an inspector is opened on.
    /// <para>The shape of the outline is read elsewhere — this only draws it, and adds the one thing a
    /// reading of the document cannot know: what a localization key says in the locale the tool is
    /// running in.</para>
    /// </summary>
    public sealed class OutlineTree(Tree tree)
    {
        /// <summary>The tree is one column wide: a row is a sentence and not a table.</summary>
        private const int LabelColumn = 0;

        private const string TranslationFormat = "{0}   ·   “{1}”";

        private readonly List<OutlineNode> _rows = [];
        private readonly List<TreeItem> _items = [];

        /// <summary>The wording of the game as the run is editing it, or null while it has none. Read
        /// here rather than from the engine so that a line retranslated in the panel beside the tree shows
        /// in the tree: the engine loaded its translations when the tool started and answers with the word
        /// that was there then.</summary>
        public LocalizedTexts? Texts { get; set; }

        /// <summary>The row the author stands on, or nothing while the tree is empty.</summary>
        public OutlineNode? Selected => Row(tree.GetSelected());

        /// <summary>
        /// Draws the outline. Rows already on screen are written again in place when the outline has the
        /// shape of the one drawn: a key typed into a line has to show in its row, and a tree rebuilt per
        /// keystroke would drop the author's place in it and collapse everything he had opened.
        /// <para>Where the shape did change the tree is built whole and the author is put back on the row
        /// he was standing on. A row that has gone leaves him on the record itself, which is the one row
        /// every outline has.</para>
        /// </summary>
        public void Show(OutlineNode? root)
        {
            List<OutlineNode> rows = [];

            if (root is not null) Flatten(root, rows);

            if (Renamed(rows)) return;

            JsonPointer? standing = Selected?.Pointer;

            tree.Clear();
            _rows.Clear();
            _items.Clear();

            if (root is not null) Draw(root, parent: null);

            Stand(standing);
        }

        private static void Flatten(OutlineNode node, List<OutlineNode> rows)
        {
            rows.Add(node);

            foreach (OutlineNode child in node.Children) Flatten(child, rows);
        }

        /// <summary>What one row says: what the outline calls it, and — for a row standing for something
        /// the player is shown — the text its key says now. A key nothing translates says nothing beside
        /// the row: the author is reading whether the text has been written yet.</summary>
        private string Named(OutlineNode node)
        {
            if (node.Key.Length == 0) return node.Label;

            string translation = Translation(node.Key);

            return translation.Length == 0 ? node.Label : Text(TranslationFormat, node.Label, translation);
        }

        /// <summary>The text a key stands for, or nothing for a key nobody has written a translation for
        /// yet. Read out of the files the run is editing whenever it has them, and out of the engine only
        /// when it has not — the engine answers in whichever locale the tool happens to be running in, and
        /// with the word that was there when it started.</summary>
        private string Translation(string key)
        {
            if (Texts is { } texts)
                return texts.Locales.Select(locale => texts.Read(locale, key)).FirstOrDefault(said => said is { Length: > 0 })
                       ?? string.Empty;

            string translation = TranslationServer.Translate(key).ToString();

            return string.Equals(translation, key, StringComparison.Ordinal) ? string.Empty : translation;
        }

        /// <summary>Writes the rows again where the outline stands where it stood. False when it does
        /// not, which is the caller's cue to build the tree afresh.</summary>
        private bool Renamed(List<OutlineNode> rows)
        {
            if (rows.Count != _rows.Count) return false;

            for (int index = 0; index < rows.Count; index++)
                if (rows[index].Pointer != _rows[index].Pointer)
                    return false;

            for (int index = 0; index < rows.Count; index++)
            {
                _rows[index] = rows[index];
                _items[index].SetText(LabelColumn, Named(rows[index]));
            }

            return true;
        }

        /// <summary>One row, and the rows under it. Its place in the walk is written onto the item: a
        /// tree item is handed back by the engine and the row it stands for has to be found from it.</summary>
        private void Draw(OutlineNode node, TreeItem? parent)
        {
            TreeItem item = tree.CreateItem(parent);
            int index = _rows.Count;

            _rows.Add(node);
            _items.Add(item);

            item.SetMetadata(LabelColumn, index);
            item.SetText(LabelColumn, Named(node));

            foreach (OutlineNode child in node.Children) Draw(child, item);
        }

        private OutlineNode? Row(TreeItem? item)
        {
            if (item is null) return null;

            Variant written = item.GetMetadata(LabelColumn);

            if (written.VariantType != Variant.Type.Int) return null;

            int index = written.AsInt32();

            return index >= 0 && index < _rows.Count ? _rows[index] : null;
        }

        /// <summary>Puts the author on the row written at an address — back where he was standing after a
        /// rebuild, or on the place another reading of the record hands over — and on the record itself
        /// when nothing is written there any more.</summary>
        public void Stand(JsonPointer? standing)
        {
            if (_items.Count == 0) return;

            int at = 0;

            if (standing is not null)
                for (int index = 0; index < _rows.Count; index++)
                    if (_rows[index].Pointer == standing)
                    {
                        at = index;
                        break;
                    }

            _items[at].Select(LabelColumn);
            tree.ScrollToItem(_items[at]);
        }
    }
}
