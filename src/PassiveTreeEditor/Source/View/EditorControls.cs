namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Godot;

    /// <summary>
    /// The widgets every editor panel builds. Stateless on purpose: a panel owns its data and its
    /// layout, this only hands out controls, so it stays usable from any panel without inheritance.
    /// </summary>
    public static class EditorControls
    {
        private const int CaptionFontSize = 13;

        /// <summary>
        /// Item id of the "no value" entry. Deliberately far outside any enum's range: Godot
        /// substitutes the item index for a negative id, and index 0 collides with the first enum
        /// member — which once silently turned "—" into <see cref="Core.Enums.Stance.Dexterity"/>.
        /// </summary>
        private const int NoneId = 100;

        /// <summary>A typo guard, not a design bound — the canvas itself is unbounded.</summary>
        private const double InputLimit = 100000;

        /// <summary>Shared body of both pickers: one item per member, keyed by the member's numeric
        /// value, with the item matching <paramref name="current"/> preselected.</summary>
        private static Dictionary<int, T> Fill<T>(OptionButton picker, IEnumerable<T> members, T? current)
            where T : struct, Enum
        {
            var byId = new Dictionary<int, T>();

            foreach (T member in members)
            {
                int id = Convert.ToInt32(member);
                byId[id] = member;
                picker.AddItem(member.ToString(), id);
                if (current.HasValue && EqualityComparer<T>.Default.Equals(member, current.Value))
                    picker.Selected = picker.ItemCount - 1;
            }

            return byId;
        }

        /// <summary>
        /// Drops the children of a container that is about to be rebuilt. <see cref="Node.QueueFree"/>
        /// on its own is not enough — it takes effect at the end of the frame, while the rebuild adds
        /// the replacements right away, so the container would spend a frame showing both sets.
        /// Detaching first is why Core's <c>NodeExtensions.QueueFreeChildren</c> cannot stand in here.
        /// </summary>
        public static void ClearContent(this Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                node.RemoveChild(child);
                child.QueueFree();
            }
        }

        public static Label Caption(string text)
        {
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride("font_size", CaptionFontSize);
            return label;
        }

        public static Label Wrapped(string text) => new()
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        public static SpinBox Number(double value, double step) => new()
        {
            MinValue = -InputLimit,
            MaxValue = InputLimit,
            Step = step,
            Value = value,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        public static string Percent(float fraction) =>
            (fraction * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        /// <summary>Dropdown over enum members. The item id is the member's numeric value, so the
        /// callback never depends on item order.</summary>
        public static OptionButton Picker<T>(IEnumerable<T> members, T current, Action<T> apply)
            where T : struct, Enum
        {
            var picker = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            Dictionary<int, T> byId = Fill(picker, members, current);

            picker.ItemSelected += index => apply(byId[picker.GetItemId((int)index)]);
            return picker;
        }

        public static OptionButton Picker<T>(T current, Action<T> apply) where T : struct, Enum =>
            Picker(Enum.GetValues<T>(), current, apply);

        /// <summary>The same dropdown plus an explicit "no value" entry, for a member that is
        /// optional rather than defaulted.</summary>
        public static OptionButton OptionalPicker<T>(T? current, Action<T?> apply, string emptyLabel = "—")
            where T : struct, Enum
        {
            var picker = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            picker.AddItem(emptyLabel, NoneId);

            Dictionary<int, T> byId = Fill(picker, Enum.GetValues<T>(), current);

            picker.ItemSelected += index =>
            {
                int id = picker.GetItemId((int)index);
                apply(id == NoneId ? null : byId[id]);
            };

            return picker;
        }
    }
}
