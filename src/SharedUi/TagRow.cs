namespace SharedUi
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>
    /// A wrapping row of <see cref="TagPlate"/>s. Hand it the tag strings and it rebuilds itself;
    /// the flow container wraps to the next line when the window is too narrow.
    /// </summary>
    public partial class TagRow : HFlowContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://dcfjvgbwrdkh";

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetTags(IEnumerable<string> tags)
        {
            foreach (var child in GetChildren()) child.QueueFree();
            var plateScene = TagPlate.Initialize();
            foreach (string tag in tags)
            {
                var plate = plateScene.Instantiate<TagPlate>();
                plate.SetTag(tag);
                AddChild(plate);
            }
        }
    }
}
