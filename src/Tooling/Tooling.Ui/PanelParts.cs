namespace Tooling.Ui
{
    using System;
    using Godot;
    using Tooling.Json;

    /// <summary>The two things every panel of the tooling that draws a document does the same way: it
    /// clears what it drew before, and it hears about one document and about no other.</summary>
    public static class PanelParts
    {
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
    }
}
