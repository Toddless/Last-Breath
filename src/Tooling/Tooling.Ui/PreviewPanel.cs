namespace Tooling.Ui
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using LastBreath.Descriptors.Preview;
    using Tooling.Catalogs;

    /// <summary>
    /// What the record on screen would read as in the game. The panel asks and draws; the host answers,
    /// because what a record means is the game's question and this is a window.
    /// <para>Two switches belong here and not to the host: which locale the reading is in, and which
    /// variant of the record is read — a template is not an item, and a piece of equipment reads one way
    /// as a common drop and another as a legendary one. Both are read back by the host from
    /// <see cref="Locale"/> and <see cref="Variant"/> while it answers.</para>
    /// <para>The reading follows the typing a moment behind. Every keystroke of the tool reaches this
    /// panel, and a preview rebuilt per letter reads every catalog of the game per letter.</para>
    /// </summary>
    public partial class PreviewPanel : VBoxContainer
    {
        /// <summary>How long the typing has to stop before the reading is built again. Long enough that
        /// a word typed at speed is read once, short enough that the author does not wait for it.</summary>
        private const double SettleSeconds = 0.25;

        private const string PanelTitle = "preview";

        private const string LocaleHint = "the locale this reading is in; the .po files as they are open now";

        private const string VariantHint = "which variant of the record is read";

        private const string NoRecordText = "no record is selected";

        private const string NothingToReadText = "this catalog has nothing to preview";

        private const int TitleFontSize = 16;

        /// <summary>How wide a line is measured at before it wraps. Fixed, because a wrapped label needs
        /// a width before it can say how tall it is, and the pane it sits in is free to be dragged.</summary>
        private const int LineWidth = 420;

        /// <summary>How tall the pane asks to be before the divider above it is dragged.</summary>
        private const int PaneHeight = 220;

        /// <summary>What a picker holds when it offers nothing at all.</summary>
        private const int NoChoice = -1;

        private OptionButton _locales = null!;
        private OptionButton _variants = null!;
        private Label _title = null!;
        private VBoxContainer _body = null!;
        private Timer _settle = null!;

        private CatalogRecord? _record;

        /// <summary>What the record on screen reads as, answered by the host. Null for a record this run
        /// has no reading of — a catalog nothing previews, or a record too unfinished to be built.</summary>
        public Func<CatalogRecord, PreviewText?>? Source { get; set; }

        /// <summary>The locale the reading is asked for in — read by the host while it answers.</summary>
        public string Locale => Chosen(_locales);

        /// <summary>The variant the reading is asked for — read by the host while it answers. Empty
        /// where the host offered none, which is every catalog whose records read one way only.</summary>
        public string Variant => Chosen(_variants);

        public override void _Ready()
        {
            CustomMinimumSize = new Vector2(0, PaneHeight);

            _locales = Picker(LocaleHint);
            _variants = Picker(VariantHint);

            var head = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            head.AddChild(new Label { Text = PanelTitle, ThemeTypeVariation = "DimLabel" });
            head.AddChild(_locales);
            head.AddChild(_variants);

            _title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            _title.AddThemeFontSizeOverride("font_size", TitleFontSize);

            _body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            _settle = new Timer { OneShot = true, WaitTime = SettleSeconds };
            _settle.Timeout += Rebuild;

            AddChild(head);
            AddChild(_title);
            AddChild(_body);
            AddChild(_settle);

            Rebuild();
        }

        /// <summary>What the two switches may be set to, and what they open on: the locales the run has
        /// read, and the variants the host offers for the catalog on screen. An empty list of variants
        /// hides that switch — a question with no answers is one that was never asked.</summary>
        public void Offer(IReadOnlyList<string> locales, IReadOnlyList<string> variants)
        {
            ArgumentNullException.ThrowIfNull(locales);
            ArgumentNullException.ThrowIfNull(variants);

            Fill(_locales, locales);
            Fill(_variants, variants);
        }

        /// <summary>Which record the tool is standing on. Asked again after every change of the run, so
        /// this is also how an edit — to a number of the record, or to a word of a locale — reaches the
        /// reading.</summary>
        public void Standing(CatalogRecord? record)
        {
            _record = record;

            Later();
        }

        private static string Chosen(OptionButton picker) =>
            picker.Selected >= 0 ? picker.GetItemText(picker.Selected) : string.Empty;

        private static void Fill(OptionButton picker, IReadOnlyList<string> options)
        {
            string held = Chosen(picker);

            picker.Clear();

            foreach (string option in options) picker.AddItem(option);

            // The switch stays where the author left it where the new list still holds that answer:
            // moving from one record to the next is not a reason to re-pick a locale.
            int at = Held(options, held);

            picker.Selected = options.Count == 0 ? NoChoice : Math.Max(at, 0);
            picker.Visible = options.Count > 0;
        }

        /// <summary>Where the answer the picker was standing on is in the list it is offered now, or
        /// <see cref="NoChoice"/> when the new list does not hold it.</summary>
        private static int Held(IReadOnlyList<string> options, string chosen)
        {
            for (int at = 0; at < options.Count; at++)
                if (string.Equals(options[at], chosen, StringComparison.Ordinal))
                    return at;

            return NoChoice;
        }

        private OptionButton Picker(string hint)
        {
            var picker = new OptionButton { TooltipText = hint };

            picker.ItemSelected += _ => Rebuild();

            return picker;
        }

        /// <summary>Asks for a reading once the typing has stopped. Restarted rather than queued: a burst
        /// of keystrokes is one edit as far as a reader is concerned.</summary>
        private void Later()
        {
            if (_settle is null) return;

            _settle.Start();
        }

        private void Rebuild()
        {
            if (_body is null || !IsInsideTree()) return;

            _body.DropChildren();

            PreviewText? read = _record is { } record ? Source?.Invoke(record) : null;

            _title.Text = read?.Title ?? string.Empty;
            _title.Visible = _title.Text.Length > 0;

            if (read is null)
            {
                Line(_record is null ? NoRecordText : NothingToReadText, dim: true);
                return;
            }

            foreach (string line in read.Lines) Line(line);
        }

        private void Line(string text, bool dim = false)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(LineWidth, 0),
            };

            if (dim) label.ThemeTypeVariation = "DimLabel";

            _body.AddChild(label);
        }
    }
}
