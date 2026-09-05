namespace Tooling.Ui
{
    using System;
    using Godot;
    using Tooling.Json;

    /// <summary>The things every panel of the tooling that draws a document does the same way: it clears
    /// what it drew before, it hears about one document and about no other, and the wheel over a number
    /// of its scrolls the pane rather than rewriting the record.</summary>
    public static class PanelParts
    {
        /// <summary>What share of the page one turn of the wheel moves a pane by. The scrolling frames of
        /// the engine move by this much, so a guarded control scrolls the way the space beside it does.</summary>
        private const double WheelShare = 8;

        /// <summary>
        /// Drops what the panel drew before. <see cref="Node.QueueFree"/> on its own is not enough — it
        /// takes effect at the end of the frame, while a rebuild adds the replacements right away, so the
        /// panel would spend a frame showing both.
        /// </summary>
        public static void DropChildren(this Node node)
        {
            ArgumentNullException.ThrowIfNull(node);

            foreach (Node child in node.GetChildren())
            {
                node.RemoveChild(child);
                child.QueueFree();
            }
        }

        /// <summary>
        /// Follows one document at a time: the one <paramref name="held"/> stops speaking and
        /// <paramref name="wanted"/> is listened to, and the answer is what the caller holds from here
        /// on. A panel that went on hearing the document it drew an hour ago would redraw a record
        /// nobody is looking at, and stop hearing about the one that is.
        /// </summary>
        public static JsonTreeDocument? Following(
            JsonTreeDocument? held, JsonTreeDocument? wanted, Action<JsonPointer> heard)
        {
            ArgumentNullException.ThrowIfNull(heard);

            if (ReferenceEquals(held, wanted)) return held;

            held?.Changed -= heard;
            wanted?.Changed += heard;

            return wanted;
        }

        /// <summary>
        /// A spin box the wheel does not turn: over it the wheel moves the pane it sits in, and the number
        /// it stands on is left alone.
        /// <para>The spin box and nothing else. It is the one control of these panels that reads a turn of
        /// the wheel at all — a button answers only the buttons of its own mask, and the wheel is not one
        /// of them, so a picker passed over is already left alone and a guard on it would guard nothing.</para>
        /// <para>Whether the box holds the keyboard is not asked. Reading a long record is done with the
        /// wheel, the pointer passes over every box on the way, and which of them the caret is in is
        /// invisible while it happens — so a rule that spared the focused one would spare exactly the box
        /// whose silent change costs most, the one just typed into. A number is written by typing it or by
        /// the arrows beside it: both are gestures aimed at the box, and neither can be made by passing
        /// over it.</para>
        /// </summary>
        public static SpinBox Unwheeled(this SpinBox box)
        {
            ArgumentNullException.ThrowIfNull(box);

            box.GuiInput += @event => Wheeled(box, @event);

            // A spin box is a text box with buttons beside it, and the pointer stands on the text box:
            // the guard has to be where the event actually lands, or the box under it answers first.
            box.GetLineEdit().GuiInput += @event => Wheeled(box, @event);

            return box;
        }

        /// <summary>Takes one turn of the wheel away from the control and gives it to the pane. The event
        /// is accepted before anything else is asked of it: the signal is heard ahead of the control's own
        /// reading, and an accepted event is one the control — and the frame around it — never sees.</summary>
        private static void Wheeled(Control control, InputEvent @event)
        {
            if (@event is not InputEventMouseButton wheel) return;
            if (wheel.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)) return;

            control.AcceptEvent();

            // One turn is a press and a release; the pane moves once, on the press.
            if (!wheel.Pressed) return;

            Scroll(control, wheel.ButtonIndex == MouseButton.WheelUp ? -1 : 1, wheel.Factor);
        }

        /// <summary>Moves the pane the control sits in by one turn. Nothing at all where it sits in no
        /// scrolling pane, or in one showing all it holds: the turn is then a gesture with nowhere to go,
        /// and the value it did not change is the whole of what was asked for.</summary>
        private static void Scroll(Control control, int way, float factor)
        {
            if (Pane(control) is not { } pane) return;

            VScrollBar bar = pane.GetVScrollBar();

            if (!bar.IsVisibleInTree()) return;

            pane.ScrollVertical += way * (int)(bar.Page / WheelShare * factor);
        }

        /// <summary>The scrolling frame the control is drawn inside, or nothing when it is drawn in none.</summary>
        private static ScrollContainer? Pane(Node node)
        {
            for (Node? walked = node.GetParent(); walked is not null; walked = walked.GetParent())
                if (walked is ScrollContainer pane)
                    return pane;

            return null;
        }
    }
}
