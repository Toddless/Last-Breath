namespace Tooling.Ui
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Godot;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Catalogs.Forms;
    using Tooling.Json;
    using static Tooling.Text.Format;

    /// <summary>
    /// One loot table as the author edits it: a block per tier, the seats standing at it one to a row,
    /// and under them what the tier comes to — how many things drop from it and what they cost together.
    /// A seat names one thing by id or a whole set of augments by a filter, and either may be written,
    /// priced, moved to another tier or taken off the table where it stands.
    /// <para>Drawn in place of the general reading of the record's schema, which shows the same table as
    /// four levels of nested lists: the question an author actually asks of this file — which thing sits
    /// in which tier, and for how much — cannot be read down a column of <c>[3] · [12] · id</c>.</para>
    /// <para>Everything a press writes goes onto the document's own history through
    /// <see cref="Gestures"/>, so one key takes it back; the panel is thrown away and drawn again after
    /// each of them, which is what keeps every address on screen one the document still has.</para>
    /// </summary>
    public partial class LootTablePanel : VBoxContainer
    {
        /// <summary>How far the seats step in from the tier heading them.</summary>
        private const int Indent = 14;

        private const int HeaderFontSize = 15;

        /// <summary>How wide a box holding one number opens. Prices run to three digits and tiers to one,
        /// and boxes each fitting their own number make a column nobody can read down.</summary>
        private const int NumberWidth = 90;

        /// <summary>How wide the box naming what a seat drops opens.</summary>
        private const int IdWidth = 240;

        /// <summary>What a seat written now costs where the tier gives nothing to go by. Not zero: the
        /// game refuses a seat priced at nothing, so one laid down that way would have to be noticed
        /// before it meant anything at all.</summary>
        private const double FirstPrice = 1;

        /// <summary>The tier of augments a fresh set names while the author has said nothing.</summary>
        private const int FirstAugmentTier = 0;

        private const string Missing = "—";

        private const string TierFormat = "tier {0}";
        private const string AddTierFormat = "+ tier {0}";
        private const string BrokenFormat = "{0} — {1}";
        private const string SummaryFormat = "{0} position(s)   ·   {1} loot units";
        private const string UnpricedFormat = "   ·   {0} the game will not buy";
        private const string RepeatedFormat = "   ·   seated twice: {0}";
        private const string ListSeparator = ", ";

        private const string TierCaption = "tier";
        private const string ChanceCaption = "chance";
        private const string RarityCaption = "rarity";
        private const string PriceCaption = "price";
        private const string AugmentsCaption = "augments";

        private const string AddPositionText = "+ item";
        private const string AddGroupText = "+ augments";
        private const string RemoveText = "×";
        private const string MoveText = "→";

        private const string TierNumberHint = "which tier this is; a table merges the tiers written under one number";
        private const string ChanceHint = "how often this tier comes up";
        private const string PriceHint = "what this position costs in loot units, which is never gold";
        private const string NewPriceHint = "what the next position written here costs";
        private const string AugmentTierHint = "which tier the augments of this set are of";
        private const string RarityHint = "where the augments of this set stand on the rarity scale";
        private const string AddPositionHint = "pick a thing out of everything that drops and seat it at this tier";
        private const string AddGroupHint = "seat a whole set of augments here, as one position";
        private const string AddTierHint = "write another tier into this table";
        private const string RemovePositionHint = "take this position off the table";
        private const string RemoveTierHint = "take this tier out, and the positions standing at it with it";
        private const string MoveHint = "move this position to another tier";

        private const string NoTiersText = "this table holds no tiers, so nothing drops from it";
        private const string NoPositionsText = "no positions stand at this tier";
        private const string NoLayoutText = "the words this table is written with were not named, so it cannot be drawn";
        private const string OneTierText = "this table has no other tier to move a position to";
        private const string NoDropsText = "this build describes no catalog a position may name, so there is nothing to pick from";
        private const string NoRaritiesText = "this build lists no rarities, so a set of augments cannot be named here";

        private const string ConfusedText =
            "a position names exactly one of an id and a set of augments: the game reads nothing of this one";

        private const string NoPriceText = "a position costs a positive number of loot units";
        private const string NotANumberFormat = "“{0}” is not a number";
        private const string MovedFormat = "moved the position to {0}";
        private const string NothingWrittenText = "nothing was written";

        private const string FontSizeOverride = "font_size";
        private const string FontColorOverride = "font_color";
        private const string MarginLeftOverride = "margin_left";

        private CatalogRecord? _record;
        private LootTableForm? _form;
        private JsonTreeDocument? _document;

        /// <summary>Where the tiers of the record on screen stand. Held because every address this panel
        /// draws hangs off it, which is what makes a change elsewhere in the record none of its business.</summary>
        private JsonPointer? _tiers;

        /// <summary>What the schema says about a seat: where an id may point, and which rarities a set of
        /// augments may name. Read once per record — it is the catalog's answer and not the table's.</summary>
        private LootPositionSchema _position = new([], []);

        /// <summary>The document moved under the panel — an undo, a redo, an edit made elsewhere. Every
        /// address on screen was read out of the tree before that, so nothing drawn here may write any
        /// more: the inspector draws the record again, and the replacement speaks for the file as it is.</summary>
        private bool _stale;

        /// <summary>A press of this panel is writing. The document tells everyone it changed, this panel
        /// included, and a change of its own is not news that its addresses have moved.</summary>
        private bool _writing;

        /// <summary>What a gesture came to when the table itself cannot show it — a number nobody typed,
        /// a tier with nowhere to move a seat to. The panel has no line of its own to say it on.</summary>
        public event Action<string>? Said;

        /// <summary>The words this table is written with. Named by the host: they belong to the game's own
        /// types, and nothing here is allowed to spell them.</summary>
        public LootTableLayout? Layout { get; set; }

        /// <summary>The ids the run knows, for the picker a position is named out of and for the mark on
        /// a position naming something no catalog writes.</summary>
        public ReferenceIndex? References { get; set; }

        /// <summary>How a press changes the document: one step of the history, and the record drawn again
        /// once the press is over. Null leaves the table read-only, which is what a panel with no way to
        /// write is.</summary>
        public RecordGesture? Gestures { get; set; }

        /// <summary>Draws one table. Called once per panel: the inspector throws the panel away and asks
        /// for another after every change, so what is on screen is never a reading of an older file.</summary>
        public void Rebuild(CatalogRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            _record = record;
            _form = Layout is { } layout ? new LootTableForm(layout) : null;
            _position = _form?.Describe(record.Schema) ?? new LootPositionSchema([], []);
            _tiers = _form is { } built ? record.Pointer.Append(built.Layout.TiersKey) : null;

            Follow(record.File.Document);
            DrawTable();
        }

        public override void _ExitTree() => Follow(null);

        private static Label Heading(string text)
        {
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride(FontSizeOverride, HeaderFontSize);

            return label;
        }

        private static Label Note(string text) => new()
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        /// <summary>What the control beside it holds, named on the row rather than in a tooltip: a seat is
        /// read across and not down, so the words have to stand between the boxes.</summary>
        private static Label Caption(string name) => new() { Text = name, VerticalAlignment = VerticalAlignment.Center };

        /// <summary>A block stepped in under the row above it: what it holds belongs to that row.</summary>
        private static Node Indented(Node parent)
        {
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride(MarginLeftOverride, Indent);

            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            margin.AddChild(body);
            parent.AddChild(margin);

            return body;
        }

        /// <summary>A tier by the number it carries, and by nothing at all where the file wrote no number:
        /// a tier the game cannot find is one the author has to see is nameless.</summary>
        private static string Numbered(LootTier tier) => Text(TierFormat, tier.Tier is { } number ? number : Missing);

        /// <summary>A number as the file spells it, and nothing at all where the file holds none: an
        /// absent price and a price of zero are two different things to say about a seat.</summary>
        private static string Written(double? value) =>
            value is { } number ? number.ToString(CultureInfo.InvariantCulture) : string.Empty;

        /// <summary>The number a text stands for, read in the invariant culture because that is the one
        /// the file writes in.</summary>
        private static double? Parsed(string text) =>
            double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            && double.IsFinite(number)
                ? number
                : null;

        /// <summary>What the next seat at a tier costs while the author has said nothing: what the last
        /// one there cost. A tier is written a price at a time and its seats are worth about the same,
        /// which is the whole reason a tier is a tier.</summary>
        private static double Opening(LootTier tier)
        {
            for (int index = tier.Positions.Count - 1; index >= 0; index--)
                if (tier.Positions[index].Price is { } price)
                    return price;

            return FirstPrice;
        }

        /// <summary>What a tier comes to, in one line: the seats at it, what they cost together, the ones
        /// the game will refuse to buy, and the things seated twice. The last two are said where they are
        /// counted rather than left for a load to find out.</summary>
        private static string Summary(LootTier tier)
        {
            LootTierSummary summary = LootTableForm.Summary(tier);
            IReadOnlyList<string> repeated = LootTableForm.Repeated(tier);
            string said = Text(SummaryFormat, summary.Positions, summary.Price);

            if (summary.Unpriced > 0) said += Text(UnpricedFormat, summary.Unpriced);
            if (repeated.Count > 0) said += Text(RepeatedFormat, string.Join(ListSeparator, repeated));

            return said;
        }

        /// <summary>A seat the game reads nothing of, shown as what the file actually wrote there. Only
        /// the gestures beside it still answer, and taking it out is exactly what an author does with
        /// one.</summary>
        private static Control Confused(LootPosition position)
        {
            Label said = Note(Text(BrokenFormat, ConfusedText, position.Token?.ToString(Formatting.None) ?? Missing));

            said.TooltipText = ConfusedText;
            said.MouseFilter = MouseFilterEnum.Pass;

            return said;
        }

        private static string Chosen(OptionButton picker) =>
            picker.Selected >= 0 ? picker.GetItemText(picker.Selected) : string.Empty;

        /// <summary>Follows one document at a time, so the panel hears about the record it is showing and
        /// about nothing else.</summary>
        private void Follow(JsonTreeDocument? document) => _document = PanelParts.Following(_document, document, Touched);

        /// <summary>
        /// Something moved in the document. It costs this panel its addresses only where it moved one of
        /// them: inside the tiers, whose seats are addressed by their place in a list, or at the record
        /// itself and above it, where a record written or taken out shifts everything below.
        /// <para>A change to another key of the record — its own name, typed in the row the inspector
        /// keeps above this form — moves nothing here and must not: the panel is not redrawn for it
        /// either, so a form that went read-only on it would stay that way for the rest of the run.</para>
        /// </summary>
        private void Touched(JsonPointer at)
        {
            if (_writing || _record is not { } record || _tiers is not { } tiers) return;
            if (!at.Within(tiers) && !record.Pointer.Within(at)) return;

            _stale = true;
        }

        private void DrawTable()
        {
            this.DropChildren();

            if (_record is not { } record || _form is not { } form)
            {
                AddChild(Note(NoLayoutText));
                return;
            }

            IReadOnlyList<LootTier> tiers = form.Tiers(record.Token, record.Pointer);

            if (tiers.Count == 0) AddChild(Note(NoTiersText));

            foreach (LootTier tier in tiers) AddTier(form, tier, tiers);

            AddChild(NewTier(form, record, tiers));
        }

        /// <summary>One tier: the number it is found by and how often it comes up, the seats standing at
        /// it, the row that writes another, and under all of it what the tier comes to.</summary>
        private void AddTier(LootTableForm form, LootTier tier, IReadOnlyList<LootTier> tiers)
        {
            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            header.AddChild(Heading(Numbered(tier)));
            header.AddChild(Caption(TierCaption));
            header.AddChild(NumberBox(tier.At, form.Layout.TierKey, tier.Tier, TierNumberHint));

            if (form.Layout.ChanceKey is { } chance)
            {
                header.AddChild(Caption(ChanceCaption));
                header.AddChild(NumberBox(tier.At, chance, tier.Chance, ChanceHint));
            }

            header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            header.AddChild(Press(RemoveText, RemoveTierHint,
                document => LootTableForm.RemoveTier(document, tier.At)));

            AddChild(header);

            Node body = Indented(this);

            if (tier.Positions.Count == 0) body.AddChild(Note(NoPositionsText));

            foreach (LootPosition position in tier.Positions) body.AddChild(Seat(form, position, tier, tiers));

            body.AddChild(NewPosition(form, tier));
            body.AddChild(Note(Summary(tier)));
        }

        /// <summary>One seat: what it names, what it costs, and what may be done to it where it stands. A
        /// seat naming both of the two things or neither of them is read as the json it holds — the game
        /// drops such a position, and a pair of boxes drawn over it would hide that.</summary>
        private Control Seat(LootTableForm form, LootPosition position, LootTier tier, IReadOnlyList<LootTier> tiers)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            if (!position.NamesOneThing) row.AddChild(Confused(position));
            else if (position.Id is { } id) row.AddChild(Drop(form, position, id));
            else row.AddChild(Group(form, position));

            row.AddChild(Caption(PriceCaption));
            row.AddChild(NumberBox(position.At, form.Layout.PriceKey, position.Price, PriceHint));
            row.AddChild(Move(form, position, tier, tiers));
            row.AddChild(Press(RemoveText, RemovePositionHint,
                document => LootTableForm.RemovePosition(document, position.At)));

            return row;
        }

        /// <summary>The thing a seat drops: the word the file holds, marked where no catalog writes it,
        /// with the picker the author would rather use behind it.</summary>
        private Control Drop(LootTableForm form, LootPosition position, string id)
        {
            var row = new HBoxContainer();
            var box = new LineEdit { Text = id, CustomMinimumSize = new Vector2(IdWidth, 0) };

            void Judge(string written)
            {
                ReferenceVerdict verdict = ReferencePicker.Judge(References, _position.Drops, written, allowEmpty: false);

                box.RemoveThemeColorOverride(FontColorOverride);
                box.TooltipText = verdict.Say;

                if (verdict.Broken) box.AddThemeColorOverride(FontColorOverride, ReferencePicker.BrokenReference);
            }

            // Written back only where it says something else. A box commits as it loses its focus, and it
            // loses it to the very press the author meant next: a write that changed nothing would still
            // redraw the record, and take that press down with the panel before it was heard.
            void Commit(string written)
            {
                if (string.Equals(written, id, StringComparison.Ordinal)) return;

                Change(document => document.Put(position.At.Append(form.Layout.IdKey), new JValue(written)));
            }

            // Judged as it is typed: the author is asking exactly whether the word he is writing answers
            // to anything.
            Judge(id);

            box.TextChanged += Judge;
            box.TextSubmitted += Commit;
            box.FocusExited += () => Commit(box.Text);

            row.AddChild(box);
            row.AddChild(Picker(box, chosen =>
            {
                box.Text = chosen;
                Commit(chosen);
            }));

            return row;
        }

        /// <summary>The set of augments a seat draws from: which tier its members are of, and where they
        /// stand on the rarity scale.</summary>
        private Control Group(LootTableForm form, LootPosition position)
        {
            if (position.Augments is not { } group) return Note(AugmentsCaption);

            var row = new HBoxContainer();
            JsonPointer at = position.At.Append(form.Layout.AugmentsKey);

            row.AddChild(Caption(AugmentsCaption));
            row.AddChild(Caption(TierCaption));
            row.AddChild(NumberBox(at, form.Layout.AugmentTierKey, group.Tier, AugmentTierHint));
            row.AddChild(Caption(RarityCaption));
            row.AddChild(Rarity(group.Rarity, chosen =>
            {
                // The picker speaks for the item picked whether or not it was already the one selected,
                // and writing back what stands would redraw the panel for a press that changed nothing.
                if (string.Equals(chosen, group.Rarity, StringComparison.Ordinal)) return;

                Change(document => document.Put(at.Append(form.Layout.RarityKey), new JValue(chosen)));
            }));

            return row;
        }

        /// <summary>Where a seat may go instead: the other tiers of this table, by the numbers they
        /// carry. A table with one tier says so on a press that cannot be made rather than opening an
        /// empty list.</summary>
        private Control Move(LootTableForm form, LootPosition position, LootTier tier, IReadOnlyList<LootTier> tiers)
        {
            bool any = tiers.Count > 1;

            var button = new MenuButton { Text = MoveText, TooltipText = any ? MoveHint : OneTierText, Disabled = !any };
            PopupMenu menu = button.GetPopup();

            for (int index = 0; index < tiers.Count; index++)
                if (!ReferenceEquals(tiers[index], tier))
                    menu.AddItem(Numbered(tiers[index]), index);

            menu.IdPressed += id => MoveTo(form, position, tiers[(int)id]);

            return button;
        }

        private void MoveTo(LootTableForm form, LootPosition position, LootTier tier)
        {
            bool moved = Change(document => form.MovePosition(document, position.At, tier.At));

            Say(moved ? Text(MovedFormat, Numbered(tier)) : NothingWrittenText);
        }

        /// <summary>The row that writes another seat at a tier: what the next one costs, the press that
        /// picks a thing to drop, and the press that seats a whole set of augments.</summary>
        private Control NewPosition(LootTableForm form, LootTier tier)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            LineEdit price = Box(Written(Opening(tier)), NewPriceHint);
            LineEdit augmentTier = Box(FirstAugmentTier.ToString(CultureInfo.InvariantCulture), AugmentTierHint);
            OptionButton rarity = Rarity(Standing(), chosen: null);
            bool drops = References is not null && _position.Drops.Count > 0;

            var pick = new Button
            {
                Text = AddPositionText,
                TooltipText = drops ? AddPositionHint : NoDropsText,
                Disabled = !drops
            };

            pick.Pressed += () =>
            {
                if (Priced(price) is not { } asked) return;

                ReferencePicker.Open(this, pick, Search,
                    id => Change(document => form.AddPosition(document, tier.At, id, asked)));
            };

            var group = new Button
            {
                Text = AddGroupText,
                TooltipText = _position.Rarities.Count > 0 ? AddGroupHint : NoRaritiesText,
                Disabled = _position.Rarities.Count == 0
            };

            group.Pressed += () =>
            {
                if (Priced(price) is not { } asked || Number(augmentTier) is not { } named) return;

                Change(document => form.AddGroup(document, tier.At, (int)named, Chosen(rarity), asked));
            };

            row.AddChild(Caption(PriceCaption));
            row.AddChild(price);
            row.AddChild(pick);
            row.AddChild(Caption(TierCaption));
            row.AddChild(augmentTier);
            row.AddChild(Caption(RarityCaption));
            row.AddChild(rarity);
            row.AddChild(group);

            return row;
        }

        /// <summary>The row that writes another tier, numbered by the first number this table has not
        /// used: two tiers under one number are one tier the author edits in two places.</summary>
        private Control NewTier(LootTableForm form, CatalogRecord record, IReadOnlyList<LootTier> tiers)
        {
            int next = LootTableForm.NextTier(tiers);

            Button add = Press(Text(AddTierFormat, next), AddTierHint,
                document => form.AddTier(document, record.Pointer, next));

            add.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            return add;
        }

        private static LineEdit Box(string text, string hint) => new()
        {
            Text = text,
            PlaceholderText = hint,
            TooltipText = hint,
            CustomMinimumSize = new Vector2(NumberWidth, 0)
        };

        /// <summary>
        /// A number of the file in a box, committed when the author leaves it or presses return and not
        /// as it is typed: every keystroke would otherwise be a step of the history, and the record is
        /// drawn again after each of them.
        /// <para>What is not a number is refused out loud and the file's own value put back — a box that
        /// swallowed the word would read as the tool having written it.</para>
        /// </summary>
        private LineEdit NumberBox(JsonPointer holder, string key, double? value, string hint)
        {
            LineEdit box = Box(Written(value), hint);

            JsonPointer at = holder.Append(key);

            void Commit(string typed)
            {
                if (Parsed(typed) is not { } number)
                {
                    box.Text = Written(value);

                    if (typed.Trim().Length > 0) Say(Text(NotANumberFormat, typed));

                    return;
                }

                // Written back only where it says another number. A box commits as it loses its focus, so
                // tabbing down a column of prices would otherwise redraw the record at every stop and tear
                // down the box the author was moving into.
                if (number == value) return;

                Change(document => document.Put(at, JsonScalars.Number(document.Resolve(at), number, whole: true)));
            }

            box.TextSubmitted += Commit;
            box.FocusExited += () => Commit(box.Text);

            return box;
        }

        /// <summary>The rarities a set of augments may name, as the schema lists them. A word the schema
        /// does not list stays on offer where the file wrote one: the tool never quietly rewrites what it
        /// does not understand.</summary>
        private OptionButton Rarity(string? written, Action<string>? chosen)
        {
            var picker = new OptionButton { TooltipText = RarityHint, Disabled = _position.Rarities.Count == 0 };
            List<string> members = [.. _position.Rarities];

            if (written is { Length: > 0 } word && !members.Contains(word, StringComparer.Ordinal)) members.Add(word);

            foreach (string member in members) picker.AddItem(member);

            // Nothing selected where nothing on offer answers for what the file holds, which is what a
            // filter naming no rarity at all is.
            picker.Selected = members.Count == 0 ? -1 : members.IndexOf(written ?? string.Empty);

            if (chosen is not null) picker.ItemSelected += index => chosen(members[(int)index]);

            return picker;
        }

        /// <summary>The rarity a fresh set of augments opens on: the first the schema lists, which is the
        /// only one that can be picked without asking.</summary>
        private string? Standing() => _position.Rarities.Count > 0 ? _position.Rarities[0] : null;

        /// <summary>The press that offers the ids a position may name, beside the box holding one.</summary>
        private Button Picker(Control under, Action<string> chosen)
        {
            bool any = References is not null && _position.Drops.Count > 0;

            var pick = new Button
            {
                Text = ReferencePicker.PickText,
                TooltipText = any ? ReferencePicker.PickHint : NoDropsText,
                Disabled = !any
            };

            pick.Pressed += () => ReferencePicker.Open(this, under, Search, chosen);

            return pick;
        }

        private IReadOnlyList<string> Search(string query) =>
            References?.Search(_position.Drops, query, ReferencePicker.Rows) ?? [];

        /// <summary>A button that changes the table.</summary>
        private Button Press(string text, string hint, Func<JsonTreeDocument, bool> change)
        {
            var button = new Button { Text = text, TooltipText = hint };

            button.Pressed += () => Change(change);

            return button;
        }

        /// <summary>What a box asking for a price was answered with, or nothing where it was answered
        /// with something that is not a positive number of loot units: the game refuses such a seat, and
        /// writing one would be the tool laying down a position it knows will be dropped.</summary>
        private double? Priced(LineEdit box)
        {
            if (Number(box) is not { } price) return null;
            if (price > 0) return price;

            Say(NoPriceText);

            return null;
        }

        private double? Number(LineEdit box)
        {
            if (Parsed(box.Text) is { } number) return number;

            Say(Text(NotANumberFormat, box.Text));

            return null;
        }

        /// <summary>
        /// Changes the document as one gesture of the author's: one step of the history, and the record
        /// drawn again once the press is over.
        /// <para>Nothing is written from a panel the document has already moved under — an undo, an edit
        /// made elsewhere — because every address on screen was read before that move, and a box losing
        /// its focus to the very press that stepped the history would write its old text back over it.</para>
        /// </summary>
        private bool Change(Func<JsonTreeDocument, bool> change)
        {
            if (_stale || Gestures is not { } gestures) return false;

            _writing = true;

            try
            {
                return gestures(change);
            }
            finally
            {
                _writing = false;
            }
        }

        private void Say(string what) => Said?.Invoke(what);
    }
}
