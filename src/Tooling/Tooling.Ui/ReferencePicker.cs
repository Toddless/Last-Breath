namespace Tooling.Ui
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Tooling.Catalogs;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>What the run has to say about the word standing in a reference: whether it answers to
    /// nothing, and what to tell the author on the box holding it.</summary>
    public readonly record struct ReferenceVerdict(bool Broken, string Say);

    /// <summary>
    /// The ids a field may be answered with — offered under the box they are written in — and the
    /// judgement on the word standing there. Both live here because they are two halves of one question:
    /// a picker that offers the ids of a catalog and a mark that says an id answers to nothing would
    /// disagree the day a catalog is described, and the author would be shown a word he had just picked
    /// painted as broken.
    /// </summary>
    public static class ReferencePicker
    {
        /// <summary>How many ids the picker offers at once. A query of one letter answers with most of a
        /// catalog, and a list nobody scrolls to the end of is a list that only costs rows.</summary>
        public const int Rows = 60;

        public const string PickText = "…";
        public const string PickHint = "pick an id out of what this field points into";

        /// <summary>How wide and how tall the list of ids opens.</summary>
        private const int Width = 340;
        private const int Height = 300;

        private const string SearchPlaceholder = "search";
        private const string TargetSeparator = ", ";

        private const string BrokenFormat = "nothing in {0} is written under this id";
        private const string EmptyText = "this field has to name something";
        private const string UncheckedFormat = "{0}: this build describes no such catalog or section, so the id is not checked";

        /// <summary>What a word answering to nothing is written in. A reference is read down a column of
        /// them, and the one that points nowhere has to be the one the eye stops on.</summary>
        public static Color BrokenReference => new(0.93f, 0.42f, 0.38f);

        /// <summary>The catalogs a field points into, as one name for an author to read.</summary>
        public static string Targets(IEnumerable<ReferenceTarget> targets) => string.Join(TargetSeparator, targets);

        /// <summary>
        /// Whether the word standing in a reference answers to anything, and what to say about it.
        /// Nothing is called broken while the run cannot tell: a catalog this build describes no schema
        /// for writes ids the tool has never read, and marking every reference into it would be the tool
        /// complaining about itself.
        /// </summary>
        public static ReferenceVerdict Judge(
            ReferenceIndex? references, IReadOnlyList<ReferenceTarget> targets, string written, bool allowEmpty)
        {
            ArgumentNullException.ThrowIfNull(targets);
            ArgumentNullException.ThrowIfNull(written);

            string named = Targets(targets);

            if (references is null || targets.Count == 0) return new ReferenceVerdict(Broken: false, named);

            IReadOnlyList<string> unread = references.Undescribed(targets);

            if (unread.Count > 0) return new ReferenceVerdict(Broken: false, Text(UncheckedFormat, string.Join(TargetSeparator, unread)));

            if (written.Length == 0 && allowEmpty) return new ReferenceVerdict(Broken: false, named);
            if (written.Length > 0 && references.Exists(targets, written)) return new ReferenceVerdict(Broken: false, named);

            return new ReferenceVerdict(Broken: true, written.Length == 0 ? EmptyText : Text(BrokenFormat, named));
        }

        /// <summary>
        /// Offers the ids a query names under the control the reference is written in: a search field and
        /// the ids answering it, best match first. One click writes the id, which is what the author came
        /// for — a picker asking for a second confirming press is a list read twice.
        /// <para>Built for the press and freed when it closes. A panel is torn down and drawn again on
        /// every change, and a list held between two of those would be offering the ids of a record that
        /// is no longer on screen.</para>
        /// </summary>
        public static void Open(Node owner, Control under, Func<string, IReadOnlyList<string>> search, Action<string> chosen)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(under);
            ArgumentNullException.ThrowIfNull(search);
            ArgumentNullException.ThrowIfNull(chosen);

            var popup = new PopupPanel();
            var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            var query = new LineEdit { PlaceholderText = SearchPlaceholder, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            var results = new ItemList { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };

            void Fill(string typed)
            {
                results.Clear();

                foreach (string id in search(typed)) results.AddItem(id);
            }

            void Take(int index)
            {
                if (index < 0 || index >= results.ItemCount) return;

                chosen(results.GetItemText(index));
                popup.Hide();
            }

            Fill(string.Empty);

            query.TextChanged += Fill;

            // Enter takes the best hit: typing the id of a record and pressing return is the fast path
            // the search field exists for.
            query.TextSubmitted += _ => Take(0);
            results.ItemSelected += index => Take((int)index);

            body.AddChild(query);
            body.AddChild(results);
            popup.AddChild(body);
            popup.PopupHide += popup.QueueFree;

            owner.AddChild(popup);

            var at = (Vector2I)(under.GetScreenPosition() + new Vector2(0, under.Size.Y));

            popup.Popup(new Rect2I(at, new Vector2I(Math.Max(Width, (int)under.Size.X), Height)));

            // Taken once the window is on screen, the way every other dialog of the tool takes it: a
            // window still being opened has no focus to hand out, and a search field that has to be
            // clicked before it can be typed into is a list the author scrolls instead.
            Callable.From(query.GrabFocus).CallDeferred();
        }
    }
}
